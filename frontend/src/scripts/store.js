import { defineStore } from 'pinia';

// Auth store (Pinia, replaces the old Vuex store). The JWT is also persisted to a
// cookie (see axios-instance / CallbackApp); this holds the in-memory copy.
export const useAuthStore = defineStore('auth', {
  state: () => ({
    jwtToken: null,
  }),
  getters: {
    getJwtToken: (state) => state.jwtToken,
  },
  actions: {
    setJwtToken(token) {
      this.jwtToken = token;
    },
  },
});
