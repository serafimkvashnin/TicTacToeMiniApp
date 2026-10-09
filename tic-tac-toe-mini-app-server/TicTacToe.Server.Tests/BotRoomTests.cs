using Microsoft.Extensions.Options;
using TicTacToe.Server.Gameplay;
using TicTacToe.Server.Rooms;
using TicTacToe.Server.Telegram;

namespace TicTacToe.Server.Tests;

public class BotRoomTests
{
    private static readonly Player Alice = new("conn-alice", new TelegramUser(1, "Alice", null, "alice"));
    private static readonly Player Bob = new("conn-bob", new TelegramUser(2, "Bob", null, null));

    private static RoomManager CreateManager() => new(Options.Create(new RoomOptions()));

    /// <summary>Доигрывает партию: человек ставит в первую свободную клетку, бот ходит через свои задачи.</summary>
    private static RoomDto PlayToEnd(RoomManager manager, RoomDto state, Player human, Random random)
    {
        var code = state.Code;
        while (state.Game!.Status == GameStatus.Playing)
        {
            var myMark = state.Players[state.YourSeat].Mark;
            if (state.Game.Turn == myMark)
            {
                var cell = state.Game.Board.Select((m, i) => (m, i)).First(x => x.m is null).i;
                state = manager.MakeMove(human.ConnectionId, cell).For(human.ConnectionId);
            }
            else
            {
                var task = Assert.IsType<BotMoveTask>(manager.NextBotTask(code));
                state = manager.TryBotMove(task, random).For(human.ConnectionId);
            }
        }
        return state;
    }

    [Fact]
    public void Play_bot_starts_game_with_visible_bot()
    {
        var manager = CreateManager();

        var update = manager.PlayBot(Alice, BotDifficulty.Hard);

        var room = update.For(Alice.ConnectionId);
        Assert.Single(update.Views); // боту состояние не отправляется
        Assert.Equal(RoomKind.Bot, room.Kind);
        Assert.Equal(2, room.Players.Count);
        Assert.Equal("Bot", room.Players[1 - room.YourSeat].Name);
        Assert.NotNull(room.Game);
    }

    [Fact]
    public void Bot_moves_only_on_its_turn_and_stale_tasks_are_ignored()
    {
        var manager = CreateManager();
        var room = manager.PlayBot(Alice, BotDifficulty.Medium).For(Alice.ConnectionId);
        var aliceMark = room.Players[room.YourSeat].Mark;
        var random = new Random(3);

        if (room.Game!.Turn == aliceMark)
        {
            Assert.Null(manager.NextBotTask(room.Code));
            room = manager.MakeMove(Alice.ConnectionId, 4).For(Alice.ConnectionId);
        }

        var task = Assert.IsType<BotMoveTask>(manager.NextBotTask(room.Code));
        var afterBot = manager.TryBotMove(task, random).For(Alice.ConnectionId);
        Assert.Equal(room.Game!.Board.Count(c => c is not null) + 1, afterBot.Game!.Board.Count(c => c is not null));
        Assert.Equal(aliceMark, afterBot.Game.Turn);

        // Повтор той же задачи (например, двойной вызов) ничего не делает
        Assert.Null(manager.TryBotMove(task, random).Code);
    }

    [Fact]
    public void Bot_rooms_cannot_be_joined_by_code_and_vanish_when_human_leaves()
    {
        var manager = CreateManager();
        var code = manager.PlayBot(Alice, BotDifficulty.Easy).For(Alice.ConnectionId).Code;

        Assert.Throws<RoomException>(() => manager.Join(code, Bob));

        Assert.Null(manager.Leave(Alice.ConnectionId).Code);
        Assert.Null(manager.NextBotTask(code));
    }

    [Fact]
    public void Quick_play_fills_waiting_player_with_disguised_bot()
    {
        var manager = CreateManager();
        var code = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;

        var task = Assert.IsType<FillWithBotTask>(manager.NextBotTask(code));
        var room = manager.TryFillWithBot(task, new Random(5)).For(Alice.ConnectionId);

        Assert.Equal(RoomKind.Public, room.Kind);
        Assert.NotNull(room.Game);
        var opponent = room.Players[1 - room.YourSeat];
        Assert.NotEqual("Bot", opponent.Name);
        Assert.False(string.IsNullOrWhiteSpace(opponent.Name));
        Assert.Null(opponent.Username);
        Assert.True(opponent.Id > 0);
    }

    [Fact]
    public void Disguised_bot_is_not_added_if_real_player_came_first()
    {
        var manager = CreateManager();
        var code = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        var task = Assert.IsType<FillWithBotTask>(manager.NextBotTask(code));

        manager.QuickPlay(Bob);

        Assert.Null(manager.TryFillWithBot(task, new Random(1)).Code);
    }

