<script>
import NavBar from './components/NavBar.vue';
import axios from 'axios';
import router from '@/scripts/router'
import { mapActions } from 'vuex';
import Cookies from 'js-cookie';

export default {
    name: "CallbackApp",
    components: { NavBar },
    mounted() {
        var code = this.extractTokenFromUrl();
        
        var url = 'https://kinobotz.herokuapp.com';
        if(process.env.NODE_ENV === 'development') {
            url = 'https://localhost:44305';
        }

        let redirectUri = 'https://k1no.tv/callback';
        if(process.env.NODE_ENV === 'development') {
            redirectUri = 'http://localhost:8080/callback';
        }

        axios.post(url + '/twitch/login', {
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
