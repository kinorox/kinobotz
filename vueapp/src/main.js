import { createApp } from 'vue';
import App from './App.vue';
import { VueSignalR } from '@dreamonkey/vue-signalr';
import { HubConnectionBuilder } from '@microsoft/signalr';

// Create your connection
// See https://docs.microsoft.com/en-us/javascript/api/@microsoft/signalr/hubconnectionbuilder
const connection = new HubConnectionBuilder()
    .withUrl('http://localhost:5000/signalr')
    .build();

createApp(App)
    .use(VueSignalR, { connection })
    .mount('#app');
