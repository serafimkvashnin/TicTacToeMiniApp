import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  // относительные пути, чтобы сборка работала на GitHub Pages по адресу /<repo>/
  base: './',
  server: {
    host: true,
    // разрешаем доступ через туннели (ngrok, cloudflared и т.п.)
    allowedHosts: true,
    // в dev клиент ходит на тот же origin, а Vite пробрасывает хаб на локальный сервер,
    // поэтому для теста в Telegram хватает одного туннеля на Vite
    proxy: {
      '/hubs': { target: 'http://localhost:5080', ws: true },
    },
  },
})
