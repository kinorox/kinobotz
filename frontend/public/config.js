// This file is overwritten at container startup with actual env vars.
// Fallback values for local development:
window.__ENV__ = {
  TWITCH_CLIENT_ID: '',
  API_URL: 'http://localhost:44305',
  TWITCH_REDIRECT_URI: 'http://localhost:8080/callback'
};
