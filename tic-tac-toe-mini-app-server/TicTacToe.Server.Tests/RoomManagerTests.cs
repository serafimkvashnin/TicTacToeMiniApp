using Microsoft.Extensions.Options;
using TicTacToe.Server.Gameplay;
using TicTacToe.Server.Rooms;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Tests;

public class RoomManagerTests
{
    private static readonly Player Alice = new("conn-alice", new TelegramUser(1, "Alice", null, "alice"));
    private static readonly Player Bob = new("conn-bob", new TelegramUser(2, "Bob", null, null));
    private static readonly Player Carol = new("conn-carol", new TelegramUser(3, "Carol", null, null));

    private static RoomManager CreateManager(bool allowSelfPlay = false) =>
        new(Options.Create(new RoomOptions { AllowSelfPlay = allowSelfPlay }));

    /// <summary>Комната с Alice и Bob; возвращает подключение, которое играет крестиками.</summary>
    private static (RoomManager Manager, string Code, Player X, Player O) StartGame()
    {
        var manager = CreateManager();
        var code = manager.Create(Alice).For(Alice.ConnectionId).Code;
        var room = manager.Join(code, Bob).For(Bob.ConnectionId);
        var aliceIsX = room.Players[0].Mark == Mark.X;
        return (manager, code, aliceIsX ? Alice : Bob, aliceIsX ? Bob : Alice);
    }

    [Fact]
    public void Game_starts_when_second_player_joins()
    {
        var manager = CreateManager();
        var created = manager.Create(Alice).For(Alice.ConnectionId);
        Assert.Null(created.Game);

        var update = manager.Join(created.Code.ToLowerInvariant(), Bob);

        var forAlice = update.For(Alice.ConnectionId);
        var forBob = update.For(Bob.ConnectionId);
        Assert.Equal(0, forAlice.YourSeat);
        Assert.Equal(1, forBob.YourSeat);
        Assert.NotNull(forBob.Game);
        Assert.Equal(GameStatus.Playing, forBob.Game.Status);
        Assert.Equal(new Mark?[] { Mark.X, Mark.O }.Order(), forBob.Players.Select(p => p.Mark).Order());
        Assert.True(forBob.Version > created.Version);
    }

    [Fact]
    public void Join_errors()
    {
        var manager = CreateManager();
        var code = manager.Create(Alice).For(Alice.ConnectionId).Code;

        Assert.Equal("Комната не найдена", Assert.Throws<RoomException>(() => manager.Join("ZZZZZ", Bob)).Message);

        var aliceAgain = Alice with { ConnectionId = "conn-alice-2" };
        Assert.Equal("Вы уже в этой комнате", Assert.Throws<RoomException>(() => manager.Join(code, aliceAgain)).Message);

        manager.Join(code, Bob);
        Assert.Equal("Комната уже заполнена", Assert.Throws<RoomException>(() => manager.Join(code, Carol)).Message);
    }

    [Fact]
    public void Self_play_uses_seats_not_user_ids()
    {
        var manager = CreateManager(allowSelfPlay: true);
        var aliceAgain = Alice with { ConnectionId = "conn-alice-2" };
        var code = manager.Create(Alice).For(Alice.ConnectionId).Code;
        var room = manager.Join(code, aliceAgain).For(aliceAgain.ConnectionId);

        var (x, o) = room.Players[0].Mark == Mark.X ? (Alice, aliceAgain) : (aliceAgain, Alice);

        Assert.Throws<RoomException>(() => manager.MakeMove(o.ConnectionId, 0));
        manager.MakeMove(x.ConnectionId, 0);
        var afterO = manager.MakeMove(o.ConnectionId, 1).For(x.ConnectionId);
        Assert.Equal(new Mark?[] { Mark.X, Mark.O }, afterO.Game!.Board.Take(2));
    }

    [Fact]
    public void Moves_follow_turn_order()
    {
        var (manager, _, x, o) = StartGame();

        Assert.Equal("Сейчас ход соперника", Assert.Throws<RoomException>(() => manager.MakeMove(o.ConnectionId, 0)).Message);

        var update = manager.MakeMove(x.ConnectionId, 4);
        Assert.Equal(Mark.X, update.For(o.ConnectionId).Game!.Board[4]);
        Assert.Equal(Mark.O, update.For(o.ConnectionId).Game!.Turn);

        Assert.Equal("Клетка уже занята", Assert.Throws<RoomException>(() => manager.MakeMove(o.ConnectionId, 4)).Message);
    }

    [Fact]
    public void Snapshot_board_is_a_copy()
    {
        var (manager, _, x, o) = StartGame();
        var before = manager.MakeMove(x.ConnectionId, 0).For(x.ConnectionId);

        manager.MakeMove(o.ConnectionId, 1);

        Assert.Null(before.Game!.Board[1]);
    }

