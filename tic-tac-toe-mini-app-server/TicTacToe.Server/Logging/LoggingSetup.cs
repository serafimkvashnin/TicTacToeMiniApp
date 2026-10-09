using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Events;

namespace TicTacToe.Server.Logging;

/// <summary>
/// Логи в консоль и в файлы logs/ рядом с проектом, новый файл каждый день:
/// server-*.log — всё, errors-*.log — только предупреждения и ошибки, чтобы быстро найти падения.
/// Уровни — в секции "Serilog" appsettings.json.
/// </summary>
public static class LoggingSetup
{
    private const string Template =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    private const int RetainedDays = 14;

    public static void AddFileLogging(this WebApplicationBuilder builder)
    {
        var logsDir = Path.Combine(builder.Environment.ContentRootPath, "logs");

        builder.Services.AddSerilog((services, config) => config
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            // Отказ по правилам игры («клетка занята») — не ошибка сервера, в логах ему не место
            .Filter.ByExcluding(e => e.Exception is HubException)
            // Упавший метод хаба уже записан HubErrorLoggingFilter вместе с пользователем — дубль без контекста не нужен
            .Filter.ByExcluding(e => e.MessageTemplate.Text.StartsWith("Failed to invoke hub method"))
            .WriteTo.Console(outputTemplate: Template)
            .WriteTo.File(
                Path.Combine(logsDir, "server-.log"),
                outputTemplate: Template,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedDays)
            .WriteTo.File(
                Path.Combine(logsDir, "errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Warning,
                outputTemplate: Template,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedDays));
    }
}
