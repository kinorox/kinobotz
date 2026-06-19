import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';

// https://vite.dev/config/ (defineConfig from vitest/config adds the `test` block)
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 8080,
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
  },
  test: {
    environment: 'jsdom',
    globals: true,
    // unit tests live under src/; e2e/ is Playwright (npm run test:e2e), not Vitest
    include: ['src/**/*.{test,spec}.{js,ts,jsx,tsx}'],
  },
});