    [Fact]
    public void Rematch_swaps_sides_and_is_idempotent()
    {
        var (manager, _, x, o) = StartGame();
        // Пока партия идёт, реванш ничего не меняет
        Assert.Equal(GameStatus.Playing, manager.Rematch(o.ConnectionId).For(o.ConnectionId).Game!.Status);

        foreach (var (player, cell) in new[] { (x, 0), (o, 3), (x, 1), (o, 4), (x, 2) })
            manager.MakeMove(player.ConnectionId, cell);

        var rematch = manager.Rematch(o.ConnectionId).For(o.ConnectionId);
        Assert.Equal(GameStatus.Playing, rematch.Game!.Status);
        Assert.All(rematch.Game.Board, Assert.Null);
        Assert.Equal(Mark.X, rematch.Players[rematch.YourSeat].Mark);

        // Второй игрок нажал «Ещё раз» одновременно — стороны не меняются повторно
        var again = manager.Rematch(x.ConnectionId).For(o.ConnectionId);
        Assert.Equal(Mark.X, again.Players[again.YourSeat].Mark);
        Assert.Equal(rematch.Version, again.Version);
    }

    [Fact]
    public void Leaving_aborts_game_and_new_player_restarts_it()
    {
        var (manager, code, x, o) = StartGame();
        manager.MakeMove(x.ConnectionId, 0);

        var afterLeave = manager.Leave(x.ConnectionId).For(o.ConnectionId);
        Assert.Null(afterLeave.Game);
        Assert.Single(afterLeave.Players);
        Assert.True(afterLeave.Players[0].IsHost);
        Assert.Equal("Игра ещё не началась", Assert.Throws<RoomException>(() => manager.MakeMove(o.ConnectionId, 1)).Message);

        var withCarol = manager.Join(code, Carol).For(Carol.ConnectionId);
        Assert.NotNull(withCarol.Game);
        Assert.All(withCarol.Game.Board, Assert.Null);
    }

    [Fact]
    public void Quick_play_creates_public_room_then_matches_next_player()
    {
        var manager = CreateManager();

        var waiting = manager.QuickPlay(Alice).For(Alice.ConnectionId);
        Assert.True(waiting.IsPublic);
        Assert.Null(waiting.Game);

        var matched = manager.QuickPlay(Bob).For(Bob.ConnectionId);
        Assert.Equal(waiting.Code, matched.Code);
        Assert.NotNull(matched.Game);
        Assert.Equal(2, matched.Players.Count);
    }

    [Fact]
    public void Quick_play_ignores_private_rooms()
    {
        var manager = CreateManager();
        var privateCode = manager.Create(Alice).For(Alice.ConnectionId).Code;

        var room = manager.QuickPlay(Bob).For(Bob.ConnectionId);

        Assert.NotEqual(privateCode, room.Code);
        Assert.True(room.IsPublic);
        Assert.Null(room.Game);
    }

    [Fact]
    public void Quick_play_matches_longest_waiting_room_first()
    {
        var manager = CreateManager();
        var dave = new Player("conn-dave", new TelegramUser(4, "Dave", null, null));

        var aliceRoom = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        manager.QuickPlay(Bob); // Bob садится к Alice
        var carolRoom = manager.QuickPlay(Carol).For(Carol.ConnectionId).Code; // Carol ждёт
        manager.Leave(Bob.ConnectionId); // теперь Alice тоже ждёт, но позже Carol

        Assert.NotEqual(aliceRoom, carolRoom);
        Assert.Equal(carolRoom, manager.QuickPlay(dave).For(dave.ConnectionId).Code);
    }

    [Fact]
    public void Quick_play_does_not_match_user_with_self_unless_allowed()
    {
        var aliceAgain = Alice with { ConnectionId = "conn-alice-2" };

        var strict = CreateManager();
        var first = strict.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        Assert.NotEqual(first, strict.QuickPlay(aliceAgain).For(aliceAgain.ConnectionId).Code);

        var relaxed = CreateManager(allowSelfPlay: true);
        var own = relaxed.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        var self = relaxed.QuickPlay(aliceAgain).For(aliceAgain.ConnectionId);
        Assert.Equal(own, self.Code);
        Assert.NotNull(self.Game);
    }

    [Fact]
    public void Public_room_becomes_searchable_again_when_opponent_leaves()
    {
        var manager = CreateManager();
        var code = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        manager.QuickPlay(Bob);

        manager.Leave(Bob.ConnectionId);
        var withCarol = manager.QuickPlay(Carol).For(Carol.ConnectionId);

        Assert.Equal(code, withCarol.Code);
        Assert.NotNull(withCarol.Game);
    }

    [Fact]
    public void Last_player_leaving_deletes_room()
    {
        var manager = CreateManager();
        var code = manager.Create(Alice).For(Alice.ConnectionId).Code;

        Assert.Empty(manager.Leave(Alice.ConnectionId).Views);
        Assert.Empty(manager.Leave(Alice.ConnectionId).Views);
        Assert.Throws<RoomException>(() => manager.Join(code, Bob));
    }
}
