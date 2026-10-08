import type { Game, Mark } from '../net/gameClient'

type Props = {
  game: Game
  canPlay: boolean
  onMove: (cell: number) => void
}

export function Board({ game, canPlay, onMove }: Props) {
  return (
    <div className={`board ${canPlay ? 'playable' : ''}`}>
      {game.board.map((mark, cell) => (
        <button
          key={cell}
          className={`cell ${game.winningLine?.includes(cell) ? 'winning' : ''}`}
          disabled={!canPlay || mark !== null}
          aria-label={mark ?? `Клетка ${cell + 1}`}
          onClick={() => onMove(cell)}
        >
          {mark && <MarkIcon mark={mark} />}
        </button>
      ))}
      {game.winningLine && game.winner && <WinLine line={game.winningLine} winner={game.winner} />}
    </div>
  )
}

/** Знаки рисуются линиями: pathLength=100 позволяет анимировать «прорисовку» через stroke-dashoffset */
export function MarkIcon({ mark, animate = true }: { mark: Mark; animate?: boolean }) {
  return (
    <svg className={`mark mark-${mark.toLowerCase()} ${animate ? 'animate' : ''}`} viewBox="0 0 100 100">
      {mark === 'X' ? (
        <>
          <line x1="24" y1="24" x2="76" y2="76" pathLength={100} />
          <line x1="76" y1="24" x2="24" y2="76" pathLength={100} className="second" />
        </>
      ) : (
        <circle cx="50" cy="50" r="28" pathLength={100} />
      )}
    </svg>
  )
}

/** Линия через центры выигравших клеток, немного выходит за крайние */
function WinLine({ line, winner }: { line: number[]; winner: Mark }) {
  const center = (cell: number) => ({ x: (cell % 3) + 0.5, y: Math.floor(cell / 3) + 0.5 })
  const from = center(line[0])
  const to = center(line[line.length - 1])
  const dx = to.x - from.x
  const dy = to.y - from.y
  const length = Math.hypot(dx, dy)
  const extend = 0.35 / length

  return (
    <svg className={`win-line win-line-${winner.toLowerCase()}`} viewBox="0 0 3 3">
      <line
        x1={from.x - dx * extend}
        y1={from.y - dy * extend}
        x2={to.x + dx * extend}
        y2={to.y + dy * extend}
        pathLength={100}
      />
    </svg>
  )
}
