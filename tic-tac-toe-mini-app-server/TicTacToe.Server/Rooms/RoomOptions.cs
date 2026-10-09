using TicTacToe.Server.Gameplay;

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

    /// <summary>Сколько игрок может думать над ходом, прежде чем бот начнёт его торопить стикерами.</summary>
    public TimeRange BotEmoteIdleDelay { get; set; } = TimeRange.Seconds(8, 12);

    /// <summary>Как часто бот решает, кинуть ли стикер, пока игрок продолжает думать.</summary>
    public TimeRange BotEmoteInterval { get; set; } = TimeRange.Seconds(2, 3);

    /// <summary>Вероятность (0–1) кинуть стикер при каждой такой проверке. 0 — боты стикеры не кидают.</summary>
    public double BotEmoteChance { get; set; } = 0.9;

    /// <summary>Вероятность (0–1), что замаскированный бот уйдёт после партии, а не сыграет ещё.</summary>
    public double BotLeaveChance { get; set; } = 0.5;

    /// <summary>
    /// Из каких сложностей случайно выбирается замаскированный бот. Лёгкий сюда не входит:
    /// его частые нелепые ходы выдают, что это не человек. Пусто — значение по умолчанию.
    /// </summary>
    public BotDifficulty[] DisguisedBotDifficulties { get; set; } = [];

    private static readonly BotDifficulty[] DefaultDisguisedDifficulties = [BotDifficulty.Medium, BotDifficulty.Hard];

    public BotDifficulty PickDisguisedDifficulty(Random random)
    {
        // Массив по умолчанию в коде пустой: биндер конфигурации дописывает элементы к существующим, а не заменяет их
        var options = DisguisedBotDifficulties.Length > 0 ? DisguisedBotDifficulties : DefaultDisguisedDifficulties;
        return options[random.Next(options.Length)];
    }
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
