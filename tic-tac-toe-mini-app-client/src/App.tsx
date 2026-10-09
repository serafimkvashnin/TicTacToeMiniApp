import { useEffect } from 'react'
import { afterSplash, setChromeColors } from './telegram'
import { Screens } from './ui/Screens'
import './ui/ui.css'

const BACKGROUND = '#2d6cdf'

export default function App() {
  // Шапка и фон Telegram того же цвета, что и фон приложения — верх экрана сливается с игрой.
  // Пока идёт заставка студии, шапка в её цвете
  useEffect(() => afterSplash(() => setChromeColors(BACKGROUND, BACKGROUND)), [])

  return (
    <div className="app">
      <main className="content">
        <Screens />
      </main>
    </div>
  )
}
