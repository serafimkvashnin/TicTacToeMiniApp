import { useEffect, useRef } from 'react'
import type { Game, Mark, Room } from '../net/gameClient'
import { haptic } from '../telegram'
import { Board, MarkIcon } from './Board'

type Props = {
  room: Room
  game: Game
  busy: boolean
  onMove: (cell: number) => void
  onRematch: () => void
  onLeave: () => void
}

export function GameView({ room, game, busy, onMove, onRematch, onLeave }: Props) {
  const myMark = room.players[room.yourSeat]?.mark as Mark
  const isMyTurn = game.status === 'Playing' && game.turn === myMark
  const isOver = game.status !== 'Playing'

  useGameHaptics(game, myMark)

  return (
    <div className="panel game">
      <div className="scoreboard">
        {/* ключ по месту, а не по id: один пользователь может занять оба места */}
        {room.players.map((player, seat) => (
          <div
            key={seat}
            className={`side ${!isOver && game.turn === player.mark ? 'active' : ''}`}
          >
            {player.mark && <MarkIcon mark={player.mark} animate={false} />}
            <span className="side-name">{player.name}</span>
            {seat === room.yourSeat && <span className="side-you">вы</span>}
          </div>
        ))}
      </div>

      <div className={`game-status ${game.status === 'Won' ? (game.winner === myMark ? 'win' : 'lose') : ''}`}>
        {statusText(game, myMark)}
      </div>

      <Board game={game} canPlay={isMyTurn && !busy} onMove={onMove} />

      {isOver && (
        <button className="button primary" disabled={busy} onClick={onRematch}>
          Ещё раз
        </button>
      )}

      <button className="button" disabled={busy} onClick={onLeave}>
        Выйти из комнаты
      </button>

      {/* Код нужен только приватным комнатам: в остальные по коду не войти */}
      {room.kind === 'Private' &&<div className="room-code-small">Комната {room.code}</div>}
    </div>
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
