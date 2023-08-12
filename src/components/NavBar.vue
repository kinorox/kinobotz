<script>
    import TwitchLogin from './TwitchLogin.vue';
    import Cookies from 'js-cookie';
    import router from '@/scripts/router'

    export default {  
        components: { TwitchLogin },
        name: 'NavBar',
        computed: {
            userLoggedIn() {
                return Cookies.get('jwtToken');
            },
        },
        methods: {
            logoff() {
                Cookies.remove('jwtToken');
                router.push('/')
            }
        }
    }
</script>

<template>
    <nav class="navbar navbar-expand-lg">
        <router-link aria-current="page" to="/" class="navbar-brand anaglyph" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation" style="font-size: 10em;">k1no.tv</router-link>
        <div class="collapse navbar-collapse" id="navbarSupportedContent">
            <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                <li v-if="userLoggedIn" class="nav-item">
                    <router-link class="nav-link active btn btn-dark" aria-current="page" to="/gptbehavior">gpt behavior</router-link>
                </li>
                <li v-if="userLoggedIn" class="nav-item">
                    <router-link class="nav-link active btn btn-dark" aria-current="page" to="/dashboard">dashboard</router-link>
                </li>
                <li v-if="userLoggedIn" class="nav-item">
                    <button type="button" class="btn btn-dark" @click="logoff()">logout</button>
                </li>
            </ul>
        </div>
        <div v-if="!userLoggedIn">
            <TwitchLogin/>
        </div>
    </nav>
</template>
