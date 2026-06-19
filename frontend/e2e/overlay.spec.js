import { test, expect } from '@playwright/test';

// End-to-end overlay / SignalR test against the DEPLOYED app (the nginx CSP and the
// SignalR WebSocket path only exist in the deployed container, not in Vite dev/preview —
// which is why a non-browser test can't catch the CSP regression that degraded TTS).
//
// It loads the real overlay and asserts:
//  1. no CSP violation (the WebSocket uses wss://, the audio uses blob: — both must be allowed),
//  2. the overlay receives audio pushed to the hub endpoint exactly like the worker does after TTS.
//
// Configure (defaults are prod):
//   OVERLAY_APP  - overlay origin           (default https://k1no.tv)
//   OVERLAY_API  - webapi origin (the hub)  (default https://kinobotz.herokuapp.com)
const APP = process.env.OVERLAY_APP || 'https://k1no.tv';
const API = process.env.OVERLAY_API || 'https://kinobotz.herokuapp.com';

test('overlay: no CSP violation (wss + blob) and receives pushed audio', async ({ page, request }) => {
  const testId = `e2e-${Date.now()}`;
  const received = [];

  // Capture CSP violations the reliable way (the securitypolicyviolation event), not console.
  await page.addInitScript(() => {
    window.__cspViolations = [];
    document.addEventListener('securitypolicyviolation', (e) => {
      window.__cspViolations.push(`${e.effectiveDirective || e.violatedDirective} blocked ${e.blockedURI}`);
    });
  });

  page.on('console', (msg) => {
    if (msg.text().startsWith('received')) received.push(msg.text()); // OverlayApp logs 'received<msg>'
  });

  await page.goto(`${APP}/overlay/${testId}`, { waitUntil: 'load' });
  await page.waitForTimeout(4000); // let SignalR negotiate + attempt the wss WebSocket

  // simulate the worker delivering TTS audio for this overlay id (triggers the blob: audio path too)
  const audio = Buffer.from([0xff, 0xfb, 0x90, 0x00, 0x00, 0x00, 0x00, 0x00]); // minimal mp3 frame header
  const resp = await request.post(`${API}/overlay/audio/${testId}`, {
    data: audio,
    headers: { 'content-type': 'application/octet-stream' },
  });
  expect(resp.status(), 'POST /overlay/audio/{id} should return 200').toBe(200);

  // the overlay must receive the SignalR broadcast (client method == testId)
  await expect.poll(() => received.length, { message: 'overlay never received the SignalR audio message' }).toBeGreaterThan(0);

  await page.waitForTimeout(1500); // let the blob: audio src be set/loaded (media-src CSP applies here)
  const violations = await page.evaluate(() => window.__cspViolations || []);
  expect(violations, `overlay hit CSP violation(s):\n${violations.join('\n')}`).toEqual([]);
});
