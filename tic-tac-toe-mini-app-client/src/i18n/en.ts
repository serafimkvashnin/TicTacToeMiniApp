import type { Dictionary } from './ru'

export const en: Dictionary = {
  title: 'Tic-Tac-Toe',

  lobby: {
    findOpponent: 'Find an opponent',
    playBot: 'Play with a bot',
    orWithFriend: 'or play with a friend',
    createRoom: 'Create a room',
    roomCodePlaceholder: 'Room code',
    join: 'Join',
    botDifficulty: 'Bot difficulty',
    back: 'Back',
    difficulty: {
      Easy: 'Easy',
      Medium: 'Medium',
      Hard: 'Hard',
    },
  },

  links: {
    channel: 'Our channel',
    leaderboard: 'Leaderboard',
  },

  connection: {
    connecting: 'Connecting…',
    lost: 'No connection to the server',
    retry: 'Retry',
  },

  searching: {
    title: 'Looking for an opponent…',
    hint: 'The game starts as soon as someone taps “Find an opponent”',
    cancel: 'Cancel',
  },

  room: {
    codeLabel: 'Room code',
    host: 'host',
    waiting: 'Waiting for an opponent…',
    invite: 'Invite',
    leave: 'Leave',
    inviteText: 'Up for a game of tic-tac-toe?',
  },

  game: {
    opponent: 'Opponent',
    youPlayAs: 'You play as',
    yourTurn: 'Your turn',
    opponentTurn: "Opponent's turn",
    win: 'You won!',
    lose: 'You lost',
    draw: 'Draw',
    rematch: 'Play again',
    leaveRoom: 'Leave the room',
    roomCode: (code) => `Room ${code}`,
    writeTo: (username) => `Message @${username}`,
    hurry: 'Hurry up the opponent',
    cell: (n) => `Cell ${n}`,
  },

  notices: {
    opponentLeftSearching: 'Opponent left — looking for a new one',
    opponentLeft: 'Opponent left the room',
  },

  crash: {
    title: 'Something went wrong',
    hint: "We already know about the error. Try restarting the game.",
    restart: 'Restart',
  },

  errors: {
    generic: 'Something went wrong',
    'room.notFound': 'Room not found',
    'room.alreadyJoined': "You're already in this room",
    'room.full': 'The room is already full',
    'room.notInRoom': "You're not in a room",
    'room.needOpponent': 'A second player is needed',
    'game.notStarted': "The game hasn't started yet",
    'move.gameOver': 'The game is already over',
    'move.notYourTurn': "It's your opponent's turn",
    'move.cellTaken': 'This cell is already taken',
    'move.invalid': 'Invalid move',
    'invite.notAllowed': 'You can only invite to your own room while it waits for an opponent',
    'invite.telegramOnly': 'Invites are only available in Telegram',
    'invite.failed': "Couldn't prepare the invite",
    'emote.unknown': 'Unknown sticker',
  },
}
