namespace TicTacToe.Server.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Токен бота от @BotFather. Им проверяется подпись initData.</summary>
    public string BotToken { get; set; } = "";

    /// <summary>Сколько initData считается действительной после выдачи.</summary>
    public TimeSpan InitDataMaxAge { get; set; } = TimeSpan.FromDays(1);
}
