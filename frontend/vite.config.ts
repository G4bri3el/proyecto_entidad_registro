import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const workspaceRoot = fileURLToPath(new URL('../', import.meta.url));

export default defineConfig({
  envDir: workspaceRoot,
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: false
  }
});