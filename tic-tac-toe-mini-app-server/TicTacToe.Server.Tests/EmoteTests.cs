using Microsoft.Extensions.Options;
using TicTacToe.Server.Gameplay;
using TicTacToe.Server.Hubs;
using TicTacToe.Server.Rooms;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Tests;

public class EmoteTests
{
    private static readonly Player Alice = new("conn-alice", new TelegramUser(1, "Alice", null, "alice"));
    private static readonly Player Bob = new("conn-bob", new TelegramUser(2, "Bob", null, null));

    /// <summary>Часы, которые двигает тест.</summary>
    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void Limiter_allows_one_emote_per_interval_per_connection()
    {
        var time = new ManualTime();
        var limiter = new EmoteLimiter(time);

        Assert.True(limiter.TryAcquire("a"));
        Assert.False(limiter.TryAcquire("a"));
        Assert.True(limiter.TryAcquire("b")); // у другого подключения свой счётчик

        time.Now += EmoteLimiter.MinInterval;
        Assert.True(limiter.TryAcquire("a"));

        // Отклонённая попытка не сдвигает окно
        time.Now += EmoteLimiter.MinInterval / 2;
        Assert.False(limiter.TryAcquire("a"));
        time.Now += EmoteLimiter.MinInterval / 2;
        Assert.True(limiter.TryAcquire("a"));
    }

    [Fact]
    public void Emote_goes_to_both_players_with_sender_seat()
    {
        var manager = new RoomManager(Options.Create(new RoomOptions()));
        var code = manager.Create(Alice).For(Alice.ConnectionId).Code;
        manager.Join(code, Bob);

        var targets = manager.EmoteTargetsFor(Bob.ConnectionId)!;

        Assert.Equal(1, targets.Seat);
        Assert.Equal(new[] { Alice.ConnectionId, Bob.ConnectionId }, targets.Recipients);
    }

    [Fact]
    public void Bot_nags_only_while_human_is_thinking()
    {
        var manager = new RoomManager(Options.Create(new RoomOptions()));
        var random = new Random(31);
        var room = manager.PlayBot(Alice, BotDifficulty.Hard).For(Alice.ConnectionId);
        if (manager.NextBotTask(room.Code) is BotMoveTask botFirst)
            room = manager.TryBotMove(botFirst, random).For(Alice.ConnectionId);

        // Ход Алисы: бот может торопить, стикер вылетает из плашки бота и приходит только Алисе
        var task = manager.NextBotEmoteTask(room.Code)!;
        var targets = manager.BotEmoteTargets(task)!;
        Assert.Equal(1 - room.YourSeat, targets.Seat);
        Assert.Equal(new[] { Alice.ConnectionId }, targets.Recipients);

        // Алиса сходила — старая задача больше не действует, а на ходу бота торопить некого
        var free = room.Game!.Board.Select((m, i) => (m, i)).First(x => x.m is null).i;
        manager.MakeMove(Alice.ConnectionId, free);
        Assert.Null(manager.BotEmoteTargets(task));
        Assert.Null(manager.NextBotEmoteTask(room.Code));
    }

    [Fact]
    public void No_nagging_in_games_between_people()
    {
        var manager = new RoomManager(Options.Create(new RoomOptions()));
        var code = manager.Create(Alice).For(Alice.ConnectionId).Code;
        manager.Join(code, Bob);

        Assert.Null(manager.NextBotEmoteTask(code));
    }

    [Fact]
    public void Emote_is_not_sent_to_bots_and_requires_a_room()
    {
        var manager = new RoomManager(Options.Create(new RoomOptions()));
        Assert.Null(manager.EmoteTargetsFor(Alice.ConnectionId));

        manager.PlayBot(Alice, BotDifficulty.Easy);
        var targets = manager.EmoteTargetsFor(Alice.ConnectionId)!;

        Assert.Equal(new[] { Alice.ConnectionId }, targets.Recipients);
    }
}
