namespace TicTacToe.Server.Telegram;

/// <param name="LanguageCode">Язык приложения Telegram у пользователя (language_code), например "ru" или "en".</param>
public sealed record TelegramUser(long Id, string FirstName, string? LastName, string? Username, string? LanguageCode = null)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}";
}
