import { useEffect, useRef } from 'react'
import Phaser from 'phaser'
import { MainScene } from './game/MainScene'

function getUserName(): string {
  const user = window.Telegram?.WebApp?.initDataUnsafe?.user
  if (!user) return 'Гость'
  return user.username ? `@${user.username}` : user.first_name
}

export default function App() {
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const game = new Phaser.Game({
      type: Phaser.AUTO,
      parent: containerRef.current!,
      backgroundColor: '#2d6cdf',
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