    [Fact]
    public void Disguised_bot_is_not_added_after_player_restarted_search()
    {
        var manager = CreateManager();
        var first = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        var staleTask = Assert.IsType<FillWithBotTask>(manager.NextBotTask(first));

        // Игрок отменил поиск и начал новый — старый таймер не должен сработать
        manager.Leave(Alice.ConnectionId);
        manager.QuickPlay(Alice);

        Assert.Null(manager.TryFillWithBot(staleTask, new Random(1)).Code);
    }

    [Fact]
    public void Disguised_bot_leaving_returns_player_to_search()
    {
        var manager = CreateManager();
        var random = new Random(11);
        var code = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        var started = manager.TryFillWithBot(Assert.IsType<FillWithBotTask>(manager.NextBotTask(code)), random)
            .For(Alice.ConnectionId);

        PlayToEnd(manager, started, Alice, random);
        var afterGame = Assert.IsType<BotAfterGameTask>(manager.NextBotTask(code));
        var room = manager.TryBotAfterGame(afterGame, leave: true).For(Alice.ConnectionId);

        Assert.Single(room.Players);
        Assert.Null(room.Game);
        // Снова ждём соперника — и снова может прийти бот
        Assert.IsType<FillWithBotTask>(manager.NextBotTask(code));
    }

    [Fact]
    public void Disguised_bot_staying_starts_rematch_with_swapped_sides()
    {
        var manager = CreateManager();
        var random = new Random(12);
        var code = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        var started = manager.TryFillWithBot(Assert.IsType<FillWithBotTask>(manager.NextBotTask(code)), random)
            .For(Alice.ConnectionId);

        var finished = PlayToEnd(manager, started, Alice, random);
        var markBefore = finished.Players[finished.YourSeat].Mark;
        var room = manager.TryBotAfterGame(Assert.IsType<BotAfterGameTask>(manager.NextBotTask(code)), leave: false)
            .For(Alice.ConnectionId);

        Assert.Equal(GameStatus.Playing, room.Game!.Status);
        Assert.NotEqual(markBefore, room.Players[room.YourSeat].Mark);
    }

    /// <summary>Партия с замаскированным ботом, доведённая до хода игрока.</summary>
    private static RoomDto DisguisedGameOnHumanTurn(RoomManager manager, Random random)
    {
        var code = manager.QuickPlay(Alice).For(Alice.ConnectionId).Code;
        var room = manager.TryFillWithBot(Assert.IsType<FillWithBotTask>(manager.NextBotTask(code)), random)
            .For(Alice.ConnectionId);

        if (manager.NextBotTask(code) is BotMoveTask botFirst)
            room = manager.TryBotMove(botFirst, random).For(Alice.ConnectionId);

        return room;
    }

    [Fact]
    public void Disguised_bot_leaves_when_player_is_idle()
    {
        var manager = CreateManager();
        var room = DisguisedGameOnHumanTurn(manager, new Random(21));

        var idle = Assert.IsType<BotIdleLeaveTask>(manager.NextBotTask(room.Code));
        var afterLeave = manager.TryBotIdleLeave(idle).For(Alice.ConnectionId);

        Assert.Single(afterLeave.Players);
        Assert.Null(afterLeave.Game);
        // Игрок снова в поиске
        Assert.IsType<FillWithBotTask>(manager.NextBotTask(room.Code));
    }

    [Fact]
    public void Disguised_bot_stays_if_player_moved_in_time()
    {
        var manager = CreateManager();
        var room = DisguisedGameOnHumanTurn(manager, new Random(22));
        var idle = Assert.IsType<BotIdleLeaveTask>(manager.NextBotTask(room.Code));

        var free = room.Game!.Board.Select((m, i) => (m, i)).First(x => x.m is null).i;
        manager.MakeMove(Alice.ConnectionId, free);

        Assert.Null(manager.TryBotIdleLeave(idle).Code);
    }

    [Fact]
    public void Visible_bot_waits_for_idle_player_forever()
    {
        var manager = CreateManager();
        var room = manager.PlayBot(Alice, BotDifficulty.Easy).For(Alice.ConnectionId);
        if (manager.NextBotTask(room.Code) is BotMoveTask botFirst)
            manager.TryBotMove(botFirst, new Random(23));

        Assert.Null(manager.NextBotTask(room.Code));
    }

    [Fact]
    public void Visible_bot_never_leaves_after_game()
    {
        var manager = CreateManager();
        var random = new Random(13);
        var started = manager.PlayBot(Alice, BotDifficulty.Hard).For(Alice.ConnectionId);
        var code = started.Code;

        PlayToEnd(manager, started, Alice, random);

        Assert.Null(manager.NextBotTask(code));
    }
}
