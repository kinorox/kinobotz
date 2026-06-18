<script>
import LoadingSpinner from './components/LoadingSpinner.vue';
import router from '@/scripts/router'
import { mapActions } from 'vuex';
import Cookies from 'js-cookie';
import axiosInstance from '@/scripts/axios-instance';
import jwtDecode from 'jwt-decode';

export default {
    name: "CallbackApp",
    components: { LoadingSpinner },
    mounted() {
        var code = this.extractTokenFromUrl();
        
        let redirectUri = 'https://k1no.tv/callback';
        if(process.env.NODE_ENV === 'development') {
            redirectUri = 'http://localhost:8080/callback';
        }

        axiosInstance.post('/twitch/login', {
            AccessToken: code,
            RedirectUri: redirectUri
        }).then((response) => {
            this.handleSuccessfulAuth(response.data.accessToken)
        });
    },
    methods: {
        extractTokenFromUrl() {
            const currentUrl = window.location.href;
            const urlObj = new URL(currentUrl);
            const code = urlObj.searchParams.get("code"); 

            return code;
        },
        handleSuccessfulAuth(token) {
            Cookies.set('jwtToken', token, { expires: 30 });
            const decodedToken = jwtDecode(token);
            Cookies.set('userAccessLevel', decodedToken.AccessLevel, { expires: 30 });
            Cookies.set('ProfileImageUrl', decodedToken.ProfileImageUrl, { expires: 30 });
            router.push('/dashboard')
        },
        ...mapActions(['saveJwtToken']),
    },
}
</script>

<template>
    <LoadingSpinner/>
</template>
