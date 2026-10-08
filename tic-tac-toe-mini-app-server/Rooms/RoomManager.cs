using System.Security.Cryptography;

namespace TicTacToe.Server.Rooms;

/// <summary>
/// Комнаты в памяти процесса. Первый игрок в списке — хост:
/// если хост выходит, хостом становится оставшийся игрок.
/// </summary>
public sealed class RoomManager
{
    public const int Capacity = 2;

    // Без похожих символов (0/O, 1/I/L), чтобы код было легко продиктовать
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 5;

    private readonly Lock _lock = new();
    private readonly Dictionary<string, List<Player>> _rooms = new();
    private readonly Dictionary<string, string> _roomByConnection = new();

    public RoomDto Create(Player host)
    {
        lock (_lock)
        {
            var code = GenerateCode();
            _rooms[code] = [host];
            _roomByConnection[host.ConnectionId] = code;
            return ToDto(code);
        }
    }

    public JoinResult Join(string code, Player player)
    {
        code = code.Trim().ToUpperInvariant();

        lock (_lock)
        {
            if (!_rooms.TryGetValue(code, out var players))
                return new JoinResult(JoinStatus.NotFound);

            if (players.Any(p => p.User.Id == player.User.Id))
                return new JoinResult(JoinStatus.AlreadyInRoom);

            if (players.Count >= Capacity)
                return new JoinResult(JoinStatus.Full);

            players.Add(player);
            _roomByConnection[player.ConnectionId] = code;
            return new JoinResult(JoinStatus.Joined, ToDto(code));
        }
    }

    public LeaveResult? Leave(string connectionId)
    {
        lock (_lock)
        {
            if (!_roomByConnection.Remove(connectionId, out var code))
                return null;

            var players = _rooms[code];
            players.RemoveAll(p => p.ConnectionId == connectionId);

            if (players.Count == 0)
            {
                _rooms.Remove(code);
                return new LeaveResult(code, null);
            }

            return new LeaveResult(code, ToDto(code));
        }
    }

    private string GenerateCode()
    {
        string code;
        do code = new string(RandomNumberGenerator.GetItems<char>(CodeAlphabet, CodeLength));
        while (_rooms.ContainsKey(code));
        return code;
    }

    private RoomDto ToDto(string code)
    {
        var players = _rooms[code]
            .Select((p, i) => new PlayerDto(p.User.Id, p.User.DisplayName, p.User.Username, IsHost: i == 0))
            .ToList();
        return new RoomDto(code, Capacity, players);
    }
}
