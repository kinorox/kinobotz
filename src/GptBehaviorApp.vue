<script>
    import NavBar from './components/NavBar.vue';
    import TableLite from 'vue3-table-lite'
    import LoadingSpinner from './components/LoadingSpinner.vue';
    import { reactive } from 'vue'
    import axiosInstance from '@/scripts/axios-instance';

    export default {
        name: 'GptBehaviorApp',        
        components: { TableLite, NavBar, LoadingSpinner },
        setup() {
            // Init Your table settings
            const table = reactive({
                isLoading: false,
                columns: [
                  {
                      label: "channel",
                      field: "channel",
                      width: "3%",
                      sortable: true,
                      isKey: false,
                  },
                  {
                      label: "defined by",
                      field: "definedBy",
                      width: "10%",
                      sortable: true,
                  },
                  {
                      label: "behavior",
                      field: "behavior",
                      width: "15%",
                      sortable: true,
                  },
                  {
                      label: "created at",
                      field: "createdAt",
                      width: "15%",
                      sortable: true,
                  },
                ],
                rows: [],
                totalRecordCount: 0,
                sortable: {
                order: "createdAt",
                sort: "desc",
              }
            })

        /**
       * Table search event
       */
       const doSearch = (order, sort) => {
        table.isLoading = true;
  
        // Start use axios to get data from Server
        axiosInstance.get('/behaviors')
          .then(response => {
            table.rows = response.data;
            table.totalRecordCount = response.data.length;
            table.sortable.order = order;
            table.sortable.sort = sort;
          })
        // End use axios to get data from Server
      };
  
      /**
       * Table search finished event
       */
      const tableLoadingFinish = () => {
        table.isLoading = false;
      };

      // Get data first
      doSearch(0, 10, 'createdAt', 'desc');
  
      return {
        table,
        doSearch,
        tableLoadingFinish,
      };
  }
}
</script>

<template>
    <NavBar/>
    <LoadingSpinner v-if="table.isLoading"/>
    <table-lite
        :is-loading="table.isLoading"
        :columns="table.columns"
        :rows="table.rows"
        :total="table.totalRecordCount"
        :sortable="table.sortable"
        @do-search="doSearch"
        @is-finished="tableLoadingFinish"
    />
</template>