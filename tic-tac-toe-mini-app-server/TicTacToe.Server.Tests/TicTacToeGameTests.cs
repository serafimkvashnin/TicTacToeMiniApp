using TicTacToe.Server.Gameplay;

namespace TicTacToe.Server.Tests;

public class TicTacToeGameTests
{
    /// <summary>Ходы по очереди, начиная с крестиков.</summary>
    private static TicTacToeGame Play(params int[] cells)
    {
        var game = new TicTacToeGame();
        var mark = Mark.X;
        foreach (var cell in cells)
        {
            Assert.Equal(MoveError.None, game.Move(mark, cell));
            mark = mark == Mark.X ? Mark.O : Mark.X;
        }
        return game;
    }

    [Fact]
    public void X_moves_first_and_turns_alternate()
    {
        var game = new TicTacToeGame();
        Assert.Equal(Mark.X, game.Turn);
        Assert.Equal(MoveError.NotYourTurn, game.Move(Mark.O, 0));

        Assert.Equal(MoveError.None, game.Move(Mark.X, 4));
        Assert.Equal(Mark.O, game.Turn);
        Assert.Equal(Mark.X, game.Board[4]);
    }

    [Theory]
    [InlineData(new[] { 0, 3, 1, 4, 2 }, new[] { 0, 1, 2 })] // строка
    [InlineData(new[] { 1, 0, 4, 2, 7 }, new[] { 1, 4, 7 })] // столбец
    [InlineData(new[] { 0, 1, 4, 2, 8 }, new[] { 0, 4, 8 })] // главная диагональ
    [InlineData(new[] { 2, 0, 4, 1, 6 }, new[] { 2, 4, 6 })] // побочная диагональ
    public void X_wins_with_line(int[] moves, int[] expectedLine)
    {
        var game = Play(moves);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Mark.X, game.Winner);
        Assert.Equal(expectedLine, game.WinningLine);
    }

    [Fact]
    public void O_can_win()
    {
        var game = Play(0, 3, 1, 4, 8, 5);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Mark.O, game.Winner);
        Assert.Equal(new[] { 3, 4, 5 }, game.WinningLine);
    }

    [Fact]
    public void Full_board_without_line_is_draw()
    {
        // X O X
        // X O O
        // O X X
        var game = Play(0, 1, 2, 4, 3, 5, 7, 6, 8);

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Null(game.WinningLine);
    }

    [Fact]
    public void Win_on_last_cell_is_not_draw()
    {
        // X O X
        // O X O
        // O X X  — последний ход крестиков закрывает диагональ
        var game = Play(0, 1, 2, 3, 4, 5, 7, 6, 8);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Mark.X, game.Winner);
    }

    [Fact]
    public void Rejects_taken_and_out_of_range_cells()
    {
        var game = Play(4);

        Assert.Equal(MoveError.CellTaken, game.Move(Mark.O, 4));
        Assert.Equal(MoveError.InvalidCell, game.Move(Mark.O, -1));
        Assert.Equal(MoveError.InvalidCell, game.Move(Mark.O, 9));
        Assert.Equal(Mark.O, game.Turn);
    }

    [Fact]
    public void No_moves_after_game_over()
    {
        var game = Play(0, 3, 1, 4, 2);

        Assert.Equal(MoveError.GameOver, game.Move(Mark.O, 8));
        Assert.Null(game.Board[8]);
    }
}
