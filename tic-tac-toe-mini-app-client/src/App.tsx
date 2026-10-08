import { useEffect, useRef } from 'react'
import Phaser from 'phaser'
import { MainScene, COLORS } from './game/MainScene'
import { getUserName, setChromeColors } from './telegram'

export default function App() {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    setChromeColors(COLORS.header, COLORS.background)

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

  return <div ref={containerRef} style={{ width: '100%', height: '100%' }} />
}
