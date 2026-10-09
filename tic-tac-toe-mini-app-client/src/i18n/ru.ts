// Русский — эталонный словарь: остальные языки обязаны повторять его структуру (тип Dictionary)

export const ru = {
  title: 'Крестики-нолики',

  lobby: {
    findOpponent: 'Найти соперника',
    playBot: 'Играть с ботом',
    orWithFriend: 'или сыграть с другом',
    createRoom: 'Создать комнату',
    roomCodePlaceholder: 'Код комнаты',
    join: 'Войти',
    botDifficulty: 'Сложность бота',
    back: 'Назад',
    difficulty: {
      Easy: 'Лёгкий',
      Medium: 'Средний',
      Hard: 'Сложный',
    },
  },

  links: {
    channel: 'Наш канал',
    leaderboard: 'Лидеры',
  },

  connection: {
    connecting: 'Подключение…',
    lost: 'Нет соединения с сервером',
    retry: 'Повторить',
  },

  searching: {
    title: 'Ищем соперника…',
    hint: 'Игра начнётся, как только кто-то нажмёт «Найти соперника»',
    cancel: 'Отмена',
  },

  room: {
    codeLabel: 'Код комнаты',
    host: 'хост',
    waiting: 'Ожидание соперника…',
    invite: 'Пригласить',
    leave: 'Выйти',
    inviteText: 'Сыграем в крестики-нолики?',
  },

  game: {
    opponent: 'Соперник',
    youPlayAs: 'Вы играете за',
    yourTurn: 'Ваш ход',
    opponentTurn: 'Ход соперника',
    win: 'Победа!',
    lose: 'Поражение',
    draw: 'Ничья',
    rematch: 'Ещё раз',
    leaveRoom: 'Выйти из комнаты',
    roomCode: (code: string) => `Комната ${code}`,
    writeTo: (username: string) => `Написать @${username}`,
    hurry: 'Поторопить соперника',
    cell: (n: number) => `Клетка ${n}`,
  },

  notices: {
    opponentLeftSearching: 'Соперник вышел — ищем нового',
    opponentLeft: 'Соперник покинул комнату',
  },

  crash: {
    title: 'Что-то пошло не так',
    hint: 'Мы уже знаем об ошибке. Попробуйте перезапустить игру.',
    restart: 'Перезапустить',
  },

  // Ключи — коды ошибок, которые присылает сервер (ErrorCodes.cs)
  errors: {
    generic: 'Что-то пошло не так',
    'room.notFound': 'Комната не найдена',
    'room.alreadyJoined': 'Вы уже в этой комнате',
    'room.full': 'Комната уже заполнена',
    'room.notInRoom': 'Вы не в комнате',
    'room.needOpponent': 'Нужен второй игрок',
    'game.notStarted': 'Игра ещё не началась',
    'move.gameOver': 'Партия уже закончилась',
    'move.notYourTurn': 'Сейчас ход соперника',
    'move.cellTaken': 'Клетка уже занята',
    'move.invalid': 'Недопустимый ход',
    'invite.notAllowed': 'Пригласить можно только в свою комнату, пока она ждёт соперника',
    'invite.telegramOnly': 'Приглашение доступно только в Telegram',
    'invite.failed': 'Не удалось подготовить приглашение',
    'emote.unknown': 'Неизвестный стикер',
  },
}

export type Dictionary = typeof ru
