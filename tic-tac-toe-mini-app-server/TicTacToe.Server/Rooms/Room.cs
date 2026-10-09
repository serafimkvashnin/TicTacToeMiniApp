using TicTacToe.Server.Gameplay;

namespace TicTacToe.Server.Rooms;

/// <summary>
/// Изменяемое состояние комнаты, доступ только под блокировкой <see cref="RoomManager"/>.
/// Место игрока — его индекс в <see cref="Players"/>; первый игрок — хост.
/// </summary>
internal sealed class Room(string code, RoomKind kind)
{
    public const int Capacity = 2;

    public string Code { get; } = code;
    public RoomKind Kind { get; } = kind;
    public bool IsPublic => Kind == RoomKind.Public;

    /// <summary>С какого момента комната ждёт соперника; при подборе первым берётся тот, кто ждёт дольше.</summary>
    public long WaitingSince { get; set; }

    public int Version { get; private set; }

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

    /// <summary>Игрок, чей сейчас ход, или null, если партия не идёт.</summary>
    public Player? PlayerToMove =>
        Game is { Status: GameStatus.Playing } game ? Players[game.Turn == Mark.X ? XSeat : 1 - XSeat] : null;

    /// <summary>Вызывается после каждого изменения: повышает версию и собирает новое состояние.</summary>
    public RoomUpdate Commit()
    {
        Version++;
        return Snapshot();
    }

    /// <summary>Текущее состояние для всех живых игроков, без изменения версии.</summary>
    public RoomUpdate Snapshot()
    {
        // Боты выглядят как обычные игроки: в данных нет признака, что это бот
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

        // Ботам состояние не отправляем — у них нет подключения
        return new RoomUpdate(Code, Players
            .Select((p, seat) => (Player: p, Seat: seat))
            .Where(x => !x.Player.IsBot)
            .Select(x => new RoomView(x.Player.ConnectionId, new RoomDto(Code, Capacity, Kind, players, x.Seat, Version, game)))
            .ToList());
    }
}
