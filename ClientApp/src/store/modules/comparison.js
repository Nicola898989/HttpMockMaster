import api from '../../services/api'

export default {
  namespaced: true,
  
  state: {
    comparisonResults: null,
    jsonDiffResult: null,
    isLoading: false,
    error: null
  },
  
  mutations: {
    SET_COMPARISON_RESULTS(state, results) {
      state.comparisonResults = results
    },
    SET_JSON_DIFF_RESULT(state, result) {
      state.jsonDiffResult = result
    },
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
    async compareRequests({ commit }, requestIds) {
      if (!requestIds || requestIds.length < 2) {
        commit('SET_ERROR', 'Seleziona almeno due richieste da confrontare')
        return null
      }
      
      commit('SET_LOADING', true)
      commit('CLEAR_ERROR')
      
      try {
        const response = await api.post('/comparison/requests', requestIds)
        commit('SET_COMPARISON_RESULTS', response.data)
        return response.data
      } catch (error) {
        console.error('Errore durante il confronto delle richieste:', error)
        commit('SET_ERROR', error.response?.data || 'Si è verificato un errore durante il confronto')
        return null
      } finally {
        commit('SET_LOADING', false)
      }
    },
    
    async compareJson({ commit }, { json1, json2 }) {
      if (!json1 || !json2) {
        commit('SET_ERROR', 'Inserisci entrambi i JSON da confrontare')
        return null
      }
      
      commit('SET_LOADING', true)
      commit('CLEAR_ERROR')
      
      try {
        const response = await api.post('/comparison/json', { json1, json2 })
        commit('SET_JSON_DIFF_RESULT', response.data)
        return response.data
      } catch (error) {
        console.error('Errore durante il confronto JSON:', error)
        commit('SET_ERROR', error.response?.data || 'Si è verificato un errore durante il confronto JSON')
        return null
      } finally {
        commit('SET_LOADING', false)
      }
    },
    
    clearResults({ commit }) {
      commit('SET_COMPARISON_RESULTS', null)
      commit('SET_JSON_DIFF_RESULT', null)
      commit('CLEAR_ERROR')
    }
  }
}