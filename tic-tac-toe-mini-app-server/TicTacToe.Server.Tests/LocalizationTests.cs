using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicTacToe.Server.Bots;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Tests;

public class LocalizationTests
{
    [Theory]
    [InlineData("ru", true)]
    [InlineData("uk", true)]
    [InlineData("be", true)]
    [InlineData("kk", true)]
    [InlineData("ru-RU", true)]
    [InlineData("RU", true)]
    [InlineData("en", false)]
    [InlineData("de", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Russian_speaking_languages(string? code, bool expected) =>
        Assert.Equal(expected, Languages.IsRussianSpeaking(code));

    [Fact]
    public void Bot_names_follow_player_language()
    {
        static double CyrillicShare(string? language)
        {
            var random = new Random(7);
            var names = Enumerable.Range(0, 400).Select(_ => BotNames.CreateUser(random, language).DisplayName).ToList();
            return names.Count(n => n.Any(c => c is >= 'А' and <= 'я' or 'Ё' or 'ё')) / (double)names.Count;
        }

        Assert.InRange(CyrillicShare("ru"), 0.55, 0.8);
        Assert.InRange(CyrillicShare("en"), 0.05, 0.25);
    }

    /// <summary>Подменяет Bot API: запоминает отправленный запрос и отвечает успехом.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public JsonElement Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)).RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":true,"result":{"id":"prepared-1"}}""", Encoding.UTF8, "application/json"),
            };
        }
    }

    [Theory]
    [InlineData("ru", "Заходи ко мне в комнату K7MPQ", "▶️ Присоединиться")]
    [InlineData("en", "Join my room K7MPQ", "▶️ Join")]
    [InlineData(null, "Join my room K7MPQ", "▶️ Join")]
    public async Task Invite_card_is_written_in_inviter_language(string? language, string captionPart, string button)
    {
        var handler = new CapturingHandler();
        var api = new TelegramBotApi(new HttpClient(handler), Options.Create(new TelegramOptions { BotToken = "123:test" }));

        var id = await api.PrepareRoomInviteAsync(42, "K7MPQ", language, CancellationToken.None);

        Assert.Equal("prepared-1", id);
        var result = handler.Request.GetProperty("result");
        Assert.Contains(captionPart, result.GetProperty("caption").GetString());
        var key = result.GetProperty("reply_markup").GetProperty("inline_keyboard")[0][0];
        Assert.Equal(button, key.GetProperty("text").GetString());
        Assert.EndsWith("?startapp=K7MPQ", key.GetProperty("url").GetString());
        Assert.Equal(42, handler.Request.GetProperty("user_id").GetInt64());
    }
}
