using Bogus;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Bots;

/// <summary>
/// Правдоподобные профили для замаскированных ботов: русские и английские имена, иногда с фамилией,
/// иногда никнейм вместо имени. @username у ботов нет — как у игроков, которые его скрыли:
/// настоящий юзернейм можно было бы попробовать найти в Telegram.
/// </summary>
public static class BotNames
{
    private static readonly Faker Ru = new("ru");
    private static readonly Faker En = new("en");

    // Faker внутри не потокобезопасен
    private static readonly Lock Lock = new();

    /// <summary>Явный бот из меню «Играть с ботом».</summary>
    public static TelegramUser Visible { get; } = new(0, "Bot", null, null);

    /// <param name="playerLanguage">
    /// Язык живого игрока: русскоязычным чаще попадаются русские имена, остальным — английские,
    /// как было бы при подборе среди людей.
    /// </param>
    public static TelegramUser CreateUser(Random random, string? playerLanguage = null)
    {
        lock (Lock)
        {
            // Похоже на настоящие id пользователей Telegram
            var id = random.NextInt64(100_000_000, 7_000_000_000);

            // Кто-то пишет в имени никнейм
            if (random.NextDouble() < 0.15)
                return new TelegramUser(id, En.Internet.UserName().Replace('.', '_'), null, null);

            var russianShare = Languages.IsRussianSpeaking(playerLanguage) ? 0.8 : 0.15;
            var faker = random.NextDouble() < russianShare ? Ru : En;
            var gender = random.Next(2) == 0 ? Bogus.DataSets.Name.Gender.Male : Bogus.DataSets.Name.Gender.Female;

            var firstName = faker.Name.FirstName(gender);
            var lastName = random.NextDouble() < 0.4 ? faker.Name.LastName(gender) : null;
            return new TelegramUser(id, firstName, lastName, null);
        }
    }
}
