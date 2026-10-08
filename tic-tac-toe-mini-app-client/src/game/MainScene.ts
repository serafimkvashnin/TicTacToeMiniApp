import Phaser from 'phaser'
import { getTopInset, onInsetsChange } from '../telegram'

export const COLORS = {
  background: '#2d6cdf',
  header: '#1b4fae',
}

const HEADER_HEIGHT = 48 // высота полосы с ником под зоной Telegram
const AMPLITUDE = 80
const SPEED = 150 // пикселей в секунду по горизонтали
const WAVELENGTH = 300 // пикселей на один период синусоиды
const RADIUS = 30

export class MainScene extends Phaser.Scene {
  private header!: Phaser.GameObjects.Rectangle
  private circle!: Phaser.GameObjects.Arc
  private nameText!: Phaser.GameObjects.Text
  private x0 = 0
  private centerY = 0

  constructor(private readonly userName: string) {
    super('MainScene')
  }

  create() {
    this.header = this.add
      .rectangle(0, 0, 1, 1, Phaser.Display.Color.HexStringToColor(COLORS.header).color)
      .setOrigin(0, 0)

    this.circle = this.add.circle(0, 0, RADIUS, 0xffd23f)

    this.nameText = this.add
      .text(0, 0, this.userName, {
        fontFamily: 'sans-serif',
        fontSize: '22px',
        color: '#ffffff',
      })
      .setOrigin(0.5, 0.5)

    this.layout()
    this.scale.on('resize', this.layout, this)
    const unsubscribe = onInsetsChange(() => this.layout())
    this.events.once(Phaser.Scenes.Events.DESTROY, unsubscribe)
  }

  update(_time: number, delta: number) {
    const { width } = this.scale
    this.x0 = (this.x0 + (SPEED * delta) / 1000) % (width + RADIUS * 2)

    const x = this.x0 - RADIUS
    const y = this.centerY + Math.sin((x / WAVELENGTH) * Math.PI * 2) * AMPLITUDE
    this.circle.setPosition(x, y)
  }

  private layout() {
    const { width, height } = this.scale
    const top = getTopInset()
    const headerBottom = top + HEADER_HEIGHT

    this.header.setSize(width, headerBottom)
    this.nameText.setPosition(width / 2, top + HEADER_HEIGHT / 2)
    this.centerY = headerBottom + (height - headerBottom) / 2
  }
}
