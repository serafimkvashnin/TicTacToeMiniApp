import * as signalR from '@microsoft/signalr'
import { getInitData } from '../telegram'

export type Mark = 'X' | 'O'

export type Player = {
  id: number
  name: string
  username: string | null
  isHost: boolean
  mark: Mark | null
}

export type Game = {
  board: (Mark | null)[]
  turn: Mark
  status: 'Playing' | 'Won' | 'Draw'
  winner: Mark | null
  winningLine: number[] | null
}

export type Room = {
  code: string
  capacity: number
  /** Комната из подбора случайного соперника */
  isPublic: boolean
  players: Player[]
  /** Ваше место в players: у каждого подключения своё */
  yourSeat: number
  version: number
  game: Game | null
}

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected'

export type GameState = {
  status: ConnectionStatus
  room: Room | null
  /** Событие, о котором стоит сказать игроку, например уход соперника */
  notice: string | null
}

// Пусто — тот же origin (в dev Vite проксирует /hubs на локальный сервер)
const SERVER_URL = import.meta.env.VITE_SERVER_URL ?? ''

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${SERVER_URL}/hubs/game?initData=${encodeURIComponent(getInitData())}`, {
    // бесплатный ngrok иначе может отдать страницу-предупреждение вместо ответа сервера
    headers: { 'ngrok-skip-browser-warning': '1' },
  })
  .withAutomaticReconnect()
  .build()

// Простое внешнее хранилище для useSyncExternalStore
let state: GameState = { status: 'disconnected', room: null, notice: null }
const listeners = new Set<() => void>()

function setState(patch: Partial<GameState>) {
  state = { ...state, ...patch }
  listeners.forEach((listener) => listener())
}

export function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function getState() {
  return state
}

export function dismissNotice() {
  setState({ notice: null })
}

// Ответ на свой запрос и рассылка сервера могут прийти в любом порядке — старое состояние отбрасываем
function applyRoom(room: Room) {
  const current = state.room
  if (current && current.code === room.code && room.version < current.version) return

  const opponentLeft = current?.code === room.code && room.players.length < current.players.length
  const leftNotice = room.isPublic ? 'Соперник вышел — ищем нового' : 'Соперник покинул комнату'
  setState({ room, notice: opponentLeft ? leftNotice : state.notice })
}

connection.on('RoomUpdated', applyRoom)
connection.onreconnecting(() => setState({ status: 'connecting' }))
// После переподключения это новое соединение: сервер уже вывел нас из комнаты
connection.onreconnected(() => setState({ status: 'connected', room: null }))
connection.onclose(() => setState({ status: 'disconnected', room: null }))

export async function connect() {
  if (connection.state !== signalR.HubConnectionState.Disconnected) return
  setState({ status: 'connecting' })
  try {
    await connection.start()
    setState({ status: 'connected' })
  } catch {
    setState({ status: 'disconnected' })
  }
}

export async function createRoom() {
  applyRoom(await connection.invoke<Room>('CreateRoom'))
}

export async function joinRoom(code: string) {
  applyRoom(await connection.invoke<Room>('JoinRoom', code))
}

export async function quickPlay() {
  applyRoom(await connection.invoke<Room>('QuickPlay'))
}

export async function makeMove(cell: number) {
  applyRoom(await connection.invoke<Room>('MakeMove', cell))
}

export async function rematch() {
  applyRoom(await connection.invoke<Room>('Rematch'))
}

export async function leaveRoom() {
  await connection.invoke('LeaveRoom')
  setState({ room: null, notice: null })
}

// SignalR оборачивает HubException в "...HubException: <сообщение>"
export function errorMessage(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error)
  return message.split('HubException: ')[1] ?? 'Что-то пошло не так'
}
