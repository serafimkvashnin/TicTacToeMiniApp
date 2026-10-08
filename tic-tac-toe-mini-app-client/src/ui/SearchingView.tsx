type Props = {
  busy: boolean
  onCancel: () => void
}

/** Публичная комната, в которой мы ждём случайного соперника */
export function SearchingView({ busy, onCancel }: Props) {
  return (
    <div className="panel status">
      <div className="spinner" aria-hidden="true" />
      <p className="searching-title">Ищем соперника…</p>
      <p className="searching-hint">Игра начнётся, как только кто-то нажмёт «Найти соперника»</p>
      <button className="button" disabled={busy} onClick={onCancel}>
        Отмена
      </button>
    </div>
  )
}
