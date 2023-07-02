<template>  
    <AudioPlayer/>
</template>

<script>
    import AudioPlayer from './components/AudioPlayer.vue'

    export default {
        setup() {

        },
        name: 'App',
        components: {
            AudioPlayer
        }
    }
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
