import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import fs from 'node:fs';
import path from 'node:path';

const certPath = path.resolve('certs/vite.crt');
const keyPath = path.resolve('certs/vite.key');
const useHttps = process.env.KIER_FRONTEND_HTTPS === '1';
const https = useHttps && fs.existsSync(certPath) && fs.existsSync(keyPath)
  ? {
      cert: fs.readFileSync(certPath),
      key: fs.readFileSync(keyPath),
    }
  : undefined;

export default defineConfig({
  plugins: [vue()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    https,
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
});
