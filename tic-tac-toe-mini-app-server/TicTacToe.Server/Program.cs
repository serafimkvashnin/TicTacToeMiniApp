using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TicTacToe.Server.Bots;
using TicTacToe.Server.Hubs;
using TicTacToe.Server.Logging;
using TicTacToe.Server.Rooms;
using TicTacToe.Server.Telegram;

// Временный логгер на время запуска: ошибки конфигурации тоже должны куда-то попасть
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddFileLogging();

    builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
    builder.Services.AddSingleton<TelegramAuthenticator>();
    builder.Services.Configure<RoomOptions>(builder.Configuration.GetSection(RoomOptions.SectionName));
    builder.Services.AddSingleton<RoomManager>();
    builder.Services.AddSingleton<BotDriver>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<EmoteLimiter>();
    builder.Services.AddSignalR(options => options.AddFilter<HubErrorLoggingFilter>())
        // Перечисления (X/O, статус партии) уходят клиенту строками, а не числами
        .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

    var app = builder.Build();

    // Исключения в фоновых задачах, которые никто не дождался, иначе пропали бы молча
    TaskScheduler.UnobservedTaskException += (_, e) =>
        Log.Error(e.Exception, "Unobserved task exception");

    app.UseCors();

    app.MapGet("/", () => "TicTacToe server is running");
    app.MapHub<GameHub>("/hubs/game");

    Log.ForContext("SourceContext", "Program").Information("Server starting, logs in {LogsDir}", Path.Combine(app.Environment.ContentRootPath, "logs"));
    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
