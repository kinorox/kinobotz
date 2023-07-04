<script>
    export default {
        name: 'App'
    }
</script>

<style>
    @import'~bootstrap/dist/css/bootstrap.css';
</style>

<template>
    <div class="container">
        <!--<nav class="navbar navbar-expand-lg bg-body-tertiary">
            <div class="container-fluid">
                <a class="navbar-brand" href="#">Navbar</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="navbarSupportedContent">
                    <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                        <li class="nav-item">
                            <router-link class="nav-link active" aria-current="page" to="/">Home</router-link>
                            <router-link class="nav-link active" aria-current="page" to="/overlay/12345">Overlay</router-link>
                        </li>
                    </ul>
                </div>
            </div>
        </nav>-->
        <router-view></router-view>
    </div>
</template>
