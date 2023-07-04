<script>
    import { useSignalR } from '@dreamonkey/vue-signalr';
    import { useRoute } from 'vue-router'

    var audioQueue = [];

    function base64ToArrayBuffer(base64) {
        var binaryString = atob(base64);
        var bytes = new Uint8Array(binaryString.length);
        for (var i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    }

    export default {
        data() {
            return {
                audioURL: null
            }
        },
        methods: {
            playNextAudio() {
                var audio = document.getElementById("audioPlayer");
                audio.removeAttribute('src')

                var firstAudio = audioQueue.shift();

                if (firstAudio == undefined)
                    return;

                audio.src = firstAudio;
                audio.load();
                audio.play();
            },
            receiveAudioMessage(message) {
                if (message == undefined)
                    return;

                var buffer = base64ToArrayBuffer(message);

                var blob = new Blob([buffer], { type: "audio/mp3" });

                var url = URL.createObjectURL(blob);

                var audio = document.getElementById("audioPlayer");

                if (audio.src == '' || audio.src == undefined) {
                    audio.src = url;
                    audio.load();
                    audio.play();
                } else {
                    audioQueue.push(url);
                }
            }
        },
        mounted() {
            const route = useRoute()
            const signalr = useSignalR();

            signalr.on(route.params.id, this.receiveAudioMessage);
        },
        name: 'OverlayApp'
    }
</script>

<template>
    <audio id="audioPlayer" autoplay @ended="playNextAudio()"/>
</template>