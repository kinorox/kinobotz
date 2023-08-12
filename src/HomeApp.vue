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
    <div class="container-home">
        <NavBar/>
    </div>
</template>
