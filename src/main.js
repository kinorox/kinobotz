import { createApp } from 'vue';
import App from '@/App.vue';
import { VueSignalR } from '@dreamonkey/vue-signalr';
import { HubConnectionBuilder } from '@microsoft/signalr';
import router from '@/scripts/router'
import store from '@/scripts/store'
import 'bootstrap/dist/css/bootstrap.css';
import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { faUserSecret } from '@fortawesome/free-solid-svg-icons'
import Vue3EasyDataTable from 'vue3-easy-data-table';
import 'vue3-easy-data-table/dist/style.css';

library.add(faUserSecret)

var url = 'https://kinobotz.herokuapp.com';
if(process.env.NODE_ENV === 'development') {
    url = 'https://localhost:44305';
}

const connection = new HubConnectionBuilder()
    .withUrl(url + '/overlayHub')
    .build();

createApp(App)
    .component('EasyDataTable', Vue3EasyDataTable)
    .use(router)
    .use(store)
    .use('font-awesome-icon', FontAwesomeIcon)
    .use(VueSignalR, { connection })
    .mount('#app');