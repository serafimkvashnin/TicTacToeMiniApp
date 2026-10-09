import { t } from '../i18n'

/** Лого и название игры над главной плашкой */
export function GameTitle() {
  return (
    <div className="game-title">
      <Logo />
      <h1 className="game-title-text">{t.title}</h1>
    </div>
  )
}

/**
 * Иконка-плитка: мини-поле с выигрышной диагональю крестиков.
 * Знаки нарисованы так же, как в игре, и прорисовываются по очереди при открытии.
 */
function Logo() {
  // Центры клеток поля 3×3 внутри viewBox 0..96 с отступом 18
  const c = (i: number) => 18 + 10 + i * 20

  const x = (col: number, row: number, delay: number) => (
    <g className="logo-mark logo-x" style={{ animationDelay: `${delay}s` }}>
      <line x1={c(col) - 6} y1={c(row) - 6} x2={c(col) + 6} y2={c(row) + 6} pathLength={100} />
      <line x1={c(col) + 6} y1={c(row) - 6} x2={c(col) - 6} y2={c(row) + 6} pathLength={100} />
    </g>
  )
  const o = (col: number, row: number, delay: number) => (
    <circle
      className="logo-mark logo-o"
      style={{ animationDelay: `${delay}s` }}
      cx={c(col)}
      cy={c(row)}
      r={6.5}
      pathLength={100}
    />
  )

  return (
    <svg className="game-logo" viewBox="0 0 96 96" aria-hidden="true">
      <rect x="2" y="2" width="92" height="92" rx="24" fill="#fff" />

      {/* сетка поля */}
      <g className="logo-grid">
        <line x1="38" y1="20" x2="38" y2="76" />
        <line x1="58" y1="20" x2="58" y2="76" />
        <line x1="20" y1="38" x2="76" y2="38" />
        <line x1="20" y1="58" x2="76" y2="58" />
      </g>

      {x(0, 0, 0.1)}
      {o(2, 0, 0.25)}
      {x(1, 1, 0.4)}
      {o(0, 2, 0.55)}
      {x(2, 2, 0.7)}

      <line className="logo-win" x1="22" y1="22" x2="74" y2="74" pathLength={100} />
    </svg>
  )
}
