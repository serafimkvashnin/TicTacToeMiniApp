import { en } from './en'
import { ru, type Dictionary } from './ru'

export type Language = 'ru' | 'en'

const dictionaries: Record<Language, Dictionary> = { ru, en }

// Языки, носителям которых привычнее русский интерфейс, чем английский
const RUSSIAN_SPEAKING = ['ru', 'uk', 'be', 'kk']

/**
 * Язык интерфейса — язык приложения Telegram у пользователя (initData.user.language_code),
 * вне Telegram — язык браузера. Всё, кроме русскоязычных, получает английский.
 */
function detectLanguage(): Language {
  const code = window.Telegram?.WebApp?.initDataUnsafe?.user?.language_code ?? navigator.language ?? 'en'
  const base = code.toLowerCase().split('-')[0]
  return RUSSIAN_SPEAKING.includes(base) ? 'ru' : 'en'
}

export const language: Language = detectLanguage()

/** Тексты на языке пользователя: t.lobby.findOpponent, t.game.roomCode('K7MPQ') */
export const t: Dictionary = dictionaries[language]

/** Текст ошибки по коду с сервера; неизвестный код — общее «что-то пошло не так» */
export function errorText(code: string | undefined): string {
  const errors = t.errors as Record<string, string>
  return (code && errors[code]) || t.errors.generic
}

document.documentElement.lang = language
document.title = t.title
