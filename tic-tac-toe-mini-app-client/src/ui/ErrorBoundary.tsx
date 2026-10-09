import { Component, type ReactNode } from 'react'
import { t } from '../i18n'

type State = { failed: boolean }

/**
 * Если интерфейс упал, показываем понятный экран вместо пустой страницы.
 * Саму ошибку отправляет на сервер обработчик onCaughtError в main.tsx.
 */
export class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { failed: false }

  static getDerivedStateFromError(): State {
    return { failed: true }
  }

  render() {
    if (!this.state.failed) return this.props.children

    return (
      <div className="content">
        <div className="panel status">
          <p className="searching-title">{t.crash.title}</p>
          <p className="searching-hint">{t.crash.hint}</p>
          <button className="button primary" onClick={() => window.location.reload()}>
            {t.crash.restart}
          </button>
        </div>
      </div>
    )
  }
}
