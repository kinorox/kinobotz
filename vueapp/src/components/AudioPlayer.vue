<template>
    <audio id="audioPlayer" :src="audioURL" autoplay muted/>
</template>

<script>
    import { useSignalR } from '@dreamonkey/vue-signalr';

    var audioQueue = [];

    function base64ToArrayBuffer(base64) {
        var binaryString = atob(base64);
        var bytes = new Uint8Array(binaryString.length);
        for (var i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    }

    function playNextAudio(src) {
        if (src == undefined)
            src = audioQueue.shift();

        var audio = document.getElementById("audioPlayer");

        if (audio != undefined && audio != null) {
            audio.src = src;
            //audio.load();
            audio.muted = true;
            audio.play();
        }
    }

    export default {
        data() {
            return {
                audioURL: ''
            }
        },
        setup() {
            const signalr = useSignalR();
            signalr.on('receiveAudio', function (message) {
                var buffer = base64ToArrayBuffer(message);

                var blob = new Blob([buffer], { type: "audio/mp3" });

                var url = URL.createObjectURL(blob);

                var audio = document.getElementById("audioPlayer");
                audio.addEventListener("ended", (event) => {
                    playNextAudio(event)
                });

                if (audioQueue.length == 0) {
                    playNextAudio(url);
                } else {
                    audioQueue.push(url);
                }
            });
        },
        computed() {
            return {
                
            }
        },
        name: 'AudioPlayer'
    }
</script>

<style>

</style>