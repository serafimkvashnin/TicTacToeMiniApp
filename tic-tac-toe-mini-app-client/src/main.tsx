import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import { reportClientError } from './net/gameClient'
import { ErrorBoundary } from './ui/ErrorBoundary'

const tg = window.Telegram?.WebApp
tg?.ready()
tg?.expand()

// Любая ошибка у игрока попадает в серверный лог — иначе о падениях в Telegram не узнать
window.addEventListener('error', (e) => reportClientError(e.message, e.error?.stack))
window.addEventListener('unhandledrejection', (e) => {
  const reason = e.reason
  reportClientError(`Unhandled rejection: ${reason?.message ?? String(reason)}`, reason?.stack)
})

const reportReactError = (error: unknown, info: { componentStack?: string }) => {
  const err = error instanceof Error ? error : new Error(String(error))
  reportClientError(`React: ${err.message}`, `${err.stack ?? ''}\nComponent stack:${info.componentStack ?? ''}`)
}

createRoot(document.getElementById('root')!, {
  onCaughtError: reportReactError,
  onUncaughtError: reportReactError,
}).render(
  <StrictMode>
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  </StrictMode>,
)
