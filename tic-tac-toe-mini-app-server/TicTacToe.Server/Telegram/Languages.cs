namespace TicTacToe.Server.Telegram;

/// <summary>Выбор языка по language_code из Telegram — те же правила, что у клиента (src/i18n/index.ts).</summary>
public static class Languages
{
    // Языки, носителям которых привычнее русский, чем английский
    private static readonly HashSet<string> RussianSpeaking = ["ru", "uk", "be", "kk"];

    public static bool IsRussianSpeaking(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return false;

        var baseCode = languageCode.Split('-')[0].ToLowerInvariant();
        return RussianSpeaking.Contains(baseCode);
    }
}
