#!/bin/sh
# Generates runtime config from environment variables.
# Runs at container startup before nginx.

cat <<EOF > /usr/share/nginx/html/config.js
window.__ENV__ = {
  TWITCH_CLIENT_ID: '${TWITCH_CLIENT_ID:-}',
  API_URL: '${API_URL:-https://kinobotz.herokuapp.com}',
  TWITCH_REDIRECT_URI: '${TWITCH_REDIRECT_URI:-https://k1no.tv/callback}'
};
EOF
