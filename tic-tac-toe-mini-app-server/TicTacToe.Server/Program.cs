using System.Text.Json.Serialization;
using TicTacToe.Server.Hubs;
using TicTacToe.Server.Rooms;
using TicTacToe.Server.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
builder.Services.AddSingleton<TelegramAuthenticator>();
builder.Services.Configure<RoomOptions>(builder.Configuration.GetSection(RoomOptions.SectionName));
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSignalR()
    // Перечисления (X/O, статус партии) уходят клиенту строками, а не числами
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => "TicTacToe server is running");
app.MapHub<GameHub>("/hubs/game");

app.Run();
