import { createApp } from 'vue';
import App from '@/App.vue';
import OverlayApp from '@/components/OverlayApp.vue';
import { createRouter, createWebHistory } from 'vue-router'
import { VueSignalR } from '@dreamonkey/vue-signalr';
import { HubConnectionBuilder } from '@microsoft/signalr';
import 'bootstrap'

const routes = [
    { path: '/', name: 'home', component: App },
    { path: '/overlay/:id', name: 'overlay', component: OverlayApp }
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

const connection = new HubConnectionBuilder()
    .withUrl('http://localhost:5000/overlay')
    .build();

createApp(App)
    .use(router)
    .use(VueSignalR, { connection })
    .mount('#app');
