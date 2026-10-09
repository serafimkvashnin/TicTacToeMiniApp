import { useEffect, useRef, useState, useSyncExternalStore } from 'react'
import * as game from '../net/gameClient'
import { getStartParam } from '../telegram'
import { GameView } from './GameView'
import { Lobby } from './Lobby'
import { RoomView } from './RoomView'
import { SearchingView } from './SearchingView'

const TOAST_DURATION_MS = 3500

export function Screens() {
  const { status, room, notice } = useSyncExternalStore(game.subscribe, game.getState)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const startParamUsed = useRef(false)

  const run = async (action: () => Promise<void>) => {
    setBusy(true)
    setError(null)
    game.dismissNotice()
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

  // Уведомления и ошибки — временные подсказки, а не состояние экрана: скрываем сами
  useEffect(() => {
    if (!notice) return
    const timer = setTimeout(game.dismissNotice, TOAST_DURATION_MS)
    return () => clearTimeout(timer)
  }, [notice])

  useEffect(() => {
    if (!error) return
    const timer = setTimeout(() => setError(null), TOAST_DURATION_MS)
    return () => clearTimeout(timer)
  }, [error])

  // Открыли по ссылке-приглашению: сразу заходим в комнату
  useEffect(() => {
    const code = getStartParam()
    if (status !== 'connected' || !code || startParamUsed.current) return
    startParamUsed.current = true
    run(() => game.joinRoom(code))
  }, [status])

  const leave = () => run(game.leaveRoom)

  return (
    <>
      {status === 'connecting' && <div className="panel status">Подключение…</div>}

      {status === 'disconnected' && (
        <div className="panel status">
          <p>Нет соединения с сервером</p>
          <button className="button primary" onClick={() => game.connect()}>
            Повторить
          </button>
        </div>
      )}

      {status === 'connected' && !room && (
        <Lobby
          busy={busy}
          onQuickPlay={() => run(game.quickPlay)}
          onPlayBot={(difficulty) => run(() => game.playBot(difficulty))}
          onCreate={() => run(game.createRoom)}
          onJoin={(code) => run(() => game.joinRoom(code))}
        />
      )}

      {status === 'connected' && room && !room.game && room.kind === 'Public' && (
        <SearchingView busy={busy} onCancel={leave} />
      )}

      {status === 'connected' && room && !room.game && room.kind === 'Private' && (
        <RoomView room={room} busy={busy} onLeave={leave} />
      )}

      {status === 'connected' && room?.game && (
        <GameView
          room={room}
          game={room.game}
          busy={busy}
          onMove={(cell) => run(() => game.makeMove(cell))}
          onRematch={() => run(game.rematch)}
          onLeave={leave}
        />
      )}

      {notice && <div className="toast">{notice}</div>}
      {error && <div className="toast error">{error}</div>}
    </>
  )
}
