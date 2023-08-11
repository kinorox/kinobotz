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
        <a class="navbar-brand anaglyph" href="#" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation" style="font-size: 10em;">k1no.tv</a>
        <div class="collapse navbar-collapse" id="navbarSupportedContent">
            <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                <li class="nav-item">
                    <router-link class="nav-link active" aria-current="page" to="/">home</router-link>
                </li>
                <li v-if="!userLoggedIn" class="nav-item">
                    <TwitchLogin/>
                </li>
                <li v-if="userLoggedIn" class="nav-item">
                    <router-link class="nav-link active" aria-current="page" to="/gptbehavior">gpt behavior</router-link>
                </li>
                <li v-if="userLoggedIn" class="nav-item">
                    <router-link class="nav-link active" aria-current="page" to="/dashboard">dashboard</router-link>
                </li>
                <li v-if="userLoggedIn" class="nav-item">
                    <button @click="logoff()">Logout</button>
                </li>
            </ul>
        </div>
    </nav>
</template>
