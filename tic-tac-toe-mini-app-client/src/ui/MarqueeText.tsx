import { useLayoutEffect, useRef, useState, type CSSProperties } from 'react'

/** Текст в одну строку; если не помещается — плавно прокручивается туда и обратно */
export function MarqueeText({ text, className = '' }: { text: string; className?: string }) {
  const outer = useRef<HTMLSpanElement>(null)
  const inner = useRef<HTMLSpanElement>(null)
  const [overflow, setOverflow] = useState(0)

  useLayoutEffect(() => {
    const measure = () => {
      if (outer.current && inner.current)
        setOverflow(Math.max(0, Math.ceil(inner.current.scrollWidth - outer.current.clientWidth)))
    }
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(outer.current!)
    return () => observer.disconnect()
  }, [text])

  // Скорость прокрутки постоянная: чем длиннее хвост, тем дольше анимация
  const style = overflow > 0
    ? ({ '--overflow': `${overflow}px`, '--duration': `${2 + overflow / 30}s` } as CSSProperties)
    : undefined

  return (
    <span ref={outer} className={`marquee ${overflow > 0 ? 'scrolling' : ''} ${className}`} title={text}>
      <span ref={inner} className="marquee-inner" style={style}>
        {text}
      </span>
    </span>
  )
}
