using TicTacToe.Server.Gameplay;

namespace TicTacToe.Server.Tests;

public class TicTacToeAiTests
{
    private static Mark?[] Board(string rows) =>
        rows.Replace("/", "").Select(c => c switch { 'X' => Mark.X, 'O' => (Mark?)Mark.O, _ => null }).ToArray();

    [Fact]
    public void Hard_takes_immediate_win()
    {
        // X X .
        // O O .
        // . . .
        var board = Board("XX./OO./...");
        for (var seed = 0; seed < 20; seed++)
            Assert.Equal(2, TicTacToeAi.ChooseMove(board, Mark.X, BotDifficulty.Hard, new Random(seed)));
    }

    [Fact]
    public void Hard_blocks_opponent_win()
    {
        // X X .
        // O . .
        // . . .   — нолики обязаны закрыть клетку 2
        var board = Board("XX./O../...");
        for (var seed = 0; seed < 20; seed++)
            Assert.Equal(2, TicTacToeAi.ChooseMove(board, Mark.O, BotDifficulty.Hard, new Random(seed)));
    }

    [Theory]
    [InlineData(Mark.X)]
    [InlineData(Mark.O)]
    public void Hard_never_loses_against_random_player(Mark botMark)
    {
        var random = new Random(42);
        for (var i = 0; i < 300; i++)
        {
            var game = new TicTacToeGame();
            while (game.Status == GameStatus.Playing)
            {
                var free = Enumerable.Range(0, 9).Where(c => game.Board[c] is null).ToList();
                var cell = game.Turn == botMark
                    ? TicTacToeAi.ChooseMove(game.Board, botMark, BotDifficulty.Hard, random)
                    : free[random.Next(free.Count)];
                Assert.Equal(MoveError.None, game.Move(game.Turn, cell));
            }

            Assert.NotEqual(TicTacToeGame.Opponent(botMark), game.Winner);
        }
    }

    [Fact]
    public void Two_hard_bots_always_draw()
    {
        var random = new Random(7);
        for (var i = 0; i < 50; i++)
        {
            var game = new TicTacToeGame();
            while (game.Status == GameStatus.Playing)
                game.Move(game.Turn, TicTacToeAi.ChooseMove(game.Board, game.Turn, BotDifficulty.Hard, random));

            Assert.Equal(GameStatus.Draw, game.Status);
        }
    }

    [Fact]
    public void Easier_bots_lose_more_often_to_hard_bot()
    {
        int Losses(BotDifficulty difficulty)
        {
            var random = new Random(1);
            var losses = 0;
            for (var i = 0; i < 400; i++)
            {
                var game = new TicTacToeGame();
                var weak = i % 2 == 0 ? Mark.X : Mark.O;
                while (game.Status == GameStatus.Playing)
                {
                    var level = game.Turn == weak ? difficulty : BotDifficulty.Hard;
                    game.Move(game.Turn, TicTacToeAi.ChooseMove(game.Board, game.Turn, level, random));
                }
                if (game.Winner == TicTacToeGame.Opponent(weak))
                    losses++;
            }
            return losses;
        }

        var easy = Losses(BotDifficulty.Easy);
        var medium = Losses(BotDifficulty.Medium);
        var hard = Losses(BotDifficulty.Hard);

        Assert.True(easy > medium, $"easy {easy} vs medium {medium}");
        Assert.True(medium > hard, $"medium {medium} vs hard {hard}");
        Assert.Equal(0, hard);
    }
}
