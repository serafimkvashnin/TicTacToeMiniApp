namespace TicTacToe.Server.Hubs;

/// <summary>Стикеры, которые можно отправлять; картинку для каждого рисует клиент (emotes.ts).</summary>
public static class Emotes
{
    public const string Impatient = "impatient";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Impatient };
}
