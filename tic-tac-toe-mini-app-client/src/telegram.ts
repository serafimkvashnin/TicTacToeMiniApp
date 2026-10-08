export const tg = window.Telegram?.WebApp

export function getUserName(): string {
  const user = tg?.initDataUnsafe?.user
  if (!user) return 'Гость'
  return user.username ? `@${user.username}` : user.first_name
}

// Подписанная строка initData — сервер проверяет её токеном бота
export function getInitData(): string {
  return tg?.initData ?? ''
}

// Параметр из ссылки t.me/<bot>/<app>?startapp=<значение>
export function getStartParam(): string | undefined {
  return tg?.initDataUnsafe?.start_param
}

export function setChromeColors(header: string, background: string) {
  tg?.setHeaderColor(header)
  tg?.setBackgroundColor(background)
}

export function shareLink(url: string, text: string) {
  const shareUrl = `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`
  if (tg) tg.openTelegramLink(shareUrl)
  else window.open(shareUrl, '_blank')
}

// Вибрация доступна только внутри Telegram; в браузере вызовы ничего не делают
export const haptic = {
  tap: () => tg?.HapticFeedback?.impactOccurred('light'),
  result: (type: 'success' | 'warning' | 'error') => tg?.HapticFeedback?.notificationOccurred(type),
}
