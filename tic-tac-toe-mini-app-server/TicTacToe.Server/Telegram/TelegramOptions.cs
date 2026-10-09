namespace TicTacToe.Server.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Токен бота от @BotFather. Им проверяется подпись initData и вызывается Bot API.</summary>
    public string BotToken { get; set; } = "";

    /// <summary>Сколько initData считается действительной после выдачи.</summary>
    public TimeSpan InitDataMaxAge { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Карточка-приглашение в комнату, которой игрок делится из игры (shareMessage).</summary>
    public InviteCardOptions Invite { get; set; } = new();
}

/// <summary>Та же карточка, что бот NamingIssuesBot отдаёт в inline-режиме.</summary>
public sealed class InviteCardOptions
{
    /// <summary>Ссылка на Mini App; к ней добавляется ?startapp=КОД комнаты.</summary>
    public string AppUrl { get; set; } = "https://t.me/NamingIssuesBot/tictactoe";

    /// <summary>Анимация 640×360 (mp4) со статического хостинга бота.</summary>
    public string MediaUrl { get; set; } = "https://app8619268643.tgcloud.ai/inline/tictactoe.mp4";

    /// <summary>Превью анимации (jpg).</summary>
    public string ThumbnailUrl { get; set; } = "https://app8619268643.tgcloud.ai/inline/tictactoe-thumb.jpg";
}
