using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TicTacToe.Server.Telegram;

/// <summary>
/// Минимальный клиент Bot API. Сам бот (inline-режим, меню) живёт на Telegram Serverless —
/// сервер игры вызывает Bot API только чтобы подготовить карточку-приглашение.
/// </summary>
public sealed class TelegramBotApi(HttpClient http, IOptions<TelegramOptions> options)
{
    private readonly TelegramOptions _options = options.Value;

    /// <summary>
    /// Готовит карточку-приглашение в комнату, которой игрок поделится через shareMessage.
    /// </summary>
    /// <returns>id подготовленного сообщения для Telegram.WebApp.shareMessage.</returns>
    public async Task<string> PrepareRoomInviteAsync(long userId, string roomCode, CancellationToken cancellationToken)
    {
        var invite = _options.Invite;
        var result = new
        {
            type = "mpeg4_gif",
            id = $"invite-{roomCode}-{Guid.NewGuid():N}"[..40],
            mpeg4_url = invite.MediaUrl,
            mpeg4_width = 640,
            mpeg4_height = 360,
            thumbnail_url = invite.ThumbnailUrl,
            thumbnail_mime_type = "image/jpeg",
            caption = $"Крестики-нолики ✕⭘ Сыграем?\nЗаходи ко мне в комнату {roomCode} 🎮",
            reply_markup = new
            {
                inline_keyboard = new[]
                {
                    new[] { new { text = "▶️ Присоединиться", url = $"{invite.AppUrl}?startapp={roomCode}" } },
                },
            },
        };

        var prepared = await CallAsync("savePreparedInlineMessage", new
        {
            user_id = userId,
            result,
            allow_user_chats = true,
            allow_group_chats = true,
            allow_channel_chats = true,
        }, cancellationToken);

        return prepared.GetProperty("id").GetString()!;
    }

    private async Task<JsonElement> CallAsync(string method, object parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.BotToken))
            throw new InvalidOperationException("Telegram:BotToken is not configured");

        // В адресе запроса есть токен — логирование HttpClient для этого отключено в appsettings
        using var response = await http.PostAsJsonAsync(
            $"https://api.telegram.org/bot{_options.BotToken}/{method}", parameters, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        if (!body.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
        {
            var description = body.TryGetProperty("description", out var d) ? d.GetString() : response.ReasonPhrase;
            throw new TelegramApiException(method, (int)response.StatusCode, description);
        }

        return body.GetProperty("result");
    }
}

public sealed class TelegramApiException(string method, int statusCode, string? description)
    : Exception($"Bot API {method} failed ({statusCode}): {description}");
