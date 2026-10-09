import { useState, type FormEvent } from 'react'
import type { BotDifficulty } from '../net/gameClient'

type Props = {
  busy: boolean
  onQuickPlay: () => void
  onPlayBot: (difficulty: BotDifficulty) => void
  onCreate: () => void
  onJoin: (code: string) => void
}

const DIFFICULTIES: { value: BotDifficulty; label: string }[] = [
  { value: 'Easy', label: 'Лёгкий' },
  { value: 'Medium', label: 'Средний' },
  { value: 'Hard', label: 'Сложный' },
]

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
        <div className="divider">Сложность бота</div>

        {DIFFICULTIES.map(({ value, label }) => (
          <button key={value} className="button primary" disabled={busy} onClick={() => onPlayBot(value)}>
            {label}
          </button>
        ))}

        <button className="button" disabled={busy} onClick={() => setChoosingBot(false)}>
          Назад
        </button>
      </div>
    )
  }

  return (
    <div className="panel">
      <button className="button primary" disabled={busy} onClick={onQuickPlay}>
        Найти соперника
      </button>

      <button className="button" disabled={busy} onClick={() => setChoosingBot(true)}>
        Играть с ботом
      </button>

      <div className="divider">или сыграть с другом</div>

      <button className="button" disabled={busy} onClick={onCreate}>
        Создать комнату
      </button>

      <form className="join-form" onSubmit={submit}>
        <input
          className="input"
          placeholder="Код комнаты"
          value={code}
          maxLength={5}
          autoCapitalize="characters"
          autoComplete="off"
          onChange={(e) => setCode(e.target.value.toUpperCase())}
        />
        <button className="button" type="submit" disabled={busy || !code.trim()}>
          Войти
        </button>
      </form>
    </div>
  )
}
