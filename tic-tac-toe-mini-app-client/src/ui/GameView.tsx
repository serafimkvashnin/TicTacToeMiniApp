import { useEffect, useRef, useState } from 'react'
import * as gameClient from '../net/gameClient'
import type { Game, Mark, Player, Room } from '../net/gameClient'
import { haptic, openUserChat } from '../telegram'
import { Board, MarkIcon } from './Board'
import { burstFrom, EMOTES, type EmoteId } from './emotes'
import { MarqueeText } from './MarqueeText'

type Props = {
  room: Room
  game: Game
  busy: boolean
  onMove: (cell: number) => void
  onRematch: () => void
  onLeave: () => void
}

const EMOTE_COOLDOWN_MS = 1200

export function GameView({ room, game, busy, onMove, onRematch, onLeave }: Props) {
  const me = room.players[room.yourSeat]
  const opponentSeat = 1 - room.yourSeat
  const opponent = room.players[opponentSeat]
  const myMark = me?.mark as Mark
  const isMyTurn = game.status === 'Playing' && game.turn === myMark
  const isOver = game.status !== 'Playing'

  useGameHaptics(game, myMark)
  useEmoteBursts()

  return (
    <div className="panel game">
      <div className="scoreboard">
        <div className={`side side-me ${!isOver && isMyTurn ? 'active' : ''}`} data-seat={room.yourSeat}>
          {myMark && <MarkIcon mark={myMark} animate={false} />}
          <span className="side-you">Вы</span>
        </div>

        {opponent && (
          <OpponentSide
            player={opponent}
            seat={opponentSeat}
            active={!isOver && !isMyTurn}
          />
        )}
      </div>

      <div className={`game-status ${game.status === 'Won' ? (game.winner === myMark ? 'win' : 'lose') : ''}`}>
        {statusText(game, myMark)}
      </div>

      <Board game={game} canPlay={isMyTurn && !busy} waiting={!isOver && !isMyTurn} onMove={onMove} />

      {isOver && (
        <button className="button primary" disabled={busy} onClick={onRematch}>
          Ещё раз
        </button>
      )}

      <div className="game-actions">
        <button className="button leave-button" disabled={busy} onClick={onLeave}>
          Выйти из комнаты
        </button>
        <EmoteButton emote="impatient" />
      </div>

      {/* Код нужен только приватным комнатам: в остальные по коду не войти */}
      {room.kind === 'Private' && <div className="room-code-small">Комната {room.code}</div>}
    </div>
  )
}

/** Плашка соперника справа: ник прижат к правому краю, знак правее ника; с @username — ссылка на чат */
function OpponentSide({ player, seat, active }: { player: Player; seat: number; active: boolean }) {
  const content = (
    <>
      <MarqueeText text={player.name} className="side-name" />
      {player.mark && <MarkIcon mark={player.mark} animate={false} />}
    </>
  )
  const className = `side side-opponent ${active ? 'active' : ''}`

  if (!player.username) {
    return (
      <div className={className} data-seat={seat}>
        {content}
      </div>
    )
  }

  const username = player.username
  return (
    <button
      className={`${className} side-link`}
      data-seat={seat}
      title={`Написать @${username}`}
      onClick={() => openUserChat(username)}
    >
      {content}
    </button>
  )
}

function EmoteButton({ emote }: { emote: EmoteId }) {
  const [cooling, setCooling] = useState(false)

  const send = () => {
    setCooling(true)
    setTimeout(() => setCooling(false), EMOTE_COOLDOWN_MS)
    haptic.tap()
    gameClient.sendEmote(emote).catch(() => {})
  }

  return (
    <button className="button emote-button" disabled={cooling} aria-label="Поторопить соперника" onClick={send}>
      {EMOTES[emote]}
    </button>
  )
}

/** Стикер вылетает из плашки того, кто его отправил */
function useEmoteBursts() {
  useEffect(
    () =>
      gameClient.onEmote((seat, emote) => {
        if (!(emote in EMOTES)) return
        const side = document.querySelector(`.side[data-seat="${seat}"]`)
        if (side) burstFrom(side, emote as EmoteId)
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
