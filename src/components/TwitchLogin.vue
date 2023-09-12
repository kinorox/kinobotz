<script>
    export default {
      name: 'TwitchLogin',
      methods: {
      async login() {
        try {
          const clientId = 'lzszb9tfwd5w3czq84agigf5lih1ur';

          let redirectUri = 'https://k1no.tv/callback';
          if(process.env.NODE_ENV === 'development') {
            redirectUri = 'http://localhost:8080/callback';
          }
          const responseType = 'code';
          const scopes = 'user:read:email analytics:read:games user:edit:broadcast channel:read:subscriptions channel:read:redemptions channel:manage:broadcast user:read:subscriptions user:read:follows channel:read:polls channel:read:predictions channel:read:vips clips:edit bits:read';

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