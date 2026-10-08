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
  },
})
