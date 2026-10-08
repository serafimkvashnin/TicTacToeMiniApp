import * as signalR from '@microsoft/signalr'
import { getInitData } from '../telegram'

export type Player = {
  id: number
  name: string
  username: string | null
  isHost: boolean
}

export type Room = {
  code: string
  capacity: number
  players: Player[]
}

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected'

export type GameState = {
  status: ConnectionStatus
  room: Room | null
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
let state: GameState = { status: 'disconnected', room: null }
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

connection.on('RoomUpdated', (room: Room) => setState({ room }))
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
  setState({ room: await connection.invoke<Room>('CreateRoom') })
}

export async function joinRoom(code: string) {
  setState({ room: await connection.invoke<Room>('JoinRoom', code) })
}

export async function leaveRoom() {
  await connection.invoke('LeaveRoom')
  setState({ room: null })
}

// SignalR оборачивает HubException в "...HubException: <сообщение>"
export function errorMessage(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error)
  return message.split('HubException: ')[1] ?? 'Что-то пошло не так'
}
