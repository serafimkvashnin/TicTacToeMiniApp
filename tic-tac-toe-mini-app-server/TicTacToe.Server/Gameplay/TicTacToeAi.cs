using System.Collections.Concurrent;

namespace TicTacToe.Server.Gameplay;

public enum BotDifficulty
{
    Easy,
    Medium,
    Hard,
}

/// <summary>
/// Ход бота. Лучший ход ищется полным перебором (негамакс): для поля 3×3 это мгновенно,
/// а такой бот никогда не проигрывает. Сложность — вероятность вместо лучшего хода сделать случайный.
/// </summary>
public static class TicTacToeAi
{
    public static double MistakeChance(BotDifficulty difficulty) => difficulty switch
    {
        BotDifficulty.Easy => 0.6,
        BotDifficulty.Medium => 0.25,
        _ => 0,
    };

    public static int ChooseMove(IReadOnlyList<Mark?> board, Mark mark, BotDifficulty difficulty, Random random)
    {
        var free = Enumerable.Range(0, board.Count).Where(i => board[i] is null).ToList();
        if (free.Count == 0)
            throw new InvalidOperationException("No free cells");

        if (random.NextDouble() < MistakeChance(difficulty))
            return free[random.Next(free.Count)];

        var cells = board.ToArray();
        var scored = free.Select(cell => (Cell: cell, Score: ScoreMove(cells, cell, mark))).ToList();
        var best = scored.Max(s => s.Score);

        // Среди равных по силе ходов выбираем случайный, чтобы бот не играл каждый раз одинаково
        var bestCells = scored.Where(s => s.Score == best).Select(s => s.Cell).ToList();
        return bestCells[random.Next(bestCells.Count)];
    }

    // Оценки позиций: их не больше 3^9, поэтому каждую считаем один раз за всё время работы
    private static readonly ConcurrentDictionary<int, int> Scores = new();

    private static int ScoreMove(Mark?[] board, int cell, Mark mark)
    {
        board[cell] = mark;
        var score = -Negamax(board, TicTacToeGame.Opponent(mark));
        board[cell] = null;
        return score;
    }

    /// <summary>
    /// Оценка позиции для того, кто сейчас ходит: победа быстрее и поражение позже ценятся выше.
    /// Зависит только от позиции (не от глубины поиска), поэтому её можно запоминать.
    /// </summary>
    private static int Negamax(Mark?[] board, Mark toMove)
    {
        var key = Encode(board);
        if (Scores.TryGetValue(key, out var cached))
            return cached;

        var filled = board.Count(c => c is not null);
        int score;

        // Линию мог собрать только тот, кто ходил последним, то есть соперник toMove
        if (TicTacToeGame.FindWinningLine(board) is not null)
        {
            score = -(10 - filled);
        }
        else
        {
            score = int.MinValue;
            for (var i = 0; i < board.Length; i++)
            {
                if (board[i] is not null)
                    continue;

                board[i] = toMove;
                score = Math.Max(score, -Negamax(board, TicTacToeGame.Opponent(toMove)));
                board[i] = null;
            }

            // Свободных клеток нет и победителя нет — ничья
            if (score == int.MinValue)
                score = 0;
        }

        Scores[key] = score;
        return score;
    }

    /// <summary>Позиция как число в троичной системе. Чей ход, однозначно следует из числа знаков на поле.</summary>
    private static int Encode(Mark?[] board)
    {
        var key = 0;
        foreach (var cell in board)
            key = key * 3 + cell switch { null => 0, Mark.X => 1, _ => 2 };
        return key;
    }
}
