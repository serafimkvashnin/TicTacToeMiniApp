using Microsoft.AspNetCore.SignalR;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Hubs;

/// <summary>
/// Неожиданное исключение в методе хаба пишется в лог вместе с тем, кто и что вызывал,
/// иначе по логу не понять, у кого и в какой ситуации упало. Отказы по правилам (HubException) не логируются.
/// </summary>
public sealed class HubErrorLoggingFilter(ILogger<HubErrorLoggingFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext context,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception e) when (e is not HubException)
        {
            var user = context.Context.Items[GameHub.UserKey] as TelegramUser;
            logger.LogError(
                e,
                "Hub method {Method}({Args}) failed for user {UserId} ({UserName}), connection {ConnectionId}",
                context.HubMethodName,
                context.HubMethodArguments,
                user?.Id,
                user?.DisplayName,
                context.Context.ConnectionId);
            throw;
        }
    }
}
