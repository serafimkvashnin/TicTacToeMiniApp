import type { Room } from '../net/gameClient'
import { shareLink } from '../telegram'

const BOT_APP_URL = import.meta.env.VITE_BOT_APP_URL

type Props = {
  room: Room
  busy: boolean
  onLeave: () => void
}

export function RoomView({ room, busy, onLeave }: Props) {
  const emptySlots = room.capacity - room.players.length

  const invite = () =>
    shareLink(`${BOT_APP_URL}?startapp=${room.code}`, 'Сыграем в крестики-нолики?')

  return (
    <div className="panel">
      <div className="room-code-label">Код комнаты</div>
      <div className="room-code">{room.code}</div>

      <ul className="players">
        {/* ключ по месту, а не по id: один пользователь может занять оба места */}
        {room.players.map((player, seat) => (
          <li key={seat} className="player">
            <span className="player-name">{player.name}</span>
            {player.username && <span className="player-username">@{player.username}</span>}
            {player.isHost && <span className="badge">хост</span>}
          </li>
        ))}
        {Array.from({ length: emptySlots }, (_, i) => (
          <li key={`empty-${i}`} className="player empty">
            Ожидание соперника…
          </li>
        ))}
      </ul>

      {BOT_APP_URL && emptySlots > 0 && (
        <button className="button primary" onClick={invite}>
          Пригласить
        </button>
      )}

      <button className="button" disabled={busy} onClick={onLeave}>
        Выйти из комнаты
      </button>
    </div>
  )
}
