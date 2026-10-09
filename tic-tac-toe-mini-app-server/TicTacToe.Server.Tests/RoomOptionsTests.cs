using System.Text;
using Microsoft.Extensions.Configuration;
using TicTacToe.Server.Gameplay;
using TicTacToe.Server.Rooms;

namespace TicTacToe.Server.Tests;

public class RoomOptionsTests
{
    [Fact]
    public void Pick_stays_within_range()
    {
        var range = TimeRange.Seconds(2, 5);
        var random = new Random(1);

        var picks = Enumerable.Range(0, 1000).Select(_ => range.Pick(random)).ToList();

        Assert.All(picks, p => Assert.InRange(p, range.Min, range.Max));
        Assert.True(picks.Max() - picks.Min() > TimeSpan.FromSeconds(2.5), "values should spread across the range");
    }

    [Fact]
    public void Pick_returns_min_when_range_is_empty_or_inverted()
    {
        var random = new Random(1);
        Assert.Equal(TimeSpan.FromSeconds(3), TimeRange.Seconds(3, 3).Pick(random));
        Assert.Equal(TimeSpan.FromSeconds(3), TimeRange.Seconds(3, 1).Pick(random));
    }

    [Fact]
    public void Ranges_bind_from_appsettings()
    {
        const string json = """
            {
              "Rooms": {
                "QuickPlayBotDelay": { "Min": "00:00:05", "Max": "00:00:08" },
                "BotLeaveChance": 0.9
              }
            }
            """;
        var config = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();

        var options = config.GetSection(RoomOptions.SectionName).Get<RoomOptions>()!;

        Assert.Equal(TimeSpan.FromSeconds(5), options.QuickPlayBotDelay.Min);
        Assert.Equal(TimeSpan.FromSeconds(8), options.QuickPlayBotDelay.Max);
        Assert.Equal(0.9, options.BotLeaveChance);
        // Не заданное в конфиге остаётся значением по умолчанию
        Assert.Equal(TimeSpan.FromSeconds(20), options.BotIdleTimeout.Min);
    }

    [Fact]
    public void Shipped_appsettings_have_valid_ranges()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var options = new ConfigurationBuilder().AddJsonFile(path).Build()
            .GetSection(RoomOptions.SectionName).Get<RoomOptions>()!;

        foreach (var range in new[] { options.QuickPlayBotDelay, options.BotMoveDelay, options.BotAfterGameDelay, options.BotIdleTimeout })
        {
            Assert.True(range.Min > TimeSpan.Zero);
            Assert.True(range.Max >= range.Min);
        }
        Assert.InRange(options.BotLeaveChance, 0, 1);
        Assert.Equal(new[] { BotDifficulty.Medium, BotDifficulty.Hard }, options.DisguisedBotDifficulties);
    }

    [Fact]
    public void Disguised_bot_is_never_easy_by_default()
    {
        var options = new RoomOptions();
        var random = new Random(1);

        var picks = Enumerable.Range(0, 500).Select(_ => options.PickDisguisedDifficulty(random)).ToHashSet();

        Assert.Equal(new HashSet<BotDifficulty> { BotDifficulty.Medium, BotDifficulty.Hard }, picks);
    }

    [Fact]
    public void Disguised_difficulties_from_config_replace_defaults()
    {
        const string json = """{ "Rooms": { "DisguisedBotDifficulties": [ "Hard" ] } }""";
        var config = new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();
        var options = config.GetSection(RoomOptions.SectionName).Get<RoomOptions>()!;
        var random = new Random(1);

        Assert.All(Enumerable.Range(0, 100), _ => Assert.Equal(BotDifficulty.Hard, options.PickDisguisedDifficulty(random)));
    }
}
