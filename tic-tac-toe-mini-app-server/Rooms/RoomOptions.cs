namespace TicTacToe.Server.Rooms;

public sealed class RoomOptions
{
    public const string SectionName = "Rooms";

    /// <summary>Разрешить одному пользователю Telegram занять оба места (для тестов).</summary>
    public bool AllowSelfPlay { get; set; }
}
