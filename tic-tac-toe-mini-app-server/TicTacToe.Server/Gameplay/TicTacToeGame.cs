namespace TicTacToe.Server.Gameplay;

public enum Mark
{
    X,
    O,
}

public enum GameStatus
{
    Playing,
    Won,
    Draw,
}

public enum MoveError
{
    None,
    GameOver,
    NotYourTurn,
    InvalidCell,
    CellTaken,
}

/// <summary>Правила крестиков-ноликов 3×3 без привязки к игрокам и сети. Крестики ходят первыми.</summary>
public sealed class TicTacToeGame
{
    public const int CellCount = 9;

    private static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6],
    ];

    private readonly Mark?[] _board = new Mark?[CellCount];

    public IReadOnlyList<Mark?> Board => _board;
    public Mark Turn { get; private set; } = Mark.X;
    public GameStatus Status { get; private set; } = GameStatus.Playing;
    public Mark? Winner { get; private set; }
    public IReadOnlyList<int>? WinningLine { get; private set; }

    public MoveError Move(Mark mark, int cell)
    {
        if (Status != GameStatus.Playing)
            return MoveError.GameOver;
        if (mark != Turn)
            return MoveError.NotYourTurn;
        if (cell is < 0 or >= CellCount)
            return MoveError.InvalidCell;
        if (_board[cell] is not null)
            return MoveError.CellTaken;

        _board[cell] = mark;

        var line = FindWinningLine(_board);
        if (line is not null)
        {
            Status = GameStatus.Won;
            Winner = mark;
            WinningLine = line;
        }
        else if (_board.All(c => c is not null))
        {
            Status = GameStatus.Draw;
        }
        else
        {
            Turn = Opponent(mark);
        }

        return MoveError.None;
    }

    /// <summary>Заполненная одним знаком линия или null.</summary>
    public static int[]? FindWinningLine(IReadOnlyList<Mark?> board) =>
        Lines.FirstOrDefault(l => board[l[0]] is not null && board[l[0]] == board[l[1]] && board[l[1]] == board[l[2]]);

    public static Mark Opponent(Mark mark) => mark == Mark.X ? Mark.O : Mark.X;
}
