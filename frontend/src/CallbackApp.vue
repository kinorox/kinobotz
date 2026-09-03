<script>
import LoadingSpinner from './components/LoadingSpinner.vue';
import router from '@/scripts/router'
import Cookies from 'js-cookie';
import axiosInstance from '@/scripts/axios-instance';
import jwtDecode from 'jwt-decode';

export default {
    name: "CallbackApp",
    components: { LoadingSpinner },
    data() {
        return {
            error: null,
        };
    },
    mounted() {
        const params = new URL(window.location.href).searchParams;

        // Twitch redirects back with ?error=...&error_description=... when the user
        // declines consent or something goes wrong on their side.
        const twitchError = params.get('error');
        if (twitchError) {
            this.error = params.get('error_description') || twitchError;
            return;
        }

        const code = params.get('code');
        if (!code) {
            this.error = 'No authorization code was returned by Twitch.';
            return;
        }

        const env = window.__ENV__ || {};
        let redirectUri = env.TWITCH_REDIRECT_URI || 'https://k1no.tv/callback';
        if (import.meta.env.DEV) {
            redirectUri = 'http://localhost:8080/callback';
        }

        axiosInstance.post('/twitch/login', {
            AccessToken: code,
            RedirectUri: redirectUri
        }).then((response) => {
            this.handleSuccessfulAuth(response.data.accessToken)
        }).catch((err) => {
            console.error('Login error:', err);
            this.error = err.response?.status === 401
                ? 'Twitch rejected the login. Please try again.'
                : 'Could not complete the login. Please try again.';
        });
    },
    methods: {
        handleSuccessfulAuth(token) {
            Cookies.set('jwtToken', token, { expires: 30 });
            const decodedToken = jwtDecode(token);
            Cookies.set('userAccessLevel', decodedToken.AccessLevel, { expires: 30 });
            Cookies.set('ProfileImageUrl', decodedToken.ProfileImageUrl, { expires: 30 });
            router.push('/dashboard')
        },
    },
}
</script>

<template>
    <div v-if="error" class="callback-error">
        <p>{{ error }}</p>
        <router-link class="btn btn-dark" to="/">back to home</router-link>
    </div>
    <LoadingSpinner v-else/>
</template>

<style>
    .callback-error {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 1rem;
        min-height: 100vh;
        text-align: center;
    }
</style>
