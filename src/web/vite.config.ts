import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// In development /api is forwarded to the local API, so CORS is not needed.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5111',
        changeOrigin: true,
        rewrite: (ruta) => ruta.replace(/^\/api/, ''),
      },
    },
  },
});
