using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace TicTacToe.Server.Telegram;

/// <summary>
/// Проверяет initData из Telegram Mini App:
/// https://core.telegram.org/bots/webapps#validating-data-received-via-the-mini-app
/// </summary>
public sealed class TelegramAuthenticator(
    IOptions<TelegramOptions> options,
    IHostEnvironment environment,
    ILogger<TelegramAuthenticator> logger)
{
    private readonly TelegramOptions _options = options.Value;

    public TelegramUser? Authenticate(string initData)
    {
        if (string.IsNullOrEmpty(_options.BotToken))
        {
            if (!environment.IsDevelopment())
            {
                logger.LogError("Telegram:BotToken is not configured, rejecting connection");
                return null;
            }

            // Локальная разработка без токена: доверяем initData без проверки,
            // а в обычном браузере (initData пустая) выдаём гостя
            return ParseUser(QueryHelpers.ParseQuery(initData)) ?? CreateGuest();
        }

        return Validate(initData);
    }

    private TelegramUser? Validate(string initData)
    {
        var fields = QueryHelpers.ParseQuery(initData);
        if (!fields.TryGetValue("hash", out var hash) || !IsSha256Hex(hash.ToString()))
            return null;

        var dataCheckString = string.Join('\n', fields
            .Where(f => f.Key != "hash")
            .OrderBy(f => f.Key, StringComparer.Ordinal)
            .Select(f => $"{f.Key}={f.Value}"));

        var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(_options.BotToken));
        var expected = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        if (!CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(hash.ToString())))
            return null;

        if (!fields.TryGetValue("auth_date", out var authDateRaw) || !long.TryParse(authDateRaw, out var authDate))
            return null;

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(authDate);
        if (age > _options.InitDataMaxAge)
            return null;

        return ParseUser(fields);
    }

    private static TelegramUser? ParseUser(Dictionary<string, StringValues> fields)
    {
        if (!fields.TryGetValue("user", out var userJson))
            return null;

        var user = JsonSerializer.Deserialize<UserPayload>(userJson.ToString());
        return user is null
            ? null
            : new TelegramUser(user.Id, user.FirstName, user.LastName, user.Username, user.LanguageCode);
    }

    // Гость бывает только при локальной разработке в обычном браузере
    private static TelegramUser CreateGuest()
    {
        var number = RandomNumberGenerator.GetInt32(1000, 10000);
        return new TelegramUser(-number, $"Guest {number}", null, null);
    }

    private static bool IsSha256Hex(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private sealed record UserPayload(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("first_name")] string FirstName,
        [property: JsonPropertyName("last_name")] string? LastName,
        [property: JsonPropertyName("username")] string? Username,
        [property: JsonPropertyName("language_code")] string? LanguageCode);
}
