using TicTacToe.Server.Gameplay;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Rooms;

public sealed record Player(string ConnectionId, TelegramUser User);

public sealed record PlayerDto(long Id, string Name, string? Username, bool IsHost, Mark? Mark);

public sealed record GameDto(
    IReadOnlyList<Mark?> Board,
    Mark Turn,
    GameStatus Status,
    Mark? Winner,
    IReadOnlyList<int>? WinningLine);

/// <param name="YourSeat">Место получателя в списке игроков: состояние у каждого подключения своё.</param>
/// <param name="Version">Растёт с каждым изменением, чтобы клиент мог отбросить устаревшее состояние.</param>
/// <param name="IsPublic">Комната из подбора случайного соперника.</param>
public sealed record RoomDto(
    string Code,
    int Capacity,
    bool IsPublic,
    IReadOnlyList<PlayerDto> Players,
    int YourSeat,
    int Version,
    GameDto? Game);

public sealed record RoomView(string ConnectionId, RoomDto Room);

/// <summary>Новое состояние комнаты для каждого из её подключений.</summary>
public sealed record RoomUpdate(IReadOnlyList<RoomView> Views)
{
    public static readonly RoomUpdate Empty = new([]);

    public RoomDto For(string connectionId) => Views.First(v => v.ConnectionId == connectionId).Room;
}

/// <summary>Нарушение правил комнаты или игры; сообщение показывается игроку.</summary>
public sealed class RoomException(string message) : Exception(message);
