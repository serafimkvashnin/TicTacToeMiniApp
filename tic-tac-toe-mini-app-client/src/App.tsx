import { useEffect } from 'react'
import { getUserName, setChromeColors } from './telegram'
import { Screens } from './ui/Screens'
import './ui/ui.css'

const COLORS = {
  header: '#1b4fae',
  background: '#2d6cdf',
}

export default function App() {
  useEffect(() => {
    // Шапка и фон Telegram того же цвета, что и приложение
    setChromeColors(COLORS.header, COLORS.background)
  }, [])

  return (
    <div className="app">
      <header className="header">
        <span className="header-name">{getUserName()}</span>
      </header>
      <main className="content">
        <Screens />
      </main>
    </div>
  )
}
