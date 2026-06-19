import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from '@/App.vue';
import { HubConnectionBuilder } from '@microsoft/signalr';
import router from '@/scripts/router';
import 'bootstrap/dist/css/bootstrap.css';
import Vue3EasyDataTable from 'vue3-easy-data-table';
import 'vue3-easy-data-table/dist/style.css';
import { library } from '@fortawesome/fontawesome-svg-core';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { fab } from '@fortawesome/free-brands-svg-icons';

// add the icon style(s) you have installed to the library
library.add(fab);

// API base comes from the runtime config (window.__ENV__, injected by config.js),
// same source axios-instance uses — no build-time/process.env branching.
const env = window.__ENV__ || {};
const apiUrl = env.API_URL || 'https://kinobotz.herokuapp.com';

const connection = new HubConnectionBuilder()
  .withUrl(apiUrl + '/overlayHub')
  .build();

createApp(App)
  .component('EasyDataTable', Vue3EasyDataTable)
  .component('font-awesome-icon', FontAwesomeIcon)
  .use(router)
  .use(createPinia())
  .provide('signalR', connection)
  .mount('#app');
