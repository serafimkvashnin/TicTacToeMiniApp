using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TicTacToe.Server.Gameplay;

namespace TicTacToe.Server.Rooms;

/// <summary>
/// Комнаты в памяти процесса. Партия начинается, когда в комнате двое,
/// и прерывается, если кто-то вышел. Все изменения идут под одной блокировкой.
/// </summary>
public sealed class RoomManager(IOptions<RoomOptions> options)
{
    // Без похожих символов (0/O, 1/I/L), чтобы код было легко продиктовать
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;

    private readonly RoomOptions _options = options.Value;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<string, Room> _roomByConnection = new();

    // Порядок постановки в ожидание: счётчик вместо времени, чтобы порядок был строгим
    private long _waitingCounter;

    /// <summary>Приватная комната, в которую заходят только по коду.</summary>
    public RoomUpdate Create(Player host)
    {
        lock (_lock)
        {
            return CreateRoom(host, isPublic: false).Commit();
        }
    }

    public RoomUpdate Join(string code, Player player)
    {
        code = code.Trim().ToUpperInvariant();

        lock (_lock)
        {
            if (!_rooms.TryGetValue(code, out var room))
                throw new RoomException("Комната не найдена");

            if (!CanMatch(room, player))
                throw new RoomException("Вы уже в этой комнате");

            if (room.IsFull)
                throw new RoomException("Комната уже заполнена");

            AddPlayer(room, player);
            return room.Commit();
        }
    }

    /// <summary>
    /// Случайный соперник: садимся в публичную комнату, где дольше всех ждут игрока,
    /// а если таких нет — создаём свою и ждём, пока кто-то зайдёт так же.
    /// </summary>
    public RoomUpdate QuickPlay(Player player)
    {
        lock (_lock)
        {
            var room = _rooms.Values
                .Where(r => r.IsPublic && !r.IsFull && CanMatch(r, player))
                .MinBy(r => r.WaitingSince);

            if (room is null)
                return CreateRoom(player, isPublic: true).Commit();

            AddPlayer(room, player);
            return room.Commit();
        }
    }

    /// <returns>Состояние для оставшихся игроков; пустое, если игрок не был в комнате или комната удалена.</returns>
    public RoomUpdate Leave(string connectionId)
    {
        lock (_lock)
        {
            if (!_roomByConnection.Remove(connectionId, out var room))
                return RoomUpdate.Empty;

            room.Players.RemoveAll(p => p.ConnectionId == connectionId);
            // Без соперника партия не продолжается: новая начнётся, когда кто-то зайдёт
            room.AbortGame();

            if (room.Players.Count == 0)
            {
                _rooms.Remove(room.Code);
                return RoomUpdate.Empty;
            }

            // Оставшийся игрок снова ждёт соперника — в конце очереди подбора
            room.WaitingSince = ++_waitingCounter;
            return room.Commit();
        }
    }

    public RoomUpdate MakeMove(string connectionId, int cell)
    {
        lock (_lock)
        {
            var room = RoomOf(connectionId);
            if (room.Game is null)
                throw new RoomException("Игра ещё не началась");

            var error = room.Game.Move(room.MarkOf(room.SeatOf(connectionId)), cell);
            if (error != MoveError.None)
            {
                throw new RoomException(error switch
                {
                    MoveError.GameOver => "Партия уже закончилась",
                    MoveError.NotYourTurn => "Сейчас ход соперника",
                    MoveError.CellTaken => "Клетка уже занята",
                    _ => "Недопустимый ход",
                });
            }

            return room.Commit();
        }
    }

    /// <summary>Новая партия после окончания текущей; стороны меняются, чтобы первым ходил другой игрок.</summary>
    public RoomUpdate Rematch(string connectionId)
    {
        lock (_lock)
        {
            var room = RoomOf(connectionId);
            if (room.Game is null)
                throw new RoomException("Нужен второй игрок");

            // Если реванш уже запросил соперник, партия идёт — просто возвращаем состояние
            if (room.Game.Status == GameStatus.Playing)
                return room.Snapshot();

            room.StartGame(xSeat: 1 - room.XSeat);
            return room.Commit();
        }
    }

    private Room CreateRoom(Player host, bool isPublic)
    {
        var room = new Room(GenerateCode(), isPublic) { WaitingSince = ++_waitingCounter };
        room.Players.Add(host);
        _rooms[room.Code] = room;
        _roomByConnection[host.ConnectionId] = room;
        return room;
    }

    private void AddPlayer(Room room, Player player)
    {
        room.Players.Add(player);
        _roomByConnection[player.ConnectionId] = room;

        if (room.IsFull)
            room.StartGame(xSeat: Random.Shared.Next(Room.Capacity));
    }

    /// <summary>Один пользователь Telegram не может играть сам с собой, если это не разрешено настройкой.</summary>
    private bool CanMatch(Room room, Player player) =>
        _options.AllowSelfPlay || room.Players.All(p => p.User.Id != player.User.Id);

    private Room RoomOf(string connectionId) =>
        _roomByConnection.TryGetValue(connectionId, out var room)
            ? room
            : throw new RoomException("Вы не в комнате");

    private string GenerateCode()
    {
        string code;
        do code = new string(RandomNumberGenerator.GetItems<char>(CodeAlphabet, CodeLength));
        while (_rooms.ContainsKey(code));
        return code;
    }
}
