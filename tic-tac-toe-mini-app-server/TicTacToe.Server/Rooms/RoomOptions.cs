namespace TicTacToe.Server.Rooms;

/// <summary>
/// Настройки комнат и ботов, секция "Rooms" в appsettings.json.
/// Времена задаются диапазонами: каждый раз берётся случайное значение между Min и Max.
/// </summary>
public sealed class RoomOptions
{
    public const string SectionName = "Rooms";

    /// <summary>Разрешить одному пользователю Telegram занять оба места (для тестов).</summary>
    public bool AllowSelfPlay { get; set; }

    /// <summary>Сколько ждать живого соперника в подборе, прежде чем подсадить замаскированного бота.</summary>
    public TimeRange QuickPlayBotDelay { get; set; } = TimeRange.Seconds(10, 14);

    /// <summary>Сколько бот «думает» над ходом.</summary>
    public TimeRange BotMoveDelay { get; set; } = TimeRange.Seconds(0.5, 2);

    /// <summary>Пауза замаскированного бота после партии, прежде чем уйти или начать реванш.</summary>
    public TimeRange BotAfterGameDelay { get; set; } = TimeRange.Seconds(1.5, 4.5);

    /// <summary>Сколько замаскированный бот ждёт хода бездействующего игрока, прежде чем уйти.</summary>
    public TimeRange BotIdleTimeout { get; set; } = TimeRange.Seconds(20, 40);

    /// <summary>Вероятность (0–1), что замаскированный бот уйдёт после партии, а не сыграет ещё.</summary>
    public double BotLeaveChance { get; set; } = 0.5;

    /// <summary>
    /// Вероятность (0–1), что замаскированный бот уйдёт посреди партии вместо ответа на ход игрока.
    /// Проверяется на каждый ход игрока.
    /// </summary>
    public double BotMidGameLeaveChance { get; set; } = 0.07;
}

/// <summary>Диапазон времени; в конфиге задаётся как { "Min": "00:00:10", "Max": "00:00:14" }.</summary>
public sealed class TimeRange
{
    public TimeSpan Min { get; set; }
    public TimeSpan Max { get; set; }

    public static TimeRange Seconds(double min, double max) =>
        new() { Min = TimeSpan.FromSeconds(min), Max = TimeSpan.FromSeconds(max) };

    /// <summary>Случайное значение в диапазоне; если Max не больше Min — ровно Min.</summary>
    public TimeSpan Pick(Random random) => Max <= Min ? Min : Min + (Max - Min) * random.NextDouble();
}
