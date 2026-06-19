import { defineConfig } from '@playwright/test';

// E2E tests run against the DEPLOYED app — the nginx CSP (and SignalR/WebSocket path)
// only exist there, not in the Vite dev/preview build.
export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: 'list',
  use: {
    headless: true,
    ignoreHTTPSErrors: true,
  },
});
