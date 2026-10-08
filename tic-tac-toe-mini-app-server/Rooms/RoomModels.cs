using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Rooms;

public sealed record Player(string ConnectionId, TelegramUser User);

public sealed record PlayerDto(long Id, string Name, string? Username, bool IsHost);

public sealed record RoomDto(string Code, int Capacity, IReadOnlyList<PlayerDto> Players);

public enum JoinStatus
{
    Joined,
    NotFound,
    Full,
    AlreadyInRoom,
}

public sealed record JoinResult(JoinStatus Status, RoomDto? Room = null);

/// <param name="Code">Код комнаты, из которой вышел игрок.</param>
/// <param name="Room">Состояние комнаты после выхода; null, если комната удалена.</param>
public sealed record LeaveResult(string Code, RoomDto? Room);
