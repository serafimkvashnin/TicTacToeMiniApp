using Microsoft.AspNetCore.SignalR;
using TicTacToe.Server.Bots;
using TicTacToe.Server.Gameplay;
using TicTacToe.Server.Rooms;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Hubs;

public interface IGameClient
{
    Task RoomUpdated(RoomDto room);

    /// <summary>Игрок на месте <paramref name="seat"/> отправил стикер.</summary>
    Task Emote(int seat, string emote);
}

public sealed class GameHub(
    RoomManager rooms,
    BotDriver bots,
    EmoteLimiter emoteLimiter,
    TelegramAuthenticator authenticator,
    TelegramBotApi botApi,
    ILogger<GameHub> logger)
    : Hub<IGameClient>
{
    internal const string UserKey = "user";

    private const string ClientErrorCountKey = "clientErrors";
    private const int MaxClientErrorsPerConnection = 20;

    private TelegramUser CurrentUser => (TelegramUser)Context.Items[UserKey]!;

    private Player CurrentPlayer => new(Context.ConnectionId, CurrentUser);

    public override async Task OnConnectedAsync()
    {
        var initData = Context.GetHttpContext()?.Request.Query["initData"].ToString() ?? "";
        var user = authenticator.Authenticate(initData);
        if (user is null)
        {
            logger.LogWarning("Rejected connection {ConnectionId}: invalid initData", Context.ConnectionId);
            Context.Abort();
            return;
        }

        Context.Items[UserKey] = user;
        // @username пишем отдельно: по нему видно, сможет ли соперник открыть чат с игроком
        logger.LogInformation("User {UserId} ({UserName}, @{Username}) connected, connection {ConnectionId}",
            user.Id, user.DisplayName, user.Username ?? "—", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        emoteLimiter.Forget(Context.ConnectionId);

        // Отклонённое подключение: пользователя нет и в комнатах его тоже нет
        if (Context.Items[UserKey] is TelegramUser user)
        {
            if (exception is null)
                logger.LogInformation("User {UserId} ({UserName}) disconnected", user.Id, user.DisplayName);
            else
                logger.LogWarning(exception, "User {UserId} ({UserName}) connection lost", user.Id, user.DisplayName);

            await LeaveCurrentRoom();
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<RoomDto> CreateRoom()
    {
        await LeaveCurrentRoom();
        var room = rooms.Create(CurrentPlayer).For(Context.ConnectionId);
        LogRoomAction("created a room", room);
        return room;
    }

    public async Task<RoomDto> JoinRoom(string? code)
    {
        await LeaveCurrentRoom();
        var room = await Apply(() => rooms.Join(code, CurrentPlayer));
        LogRoomAction("joined a room by code", room);
        return room;
    }

    public async Task<RoomDto> QuickPlay()
    {
        await LeaveCurrentRoom();
        var room = await Apply(() => rooms.QuickPlay(CurrentPlayer));
        LogRoomAction(room.Game is null ? "started searching for an opponent" : "found an opponent", room);
        return room;
    }

    public async Task<RoomDto> PlayBot(BotDifficulty difficulty)
    {
        await LeaveCurrentRoom();
        var room = await Apply(() => rooms.PlayBot(CurrentPlayer, difficulty));
        LogRoomAction($"started a {difficulty} bot game", room);
        return room;
    }

    /// <summary>
    /// Ошибка JS у игрока: иначе мы бы о ней никогда не узнали.
    /// Количество с одного подключения ограничено, чтобы сломанный клиент не забил лог.
    /// </summary>
    public void ReportClientError(string message, string? stack)
    {
        var count = Context.Items.TryGetValue(ClientErrorCountKey, out var value) ? (int)value! : 0;
        if (count >= MaxClientErrorsPerConnection)
            return;
        Context.Items[ClientErrorCountKey] = count + 1;

        var userAgent = Context.GetHttpContext()?.Request.Headers.UserAgent.ToString();
        logger.LogWarning(
            "Client error from user {UserId} ({UserName}): {ErrorMessage}{NewLine}User agent: {UserAgent}{NewLine}{Stack}",
            CurrentUser.Id, CurrentUser.DisplayName, Truncate(message, 1000),
            Environment.NewLine, userAgent, Environment.NewLine, Truncate(stack, 4000));
    }

    private void LogRoomAction(string action, RoomDto room) =>
        logger.LogInformation("User {UserId} ({UserName}) {Action}: room {RoomCode} ({RoomKind})",
            CurrentUser.Id, CurrentUser.DisplayName, action, room.Code, room.Kind.ToString());

    private static string? Truncate(string? text, int max) =>
        text is null || text.Length <= max ? text : text[..max] + "…";

    public Task<RoomDto> MakeMove(int cell) => Apply(() => rooms.MakeMove(Context.ConnectionId, cell));

    public Task<RoomDto> Rematch() => Apply(() => rooms.Rematch(Context.ConnectionId));

    public Task LeaveRoom() => LeaveCurrentRoom();

    /// <summary>
    /// Готовит карточку-приглашение в свою комнату; клиент делится ей через Telegram.WebApp.shareMessage.
    /// </summary>
    /// <returns>id подготовленного сообщения.</returns>
    public async Task<string> PrepareInvite()
    {
        var code = rooms.InviteCodeFor(Context.ConnectionId)
            ?? throw new HubException(ErrorCodes.InviteNotAllowed);

        // Гости из режима разработки — не пользователи Telegram, им Bot API ничего не подготовит
        if (CurrentUser.Id <= 0)
            throw new HubException(ErrorCodes.InviteTelegramOnly);

        try
        {
            return await botApi.PrepareRoomInviteAsync(CurrentUser.Id, code, CurrentUser.LanguageCode, Context.ConnectionAborted);
        }
        catch (Exception e) when (e is TelegramApiException or HttpRequestException)
        {
            logger.LogError(e, "Failed to prepare invite to room {RoomCode} for user {UserId}", code, CurrentUser.Id);
            throw new HubException(ErrorCodes.InviteFailed);
        }
    }

    /// <summary>Стикер видят оба игрока: он вылетает из плашки отправителя. Слишком частые молча отбрасываются.</summary>
    public async Task SendEmote(string emote)
    {
        if (!Emotes.All.Contains(emote))
            throw new HubException(ErrorCodes.EmoteUnknown);

        if (!emoteLimiter.TryAcquire(Context.ConnectionId) || rooms.EmoteTargetsFor(Context.ConnectionId) is not { } targets)
            return;

        await Clients.Clients(targets.Recipients).Emote(targets.Seat, emote);
    }

    /// <summary>Выполняет действие, рассылает новое состояние соперникам и возвращает его вызвавшему.</summary>
    private async Task<RoomDto> Apply(Func<RoomUpdate> action)
    {
        RoomUpdate update;
        try
        {
            update = action();
        }
        catch (RoomException e)
        {
            throw new HubException(e.Message);
        }

        await NotifyOthers(update);
        bots.OnRoomChanged(update.Code);
        return update.For(Context.ConnectionId);
    }

    private async Task LeaveCurrentRoom()
    {
        var update = rooms.Leave(Context.ConnectionId);
        await NotifyOthers(update);
        bots.OnRoomChanged(update.Code);
    }

    private Task NotifyOthers(RoomUpdate update) => Task.WhenAll(update.Views
        .Where(v => v.ConnectionId != Context.ConnectionId)
        .Select(v => Clients.Client(v.ConnectionId).RoomUpdated(v.Room)));
}
