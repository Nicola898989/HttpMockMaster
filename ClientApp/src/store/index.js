import { createStore } from 'vuex'
import requestsModule from './modules/requests'
import rulesModule from './modules/rules'
import proxyModule from './modules/proxy'
import topologyModule from './modules/topology'

export default createStore({
  state: {
    isLoading: false,
    error: null
  },
  mutations: {
    SET_LOADING(state, isLoading) {
      state.isLoading = isLoading
    },
    SET_ERROR(state, error) {
      state.error = error
    },
    CLEAR_ERROR(state) {
      state.error = null
    }
  },
  actions: {
    setLoading({ commit }, isLoading) {
      commit('SET_LOADING', isLoading)
    },
    setError({ commit }, error) {
      commit('SET_ERROR', error)
    },
    clearError({ commit }) {
      commit('CLEAR_ERROR')
    }
  },
  modules: {
    requests: requestsModule,
    rules: rulesModule,
    proxy: proxyModule,
    topology: topologyModule
  }
})
