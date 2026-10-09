import { useState, type FormEvent } from 'react'
import { t } from '../i18n'
import type { BotDifficulty } from '../net/gameClient'

type Props = {
  busy: boolean
  onQuickPlay: () => void
  onPlayBot: (difficulty: BotDifficulty) => void
  onCreate: () => void
  onJoin: (code: string) => void
}

const DIFFICULTIES: BotDifficulty[] = ['Easy', 'Medium', 'Hard']

export function Lobby({ busy, onQuickPlay, onPlayBot, onCreate, onJoin }: Props) {
  const [code, setCode] = useState('')
  const [choosingBot, setChoosingBot] = useState(false)

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (code.trim()) onJoin(code)
  }

  if (choosingBot) {
    return (
      <div className="panel">
        <div className="divider">{t.lobby.botDifficulty}</div>

        {DIFFICULTIES.map((difficulty) => (
          <button key={difficulty} className="button primary" disabled={busy} onClick={() => onPlayBot(difficulty)}>
            {t.lobby.difficulty[difficulty]}
          </button>
        ))}

        <button className="button" disabled={busy} onClick={() => setChoosingBot(false)}>
          {t.lobby.back}
        </button>
      </div>
    )
  }

  return (
    <div className="panel">
      <button className="button primary" disabled={busy} onClick={onQuickPlay}>
        {t.lobby.findOpponent}
      </button>

      <button className="button" disabled={busy} onClick={() => setChoosingBot(true)}>
        {t.lobby.playBot}
      </button>

      <div className="divider">{t.lobby.orWithFriend}</div>

      <button className="button" disabled={busy} onClick={onCreate}>
        {t.lobby.createRoom}
      </button>

      <form className="join-form" onSubmit={submit}>
        <input
          className="input"
          placeholder={t.lobby.roomCodePlaceholder}
          value={code}
          maxLength={5}
          autoCapitalize="characters"
          autoComplete="off"
          onChange={(e) => setCode(e.target.value.toUpperCase())}
        />
        <button className="button" type="submit" disabled={busy || !code.trim()}>
          {t.lobby.join}
        </button>
      </form>
    </div>
  )
}
