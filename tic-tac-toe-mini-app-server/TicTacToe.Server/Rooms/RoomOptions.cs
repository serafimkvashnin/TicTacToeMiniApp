namespace TicTacToe.Server.Rooms;

public sealed class RoomOptions
{
    public const string SectionName = "Rooms";

    /// <summary>Разрешить одному пользователю Telegram занять оба места (для тестов).</summary>
    public bool AllowSelfPlay { get; set; }

    /// <summary>Сколько ждать живого соперника в подборе, прежде чем подсадить замаскированного бота.</summary>
    public TimeSpan QuickPlayBotDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Вероятность, что замаскированный бот уйдёт после партии, а не сыграет ещё.</summary>
    public double BotLeaveChance { get; set; } = 0.3;
}
