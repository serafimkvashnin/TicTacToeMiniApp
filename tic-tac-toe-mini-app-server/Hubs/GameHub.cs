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

        var room = rooms.Create(CurrentPlayer);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);
        return room;
    }

    public async Task<RoomDto> JoinRoom(string code)
    {
        await LeaveCurrentRoom();

        var result = rooms.Join(code, CurrentPlayer);
        var room = result.Status switch
        {
            JoinStatus.Joined => result.Room!,
            JoinStatus.NotFound => throw new HubException("Комната не найдена"),
            JoinStatus.Full => throw new HubException("Комната уже заполнена"),
            JoinStatus.AlreadyInRoom => throw new HubException("Вы уже в этой комнате"),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);
        await Clients.Group(room.Code).RoomUpdated(room);
        return room;
    }

    public Task LeaveRoom() => LeaveCurrentRoom();

    private async Task LeaveCurrentRoom()
    {
        var left = rooms.Leave(Context.ConnectionId);
        if (left is null)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, left.Code);
        if (left.Room is not null)
            await Clients.Group(left.Code).RoomUpdated(left.Room);
    }
}
