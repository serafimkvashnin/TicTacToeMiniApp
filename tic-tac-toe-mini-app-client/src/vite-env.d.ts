/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Адрес сервера, например https://api.example.com. Пусто — тот же origin */
  readonly VITE_SERVER_URL?: string
  /** Ссылка на Mini App, например https://t.me/MyBot/app. Нужна для кнопки «Пригласить» */
  readonly VITE_BOT_APP_URL?: string
}

interface Window {
  /** Заставка студии из index.html уже начала исчезать */
  __splashDone?: boolean
}
