<script>
import NavBar from './components/NavBar.vue';
import LoadingSpinner from './components/LoadingSpinner.vue';
import axiosInstance from '@/scripts/axios-instance';

export default {
  name: 'GptBehaviorApp',
  components: { NavBar, LoadingSpinner },
  data() {
    return {
      behaviorData: [],
      isLoading: false,
      searchQuery: ''
    };
  },
  mounted() {
    this.fetchBehaviorData();
  },
  methods: {
    fetchBehaviorData() {
      this.isLoading = true;
      axiosInstance.get('/Public/gptBehaviors')
        .then(response => {
          this.behaviorData = response.data;
        })
        .finally(() => {
          this.isLoading = false;
        });
    }
  },
  computed: {
    formattedBehaviorData() {
      return this.behaviorData.map(item => ({
        ...item,
        CreatedAtFormatted: new Date(item.createdAt).toLocaleString('pt-BR', {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
          timeZone: 'America/Sao_Paulo' // Brazilian time zone
        })
      }));
    },
    filteredBehaviorData() {
      if (!this.searchQuery) {
        return this.formattedBehaviorData; // If no search query, show all data
      }

      const normalizedQuery = this.searchQuery.toLowerCase();

      // Filter the data based on the search query
      return this.formattedBehaviorData.filter(item => {
        return (
          item.behavior.toLowerCase().includes(normalizedQuery) ||
          item.definedBy.toLowerCase().includes(normalizedQuery) ||
          item.channel.toLowerCase().includes(normalizedQuery)
        );
      });
    }
  }
}
</script>

<template>
  <NavBar />
  <LoadingSpinner v-if="isLoading" />
  <!-- Search input -->
  <input v-model="searchQuery" placeholder="Search..." class="form-control mb-3" />
  <table class="table table-bordered">
    <thead>
      <tr>
        <th>Channel</th>
        <th>Created At</th>
        <th>Defined By</th>
        <th>Behavior</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="(item, index) in filteredBehaviorData" :key="index">
        <td>{{ item.channel }}</td>
        <td>{{ item.CreatedAtFormatted }}</td>
        <td>{{ item.definedBy }}</td>
        <td>{{ item.behavior }}</td>
      </tr>
    </tbody>
  </table>
</template>