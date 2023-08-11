import { createApp } from 'vue';
import App from '@/App.vue';
import { VueSignalR } from '@dreamonkey/vue-signalr';
import { HubConnectionBuilder } from '@microsoft/signalr';
import router from '@/scripts/router'
import store from '@/scripts/store'

var url = 'https://kinobotz.herokuapp.com';
if(process.env.NODE_ENV === 'development') {
    url = 'https://localhost:44305';
}

const connection = new HubConnectionBuilder()
    .withUrl(url + '/overlayHub')
    .build();

createApp(App)
    .use(router)
    .use(store)
    .use(VueSignalR, { connection })
    .mount('#app');