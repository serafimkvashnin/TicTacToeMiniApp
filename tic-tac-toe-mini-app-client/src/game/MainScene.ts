import Phaser from 'phaser'

const AMPLITUDE = 80
const SPEED = 150 // пикселей в секунду по горизонтали
const WAVELENGTH = 300 // пикселей на один период синусоиды
const RADIUS = 30

export class MainScene extends Phaser.Scene {
  private circle!: Phaser.GameObjects.Arc
  private nameText!: Phaser.GameObjects.Text
  private x0 = 0

  constructor(private readonly userName: string) {
    super('MainScene')
  }

  create() {
    this.circle = this.add.circle(0, 0, RADIUS, 0xffd23f)

    this.nameText = this.add
      .text(0, 24, this.userName, {
        fontFamily: 'sans-serif',
        fontSize: '28px',
        color: '#ffffff',
      })
      .setOrigin(0.5, 0)

    this.layout()
    this.scale.on('resize', this.layout, this)
  }

  update(_time: number, delta: number) {
    const { width, height } = this.scale
    this.x0 = (this.x0 + (SPEED * delta) / 1000) % (width + RADIUS * 2)

    const x = this.x0 - RADIUS
    const y = height / 2 + Math.sin((x / WAVELENGTH) * Math.PI * 2) * AMPLITUDE
    this.circle.setPosition(x, y)
  }

  private layout() {
    this.nameText.setX(this.scale.width / 2)
  }
}
