<script>
    export default {
      name: 'TwitchLogin',
      methods: {
      async login() {
        try {
          const clientId = process.env.VUE_APP_TWITCH_CLIENT_ID;

          if (!clientId) {
            console.error('Twitch Client ID not configured');
            return;
          }

          let redirectUri = process.env.VUE_APP_TWITCH_REDIRECT_URI || 'https://k1no.tv/callback';
          if (process.env.NODE_ENV === 'development') {
            redirectUri = 'http://localhost:8080/callback';
          }
          const responseType = 'code';
          // Reduced to minimum required scopes
          const scopes = 'user:read:email';

          const twitchAuthUrl = `https://id.twitch.tv/oauth2/authorize?response_type=${responseType}&client_id=${clientId}&redirect_uri=${redirectUri}&scope=${scopes}`;

          // Redirect the user to the Twitch OAuth2 page
          window.location.href = twitchAuthUrl;
        } catch (error) {
          console.error('Login error:', error);
        }
      }
    },
  };
</script>

<template>
  <div>
    <button type="button" class="btn btn-twitch" @click="login">
      <font-awesome-icon :icon="['fab', 'twitch']" /> Log in with Twitch
    </button>
  </div>
</template>