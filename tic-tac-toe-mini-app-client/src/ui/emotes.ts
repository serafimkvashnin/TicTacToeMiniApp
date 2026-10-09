/** Стикеры: id совпадает с серверным, картинка — эмодзи */
export const EMOTES = {
  impatient: '😤',
} as const

export type EmoteId = keyof typeof EMOTES

const GRAVITY = 1500 // px/s²

let layer: HTMLDivElement | null = null

// Отдельный слой поверх всего: частицы летают по всему экрану и не мешают кликам
function getLayer() {
  if (!layer) {
    layer = document.createElement('div')
    layer.className = 'emote-layer'
    document.body.appendChild(layer)
  }
  return layer
}

/**
 * Один стикер выпрыгивает из случайного места элемента и падает под гравитацией.
 * Одно нажатие — один стикер: хочешь больше — жми чаще.
 */
export function popFrom(element: Element, emote: EmoteId) {
  const rect = element.getBoundingClientRect()
  const x = rect.left + rect.width * (0.2 + Math.random() * 0.6)
  const y = rect.top + rect.height / 2
  spawn(x, y, EMOTES[emote])
}

function spawn(x: number, y: number, emoji: string) {
  const size = 26 + Math.random() * 20
  const el = document.createElement('span')
  el.className = 'emote-particle'
  el.textContent = emoji
  el.style.fontSize = `${size}px`
  getLayer().appendChild(el)

  // Выпрыгивает вверх и в сторону, крутится, падает
  let vx = (Math.random() - 0.5) * 360
  let vy = -(450 + Math.random() * 350)
  const spin = (Math.random() - 0.5) * 720
  let px = x
  let py = y
  let angle = 0
  let last = performance.now()

  const step = (now: number) => {
    const dt = Math.min(0.05, (now - last) / 1000)
    last = now
    vy += GRAVITY * dt
    vx *= 0.995
    px += vx * dt
    py += vy * dt
    angle += spin * dt
    el.style.transform = `translate(${px - size / 2}px, ${py - size / 2}px) rotate(${angle}deg)`

    if (py - size > window.innerHeight) el.remove()
    else requestAnimationFrame(step)
  }
  requestAnimationFrame(step)
}
