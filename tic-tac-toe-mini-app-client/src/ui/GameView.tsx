import { useEffect, useRef } from 'react'
import * as gameClient from '../net/gameClient'
import type { Game, Mark, Player, Room } from '../net/gameClient'
import { haptic, openTelegramUsername } from '../telegram'
import { Board, MarkIcon } from './Board'
import { EMOTES, popFrom, type EmoteId } from './emotes'
import { MarqueeText } from './MarqueeText'

type Props = {
  room: Room
  game: Game
  busy: boolean
  onMove: (cell: number) => void
  onRematch: () => void
  onLeave: () => void
}

export function GameView({ room, game, busy, onMove, onRematch, onLeave }: Props) {
  const me = room.players[room.yourSeat]
  const opponentSeat = 1 - room.yourSeat
  const opponent = room.players[opponentSeat]
  const myMark = me?.mark as Mark
  const isMyTurn = game.status === 'Playing' && game.turn === myMark
  const isOver = game.status !== 'Playing'

  useGameHaptics(game, myMark)
  useEmotePops()

  return (
    <>
      {opponent && <OpponentCard player={opponent} seat={opponentSeat} />}

      <div className="panel game">
        <div className={`game-status ${game.status === 'Won' ? (game.winner === myMark ? 'win' : 'lose') : ''}`}>
          {statusText(game, myMark)}
        </div>

        {myMark && (
          <div className="you-play">
            Вы играете за <MarkIcon mark={myMark} animate={false} />
          </div>
        )}

        <Board game={game} canPlay={isMyTurn && !busy} waiting={!isOver && !isMyTurn} onMove={onMove} />

        {isOver && (
          <button className="button primary" disabled={busy} onClick={onRematch}>
            Ещё раз
          </button>
        )}

        {/* Код нужен только приватным комнатам: в остальные по коду не войти */}
        {room.kind === 'Private' && <div className="room-code-small">Комната {room.code}</div>}
      </div>

      {/* Отдельными островками под полем, чтобы не нажать случайно во время игры */}
      <div className="game-actions">
        <button className="island-button leave-island" disabled={busy} onClick={onLeave}>
          Выйти из комнаты
        </button>
        <EmoteButton emote="impatient" seat={room.yourSeat} />
      </div>
    </>
  )
}

/**
 * Карточка соперника над полем. С публичным @username имя синее, как ссылка, и открывает чат;
 * без него (или у бота) — обычный текст.
 */
function OpponentCard({ player, seat }: { player: Player; seat: number }) {
  const content = (
    <>
      <span className="opponent-caption">Соперник</span>
      <MarqueeText text={player.name} className="opponent-name" />
    </>
  )

  if (!player.username) {
    return (
      <div className="panel opponent-card" data-emote-seat={seat}>
        {content}
      </div>
    )
  }

  const username = player.username
  return (
    <button
      className="panel opponent-card opponent-link"
      data-emote-seat={seat}
      title={`Написать @${username}`}
      onClick={() => openTelegramUsername(username)}
    >
      {content}
    </button>
  )
}

/** Свои стикеры вылетают прямо из этой кнопки — под пальцем */
function EmoteButton({ emote, seat }: { emote: EmoteId; seat: number }) {
  const send = () => {
    haptic.tap()
    gameClient.sendEmote(emote).catch(() => {})
  }

  return (
    <button
      className="island-button emote-island"
      data-emote-seat={seat}
      aria-label="Поторопить соперника"
      onClick={send}
    >
      {EMOTES[emote]}
    </button>
  )
}

/** Стикер вылетает из элемента отправителя: у соперника — его карточка, у нас — кнопка стикера */
function useEmotePops() {
  useEffect(
    () =>
      gameClient.onEmote((seat, emote) => {
        if (!(emote in EMOTES)) return
        const origin = document.querySelector(`[data-emote-seat="${seat}"]`)
        if (origin) popFrom(origin, emote as EmoteId)
      }),
    [],
  )
}

function statusText(game: Game, myMark: Mark): string {
  if (game.status === 'Draw') return 'Ничья'
  if (game.status === 'Won') return game.winner === myMark ? 'Победа!' : 'Поражение'
  return game.turn === myMark ? 'Ваш ход' : 'Ход соперника'
}

/** Лёгкая вибрация на каждый ход и отдельный сигнал в конце партии */
function useGameHaptics(game: Game, myMark: Mark) {
  const moves = game.board.filter(Boolean).length
  const prevMoves = useRef(moves)

  useEffect(() => {
    if (moves > prevMoves.current) {
      if (game.status === 'Won') haptic.result(game.winner === myMark ? 'success' : 'error')
      else if (game.status === 'Draw') haptic.result('warning')
      else haptic.tap()
    }
    prevMoves.current = moves
  }, [moves, game.status, game.winner, myMark])
}
