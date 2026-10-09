namespace TicTacToe.Server;

/// <summary>
/// Коды ошибок, которые сервер отправляет клиенту вместо готового текста.
/// Текст на языке игрока берётся из словарей клиента (src/i18n, раздел errors) — коды должны совпадать.
/// </summary>
public static class ErrorCodes
{
    public const string RoomNotFound = "room.notFound";
    public const string RoomAlreadyJoined = "room.alreadyJoined";
    public const string RoomFull = "room.full";
    public const string NotInRoom = "room.notInRoom";
    public const string NeedOpponent = "room.needOpponent";

    public const string GameNotStarted = "game.notStarted";
    public const string MoveGameOver = "move.gameOver";
    public const string MoveNotYourTurn = "move.notYourTurn";
    public const string MoveCellTaken = "move.cellTaken";
    public const string MoveInvalid = "move.invalid";

    public const string InviteNotAllowed = "invite.notAllowed";
    public const string InviteTelegramOnly = "invite.telegramOnly";
    public const string InviteFailed = "invite.failed";

    public const string EmoteUnknown = "emote.unknown";
}
