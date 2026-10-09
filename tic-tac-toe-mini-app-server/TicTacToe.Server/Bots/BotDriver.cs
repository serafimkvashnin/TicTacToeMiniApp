using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TicTacToe.Server.Hubs;
using TicTacToe.Server.Rooms;

namespace TicTacToe.Server.Bots;

/// <summary>
/// Выполняет действия ботов с паузами, как у живого человека, и рассылает результат игрокам.
/// Вызывается после каждого изменения комнаты; каждое действие перед выполнением сверяется
/// с текущим состоянием, поэтому лишние или опоздавшие вызовы безопасны.
/// </summary>
public sealed class BotDriver(
    RoomManager rooms,
    IHubContext<GameHub, IGameClient> hub,
    IOptions<RoomOptions> options,
    ILogger<BotDriver> logger)
{
    private readonly RoomOptions _options = options.Value;

    public void OnRoomChanged(string? code)
    {
        if (code is null)
            return;

        if (rooms.NextBotTask(code) is { } task)
            _ = RunAsync(task);

        // Независимо от основной задачи: пока игрок думает, бот может его поторапливать
        if (_options.BotEmoteChance > 0 && rooms.NextBotEmoteTask(code) is { } emote)
            _ = NagAsync(emote);
    }

    /// <summary>
    /// Игрок думает над ходом: после паузы бот тапает по стикеру сериями, как нетерпеливый человек —
    /// то разок, то несколько раз подряд, и чем дольше ждёт, тем серии длиннее.
    /// Цикл заканчивается сам, как только игрок сходил, вышел или бот ушёл.
    /// </summary>
    private async Task NagAsync(BotEmoteTask task)
    {
        try
        {
            var random = Random.Shared;
            await Task.Delay(_options.BotEmoteIdleDelay.Pick(random));

            var burst = 0;
            while (rooms.BotEmoteTargets(task) is not null)
            {
                if (random.NextDouble() < _options.BotEmoteChance)
                {
                    var taps = _options.BotEmoteTaps.PickEscalating(random, burst++);
                    for (var tap = 0; tap < taps; tap++)
                    {
                        // Игрок мог сходить посреди серии — тогда сразу перестаём
                        if (rooms.BotEmoteTargets(task) is not { } targets)
                            return;

                        await hub.Clients.Clients(targets.Recipients).Emote(targets.Seat, Emotes.Impatient);
                        if (tap < taps - 1)
                            await Task.Delay(_options.BotEmoteTapGap.Pick(random));
                    }
                }

                await Task.Delay(_options.BotEmoteInterval.Pick(random));
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Bot emote task {Task} failed", task);
        }
    }

    private async Task RunAsync(BotTask task)
    {
        try
        {
            var random = Random.Shared;
            RoomUpdate update;

            switch (task)
            {
                case FillWithBotTask fill:
                    await Task.Delay(_options.QuickPlayBotDelay.Pick(random));
                    update = rooms.TryFillWithBot(fill, random);
                    break;

                case BotMoveTask move:
                    await Task.Delay(_options.BotMoveDelay.Pick(random));
                    update = rooms.TryBotMove(move, random);
                    break;

                case BotAfterGameTask afterGame:
                    await Task.Delay(_options.BotAfterGameDelay.Pick(random));
                    update = rooms.TryBotAfterGame(afterGame, leave: random.NextDouble() < _options.BotLeaveChance);
                    break;

                case BotIdleLeaveTask idle:
                    await Task.Delay(_options.BotIdleTimeout.Pick(random));
                    update = rooms.TryBotIdleLeave(idle);
                    break;

                default:
                    return;
            }

            if (update.Code is null)
                return;

            await Task.WhenAll(update.Views.Select(v => hub.Clients.Client(v.ConnectionId).RoomUpdated(v.Room)));

            // Следующий шаг: например, после подсадки бота его ход, если ему достались крестики
            OnRoomChanged(update.Code);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Bot task {Task} failed", task);
        }
    }
}
