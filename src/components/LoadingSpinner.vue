<script>
    import { PixelSpinner } from 'epic-spinners'

    export default {
        name: 'LoadingSpinner',
        components: {
            PixelSpinner
        }
    }
</script>

<style>
    .loading-spinner {
        display: flex;
        justify-content: center;
        align-items: center;
        min-height: 100vh; /* Set a minimum height to fill the viewport */
    }
</style>

<template>     
    <div class="loading-spinner">
        <pixel-spinner
            :animation-duration="2000"
            :size="70"
            color="#1c9716"
        />
    </div>
</template>
