<script>
import NavBar from './components/NavBar.vue';
import axios from 'axios';
import router from '@/scripts/router'
import { mapActions } from 'vuex';
import Cookies from 'js-cookie';
import axiosInstance from '@/scripts/axios-instance';

export default {
    name: "CallbackApp",
    components: { NavBar },
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
            router.push('/dashboard')
        },
        ...mapActions(['saveJwtToken']),
    },
}
</script>

<template>
    <NavBar/>
</template>
