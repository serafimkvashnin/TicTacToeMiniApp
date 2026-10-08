const tg = window.Telegram?.WebApp

export function getUserName(): string {
  const user = tg?.initDataUnsafe?.user
  if (!user) return 'Гость'
  return user.username ? `@${user.username}` : user.first_name
}

// Отступ сверху, под которым начинается свободная зона:
// системный (Dynamic Island, статус-бар) + зона кнопок Telegram в fullscreen
export function getTopInset(): number {
  if (!tg) return 0
  return (tg.safeAreaInset?.top ?? 0) + (tg.contentSafeAreaInset?.top ?? 0)
}

const INSET_EVENTS = ['safeAreaChanged', 'contentSafeAreaChanged', 'fullscreenChanged', 'viewportChanged'] as const

export function onInsetsChange(handler: () => void): () => void {
  if (!tg) return () => {}
  // onEvent перегружен по имени события, поэтому приводим тип для общего обработчика
  const on = tg.onEvent as (event: string, cb: () => void) => void
  const off = tg.offEvent as (event: string, cb: () => void) => void
  INSET_EVENTS.forEach((e) => on(e, handler))
  return () => INSET_EVENTS.forEach((e) => off(e, handler))
}

export function setChromeColors(header: string, background: string) {
  tg?.setHeaderColor(header)
  tg?.setBackgroundColor(background)
}
