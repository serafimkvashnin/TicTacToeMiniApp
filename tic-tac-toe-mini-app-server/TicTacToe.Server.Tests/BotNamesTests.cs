using TicTacToe.Server.Bots;
using Xunit.Abstractions;

namespace TicTacToe.Server.Tests;

public class BotNamesTests(ITestOutputHelper output)
{
    [Fact]
    public void Generated_users_have_names_but_no_username()
    {
        var random = new Random(2026);
        var users = Enumerable.Range(0, 200).Select(_ => BotNames.CreateUser(random)).ToList();

        Assert.All(users, u =>
        {
            Assert.False(string.IsNullOrWhiteSpace(u.DisplayName));
            Assert.DoesNotContain("@", u.DisplayName);
            Assert.Null(u.Username);
            Assert.InRange(u.Id, 100_000_000, 7_000_000_000);
        });
        Assert.True(users.Select(u => u.DisplayName).Distinct().Count() > 150, "names should vary");

        foreach (var user in users.Take(20))
            output.WriteLine(user.DisplayName);
    }
}
