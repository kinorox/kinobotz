<script>
    import TwitchLogin from './TwitchLogin.vue';
    import Cookies from 'js-cookie';
    import router from '@/scripts/router'
    import axiosInstance from '@/scripts/axios-instance';

    export default {  
        components: { TwitchLogin },
        name: 'NavBar',
        computed: {
            userLoggedIn() {
                return Cookies.get('jwtToken');
            },
            isAdmin() {
                return Cookies.get('userAccessLevel') === 'Admin';
            },
        },
        methods: {
            logoff() {
                axiosInstance.post('/twitch/logout').then(() => {                    
                    Cookies.remove('jwtToken');
                    router.push('/')
                });
            }
        }
    }
</script>

<template>
    <nav class="navbar navbar-expand-lg">
        <a aria-current="page" href="/" class="navbar-brand anaglyph" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation" style="font-size: 10em;">k1no.tv</a>
        <div v-if="!userLoggedIn">
            <TwitchLogin/>
        </div>
        <div v-else class="collapse navbar-collapse" id="navbarSupportedContent">
            <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                <li class="nav-item">
                    <router-link class="btn btn-dark" aria-current="page" to="/dashboard" exact>dashboard</router-link>
                </li>
                <li class="nav-item">
                    <router-link class="btn btn-dark" aria-current="page" to="/gptbehavior" exact>gpt behavior</router-link>
                </li>
                <li class="nav-item">
                    <router-link class="btn btn-dark" aria-current="page" to="/commands" exact>commands</router-link>
                </li>
                <li v-if="isAdmin" class="nav-item">
                    <router-link class="btn btn-dark" aria-current="page" to="/users" exact>users</router-link>
                </li>
                <li v-if="isAdmin" class="nav-item">
                    <router-link class="btn btn-dark" aria-current="page" to="/stats" exact>stats</router-link>
                </li>
                <li class="nav-item">
                    <button type="button" class="btn btn-dark" @click="logoff()">logout</button>
                </li>
            </ul>
        </div>
    </nav>
</template>
