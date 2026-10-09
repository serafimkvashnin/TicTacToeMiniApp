import { t } from '../i18n'

type Props = {
  busy: boolean
  onCancel: () => void
}

/** Публичная комната, в которой мы ждём случайного соперника */
export function SearchingView({ busy, onCancel }: Props) {
  return (
    <div className="panel status">
      <div className="spinner" aria-hidden="true" />
      <p className="searching-title">{t.searching.title}</p>
      <p className="searching-hint">{t.searching.hint}</p>
      <button className="button" disabled={busy} onClick={onCancel}>
        {t.searching.cancel}
      </button>
    </div>
  )
}
