import { useEffect, useRef, useState, useSyncExternalStore } from 'react'
import * as game from '../net/gameClient'
import { getStartParam } from '../telegram'
import { Lobby } from './Lobby'
import { RoomView } from './RoomView'

export function Overlay() {
  const { status, room } = useSyncExternalStore(game.subscribe, game.getState)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const startParamUsed = useRef(false)

  const run = async (action: () => Promise<void>) => {
    setBusy(true)
    setError(null)
    try {
      await action()
    } catch (e) {
      setError(game.errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => {
    game.connect()
  }, [])

  // Открыли по ссылке-приглашению: сразу заходим в комнату
  useEffect(() => {
    const code = getStartParam()
    if (status !== 'connected' || !code || startParamUsed.current) return
    startParamUsed.current = true
    run(() => game.joinRoom(code))
  }, [status])

  return (
    <div className="overlay">
      {status === 'connecting' && <div className="panel status">Подключение…</div>}

      {status === 'disconnected' && (
        <div className="panel status">
          <p>Нет соединения с сервером</p>
          <button className="button primary" onClick={() => game.connect()}>
            Повторить
          </button>
        </div>
      )}

      {status === 'connected' &&
        (room ? (
          <RoomView room={room} busy={busy} onLeave={() => run(game.leaveRoom)} />
        ) : (
          <Lobby busy={busy} onCreate={() => run(game.createRoom)} onJoin={(code) => run(() => game.joinRoom(code))} />
        ))}

      {error && <div className="error">{error}</div>}
    </div>
  )
}
