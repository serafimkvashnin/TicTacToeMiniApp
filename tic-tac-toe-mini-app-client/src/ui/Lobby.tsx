import { useState, type FormEvent } from 'react'

type Props = {
  busy: boolean
  onCreate: () => void
  onJoin: (code: string) => void
}

export function Lobby({ busy, onCreate, onJoin }: Props) {
  const [code, setCode] = useState('')

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (code.trim()) onJoin(code)
  }

  return (
    <div className="panel">
      <h2>Крестики-нолики</h2>

      <button className="button primary" disabled={busy} onClick={onCreate}>
        Создать комнату
      </button>

      <div className="divider">или</div>

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
