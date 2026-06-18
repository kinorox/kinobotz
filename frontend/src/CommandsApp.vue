<script>
    import NavBar from './components/NavBar.vue';
    import axiosInstance from '@/scripts/axios-instance';
    import Cookies from 'js-cookie';
    import router from '@/scripts/router'
    import LoadingSpinner from './components/LoadingSpinner.vue';

    export default {
        name: 'CommandsApp',        
        components: { NavBar, LoadingSpinner },
        data() {
            return {
                commands: null,
                isLoading: false
            };
        },
        mounted() {
            if(!Cookies.get('jwtToken')) {
                router.push('/')
            } else {
                this.load();
            }
        },
        methods: {
            load() {
                this.isLoading = true;
                axiosInstance.get('/Commands')
                    .then(response => {
                        this.commands = response.data;
                    })
                    .catch(error => {
                        console.log(error)
                    })
                    .finally(() => {
                      this.isLoading = false;
                    });
            }
          }
        }
</script>

<template>
  <NavBar/>
  <LoadingSpinner v-if="isLoading"/>
  <table class="table table-bordered">
    <thead>
      <tr>
        <th>Prefix</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="command in commands" :key="command.Prefix">
        <td>{{ command.prefix }}</td>
        <td>{{ command.description }}</td>
      </tr>
    </tbody>
  </table>
</template>