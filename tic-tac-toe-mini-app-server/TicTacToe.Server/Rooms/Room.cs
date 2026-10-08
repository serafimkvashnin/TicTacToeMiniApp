using TicTacToe.Server.Gameplay;

namespace TicTacToe.Server.Rooms;

/// <summary>
/// Изменяемое состояние комнаты, доступ только под блокировкой <see cref="RoomManager"/>.
/// Место игрока — его индекс в <see cref="Players"/>; первый игрок — хост.
/// </summary>
internal sealed class Room(string code)
{
    public const int Capacity = 2;

    private int _version;

    public string Code { get; } = code;
    public List<Player> Players { get; } = [];
    public TicTacToeGame? Game { get; private set; }

    /// <summary>Место, которое играет крестиками в текущей партии.</summary>
    public int XSeat { get; private set; }

    public bool IsFull => Players.Count >= Capacity;

    public void StartGame(int xSeat)
    {
        XSeat = xSeat;
        Game = new TicTacToeGame();
    }

    public void AbortGame() => Game = null;

    public int SeatOf(string connectionId) => Players.FindIndex(p => p.ConnectionId == connectionId);

    public Mark MarkOf(int seat) => seat == XSeat ? Mark.X : Mark.O;

    /// <summary>Вызывается после каждого изменения: повышает версию и собирает новое состояние.</summary>
    public RoomUpdate Commit()
    {
        _version++;
        return Snapshot();
    }

    /// <summary>Текущее состояние для всех игроков, без изменения версии.</summary>
    public RoomUpdate Snapshot()
    {
        var players = Players
            .Select((p, seat) => new PlayerDto(
                p.User.Id,
                p.User.DisplayName,
                p.User.Username,
                IsHost: seat == 0,
                Mark: Game is null ? null : MarkOf(seat)))
            .ToList();

        // Копия доски: рассылка идёт уже после выхода из блокировки
        var game = Game is null
            ? null
            : new GameDto(Game.Board.ToArray(), Game.Turn, Game.Status, Game.Winner, Game.WinningLine);

        return new RoomUpdate(Players
            .Select((p, seat) => new RoomView(p.ConnectionId, new RoomDto(Code, Capacity, players, seat, _version, game)))
            .ToList());
    }
}
