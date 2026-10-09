using TicTacToe.Server.Gameplay;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Rooms;

/// <param name="Disguised">Бот подставлен вместо живого соперника, и игрок об этом не знает.</param>
public sealed record BotProfile(BotDifficulty Difficulty, bool Disguised);

/// <param name="Bot">Не null — игроком управляет сервер; подключения у бота нет.</param>
public sealed record Player(string ConnectionId, TelegramUser User, BotProfile? Bot = null)
{
    public bool IsBot => Bot is not null;
}

public enum RoomKind
{
    /// <summary>Игра с другом, вход по коду.</summary>
    Private,

    /// <summary>Подбор случайного соперника.</summary>
    Public,

    /// <summary>Игра с ботом, выбранная в меню.</summary>
    Bot,
}

public sealed record PlayerDto(long Id, string Name, string? Username, bool IsHost, Mark? Mark);

public sealed record GameDto(
    IReadOnlyList<Mark?> Board,
    Mark Turn,
    GameStatus Status,
    Mark? Winner,
    IReadOnlyList<int>? WinningLine);

/// <param name="YourSeat">Место получателя в списке игроков: состояние у каждого подключения своё.</param>
/// <param name="Version">Растёт с каждым изменением, чтобы клиент мог отбросить устаревшее состояние.</param>
public sealed record RoomDto(
    string Code,
    int Capacity,
    RoomKind Kind,
    IReadOnlyList<PlayerDto> Players,
    int YourSeat,
    int Version,
    GameDto? Game);

public sealed record RoomView(string ConnectionId, RoomDto Room);

/// <summary>Новое состояние комнаты для каждого живого игрока в ней.</summary>
/// <param name="Code">Комната, которая изменилась; null — ничего не изменилось.</param>
public sealed record RoomUpdate(string? Code, IReadOnlyList<RoomView> Views)
{
    public static readonly RoomUpdate Empty = new(null, []);

    public RoomDto For(string connectionId) => Views.First(v => v.ConnectionId == connectionId).Room;
}

/// <summary>Нарушение правил комнаты или игры; сообщение показывается игроку.</summary>
public sealed class RoomException(string message) : Exception(message);

/// <summary>Отложенное действие бота. Токен (версия или момент ожидания) не даёт действовать по устаревшему состоянию.</summary>
public abstract record BotTask(string Code);

/// <summary>Игрок ждёт случайного соперника — через паузу подсадить к нему бота.</summary>
public sealed record FillWithBotTask(string Code, long WaitingSince) : BotTask(Code);

public sealed record BotMoveTask(string Code, int Version) : BotTask(Code);

/// <summary>Партия с замаскированным ботом закончилась — он решает, уйти или сыграть ещё.</summary>
public sealed record BotAfterGameTask(string Code, int Version) : BotTask(Code);

/// <summary>Ход игрока в партии с замаскированным ботом — если игрок так и не сходит, бот уйдёт.</summary>
public sealed record BotIdleLeaveTask(string Code, int Version) : BotTask(Code);
