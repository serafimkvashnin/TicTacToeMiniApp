using Microsoft.AspNetCore.SignalR;
using TicTacToe.Server.Rooms;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Hubs;

public interface IGameClient
{
    Task RoomUpdated(RoomDto room);
}

public sealed class GameHub(RoomManager rooms, TelegramAuthenticator authenticator, ILogger<GameHub> logger)
    : Hub<IGameClient>
{
    private const string UserKey = "user";

    private Player CurrentPlayer => new(Context.ConnectionId, (TelegramUser)Context.Items[UserKey]!);

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
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCurrentRoom();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<RoomDto> CreateRoom()
    {
        await LeaveCurrentRoom();
        return rooms.Create(CurrentPlayer).For(Context.ConnectionId);
    }

    public async Task<RoomDto> JoinRoom(string code)
    {
        await LeaveCurrentRoom();
        return await Apply(() => rooms.Join(code, CurrentPlayer));
    }

    public Task<RoomDto> MakeMove(int cell) => Apply(() => rooms.MakeMove(Context.ConnectionId, cell));

    public Task<RoomDto> Rematch() => Apply(() => rooms.Rematch(Context.ConnectionId));

    public Task LeaveRoom() => LeaveCurrentRoom();

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
        return update.For(Context.ConnectionId);
    }

    private Task LeaveCurrentRoom() => NotifyOthers(rooms.Leave(Context.ConnectionId));

    private Task NotifyOthers(RoomUpdate update) => Task.WhenAll(update.Views
        .Where(v => v.ConnectionId != Context.ConnectionId)
        .Select(v => Clients.Client(v.ConnectionId).RoomUpdated(v.Room)));
}
