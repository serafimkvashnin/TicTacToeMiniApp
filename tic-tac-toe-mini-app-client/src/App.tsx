import { useEffect, useRef } from 'react'
import Phaser from 'phaser'
import { MainScene, COLORS, HEADER_HEIGHT } from './game/MainScene'
import { getUserName, setChromeColors } from './telegram'
import { Overlay } from './ui/Overlay'
import './ui/ui.css'

export default function App() {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    setChromeColors(COLORS.header, COLORS.background)
    // React-интерфейс начинается под полосой с ником, которую рисует сцена
    document.documentElement.style.setProperty('--hud-height', `${HEADER_HEIGHT}px`)

    const game = new Phaser.Game({
      type: Phaser.AUTO,
      parent: containerRef.current!,
      backgroundColor: COLORS.background,
      scale: {
        mode: Phaser.Scale.RESIZE,
        width: window.innerWidth,
        height: window.innerHeight,
      },
      scene: new MainScene(getUserName()),
    })

    // StrictMode в dev монтирует компонент дважды — уничтожаем игру при размонтировании
    return () => game.destroy(true)
  }, [])

  return (
    <div style={{ position: 'relative', width: '100%', height: '100%' }}>
      <div ref={containerRef} style={{ width: '100%', height: '100%' }} />
      <Overlay />
    </div>
  )
}
