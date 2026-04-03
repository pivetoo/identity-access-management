import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      'd-rts': path.resolve(__dirname, './src/compat/d-rts.ts'),
    },
  },
})
