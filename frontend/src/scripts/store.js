import Vuex from 'vuex';

export default new Vuex.Store({
  state: {
    auth: {
      jwtToken: null,
    }
  },
  mutations: {
    setJwtToken(state, token) {
      state.auth.jwtToken = token;
    }
  },
  actions: {
    saveJwtToken({ commit }, token) {
      commit('setJwtToken', token);
    }
  },
  getters: {
    getJwtToken: state => state.auth.jwtToken,
  }
});