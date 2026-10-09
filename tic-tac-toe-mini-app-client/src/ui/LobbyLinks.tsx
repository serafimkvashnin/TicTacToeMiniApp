import type { ReactNode } from 'react'
import { t } from '../i18n'
import { haptic, openTelegramUsername } from '../telegram'

const CHANNEL_USERNAME = 'NamingIssues'

// Таблица лидеров ещё не готова: плитка свёрстана, но скрыта
const SHOW_LEADERBOARD = false

/** Плитки под главной плашкой: делят ширину поровну, одна плитка — на всю ширину */
export function LobbyLinks() {
  const tiles: ReactNode[] = []

  if (SHOW_LEADERBOARD) {
    tiles.push(<Tile key="leaderboard" icon={<TrophyIcon />} label={t.links.leaderboard} onClick={() => {}} />)
  }

  tiles.push(
    <Tile
      key="channel"
      icon={<TelegramIcon />}
      label={t.links.channel}
      onClick={() => openTelegramUsername(CHANNEL_USERNAME)}
    />,
  )

  return <div className="lobby-links">{tiles}</div>
}

function Tile({ icon, label, onClick }: { icon: ReactNode; label: string; onClick: () => void }) {
  return (
    <button
      className="link-tile"
      onClick={() => {
        haptic.tap()
        onClick()
      }}
    >
      {icon}
      <span className="link-tile-label">{label}</span>
    </button>
  )
}

function TelegramIcon() {
  return (
    <svg className="link-tile-icon" viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="12" r="12" fill="#2aabee" />
      <path
        fill="#fff"
        d="M5.4 11.8c3.5-1.5 5.8-2.5 7-3 3.3-1.4 4-1.6 4.5-1.6.1 0 .3 0 .5.2.1.1.2.3.2.4v.6c-.2 2-1 6.8-1.4 9-.2.9-.5 1.2-.8 1.3-.7.1-1.2-.5-1.9-.9l-2.6-1.8c-1.2-.8-.4-1.2.3-1.9.2-.2 3.1-2.8 3.1-3.1 0 0 0-.1-.1-.2h-.3c-.1 0-2 1.3-5.7 3.8-.5.4-1 .5-1.4.5-.5 0-1.4-.3-2-.5-.8-.3-1.5-.4-1.4-.9 0-.2.4-.5 1-.9Z"
      />
    </svg>
  )
}

function TrophyIcon() {
  return (
    // Кубок уже квадрата: обрезаем поля, чтобы визуально он был одного размера с иконкой канала
    <svg className="link-tile-icon" viewBox="3 2.5 18 19" aria-hidden="true">
      <path
        fill="#f5b301"
        d="M7 3h10v2h3v3a4 4 0 0 1-4 4h-.3A5 5 0 0 1 13 14.9V17h3v2H8v-2h3v-2.1A5 5 0 0 1 8.3 12H8a4 4 0 0 1-4-4V5h3V3Zm0 4H6v1a2 2 0 0 0 1 1.7V7Zm10 0v2.7A2 2 0 0 0 18 8V7h-1Z"
      />
      <rect x="7" y="19" width="10" height="2" rx="1" fill="#d99a00" />
    </svg>
  )
}
