<script>
import NavBar from './components/NavBar.vue';
import axiosInstance from '@/scripts/axios-instance';
import Cookies from 'js-cookie';
import router from '@/scripts/router'
import LoadingSpinner from './components/LoadingSpinner.vue';

export default {
  name: 'StatsApp',
  components: { NavBar, LoadingSpinner },
  data() {
    return {
      logs: null,
      isLoading: false,
      searchQuery: '',       
      selectedColumn: ''
    };
  },
  mounted() {
    if (!Cookies.get('jwtToken')) {
      router.push('/')
    } else {
      this.load();
    }
  },
  methods: {
    load() {
      this.isLoading = true;
      axiosInstance.get('/Commands/log')
        .then(response => {
          this.logs = response.data;
        })
        .catch(error => {
          console.log(error)
        })
        .finally(() => {
          this.isLoading = false;
        });
    },
    formatAsJSON(obj) {
      return JSON.stringify(obj);
    }
  },
  computed: {
    filteredLogs() {
      return this.logs.filter((log) => {
        if (this.selectedColumn === '') {
          // If no column is selected, search in all columns
          for (const key in log) {
            if (log[key].toString().toLowerCase().includes(this.searchQuery.toLowerCase())) {
              return true;
            }
          }
          return false;
        } else {
          // Search in the selected column
          return log[this.selectedColumn].toString().toLowerCase().includes(this.searchQuery.toLowerCase());
        }
      });
    }
  }
}
</script>

<template>
  <NavBar />
  <LoadingSpinner v-if="isLoading" />
  <div v-if="logs">
    <div class="search-bar">
      <select v-model="selectedColumn" @change="performSearch">
        <option value="">All Columns</option>
        <option v-for="(item, key) in logs[0]" :key="key" :value="key">{{ key }}</option>
      </select>
      <input v-model="searchQuery" placeholder="Search" />
    </div>
    <table class="table table-bordered">
      <thead>
        <tr>
          <th v-for="(item, key) in logs[0]" :key="key">{{ key }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in logs" :key="item.id">
          <td v-for="(value, key) in item" :key="key">
            <!-- Check if the property is an object and format as JSON -->
            {{ key === 'rewardRedeemed' || key === 'chatMessage' || key === 'command' ? formatAsJSON(value) : value }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>