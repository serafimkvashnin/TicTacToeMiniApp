import { useState } from 'react'
import { t } from '../i18n'
import { prepareInvite, type Room } from '../net/gameClient'
import { shareLink, shareMessage, supportsShareMessage } from '../telegram'

const BOT_APP_URL = import.meta.env.VITE_BOT_APP_URL

type Props = {
  room: Room
  busy: boolean
  onLeave: () => void
}

/** Комната до начала партии: ждём второго игрока */
export function RoomView({ room, busy, onLeave }: Props) {
  const emptySlots = room.capacity - room.players.length
  const [preparing, setPreparing] = useState(false)

  // Карточка с кнопкой «Присоединиться» через окно «Поделиться»; если Telegram старый
  // или подготовить не вышло — обычная ссылка, которая откроет игру в комнате
  const invite = async () => {
    if (supportsShareMessage()) {
      setPreparing(true)
      try {
        shareMessage(await prepareInvite())
        return
      } catch {
        // дальше — запасной вариант со ссылкой
      } finally {
        setPreparing(false)
      }
    }
    shareLink(`${BOT_APP_URL}?startapp=${room.code}`, t.room.inviteText)
  }

  return (
    <div className="panel">
      <div className="room-code-label">{t.room.codeLabel}</div>
      <div className="room-code">{room.code}</div>

      <ul className="players">
        {/* ключ по месту, а не по id: один пользователь может занять оба места */}
        {room.players.map((player, seat) => (
          <li key={seat} className="player">
            <span className="player-name">{player.name}</span>
            {player.username && <span className="player-username">@{player.username}</span>}
            {player.isHost && <span className="badge">{t.room.host}</span>}
          </li>
        ))}
        {Array.from({ length: emptySlots }, (_, i) => (
          <li key={`empty-${i}`} className="player empty">
            {t.room.waiting}
          </li>
        ))}
      </ul>

      {/* В один ряд, чтобы плашка была той же высоты, что и в остальных разделах меню */}
      <div className="button-row">
        {BOT_APP_URL && (
          <button className="button primary" disabled={preparing} onClick={invite}>
            {t.room.invite}
          </button>
        )}
        <button className="button" disabled={busy} onClick={onLeave}>
          {t.room.leave}
        </button>
      </div>
    </div>
  )
}
