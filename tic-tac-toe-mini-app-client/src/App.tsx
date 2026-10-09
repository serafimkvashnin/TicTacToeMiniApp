import { useEffect } from 'react'
import { setChromeColors } from './telegram'
import { Screens } from './ui/Screens'
import './ui/ui.css'

const BACKGROUND = '#2d6cdf'

export default function App() {
  useEffect(() => {
    // Шапка и фон Telegram того же цвета, что и фон приложения — верх экрана сливается с игрой
    setChromeColors(BACKGROUND, BACKGROUND)
  }, [])

  return (
    <div className="app">
      <main className="content">
        <Screens />
      </main>
    </div>
  )
}
