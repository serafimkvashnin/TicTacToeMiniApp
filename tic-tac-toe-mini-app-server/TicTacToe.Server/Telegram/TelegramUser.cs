namespace TicTacToe.Server.Telegram;

public sealed record TelegramUser(long Id, string FirstName, string? LastName, string? Username)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}";
}
