<script>
import NavBar from './components/NavBar.vue';

export default {
    name: "HomeApp",
    components: { NavBar }
}
</script>

<style>
  
    .container-home {
        display: flex;
        justify-content: center;
        align-items: center;
        min-height: 100vh; /* Set a minimum height to fill the viewport */
    }

</style>

<template>
    <div class="container-home flex-column">
        <NavBar/>
        <ul class="list-group list-group-horizontal">
            <li class="list-group-item"><a href="https://twitch.tv/k1notv" target=”_blank”><font-awesome-icon :icon="['fab', 'twitch']" class="icon alt"/></a></li>
            <li class="list-group-item"><a href="https://twitter.com/k1notv" target=”_blank”><font-awesome-icon :icon="['fab', 'twitter']" class="icon alt"/></a></li>
            <li class="list-group-item"><a href="https://github.com/kinorox" target=”_blank”><font-awesome-icon :icon="['fab', 'github']" class="icon alt"/></a></li>
            <li class="list-group-item"><a href="https://www.instagram.com/k1notv/" target=”_blank”><font-awesome-icon :icon="['fab', 'instagram']" class="icon alt"/></a></li>
            <li class="list-group-item"><a href="https://discord.gg/YdwQutt" target=”_blank”><font-awesome-icon :icon="['fab', 'discord']" class="icon alt"/></a></li>
            <li class="list-group-item"><a href="https://www.tiktok.com/@ttvk1notv" target=”_blank”><font-awesome-icon :icon="['fab', 'tiktok']" class="icon alt"/></a></li>
            <li class="list-group-item"><a href="https://www.youtube.com/channel/UCtOKGIvNUyLyMmFcDUDWSNA" target=”_blank”><font-awesome-icon :icon="['fab', 'youtube']" class="icon alt"/></a></li>
            <li class="list-group-item"><a href="https://patreon.com/K1NOtv" target=”_blank”><font-awesome-icon :icon="['fab', 'patreon']" class="icon alt"/></a></li>
        </ul>
        <br>
        contatok1notv@gmail.com
    </div>
</template>
