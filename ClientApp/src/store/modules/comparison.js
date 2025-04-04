import api from '../../services/api'

/**
 * Modulo Vuex per la gestione della funzionalità di confronto
 * Gestisce lo stato, le mutazioni e le azioni per il confronto tra richieste e JSON
 */
export default {
  namespaced: true,
  
  state: {
    comparisonResults: null,  // Risultati del confronto tra richieste
    jsonDiffResult: null,     // Risultati del confronto tra JSON personalizzati
    isLoading: false,         // Stato di caricamento
    error: null               // Eventuali errori
  },
  
  mutations: {
    /**
     * Imposta i risultati del confronto tra richieste
     * @param {Object} state - Stato corrente
     * @param {Object} results - Risultati del confronto
     */
    SET_COMPARISON_RESULTS(state, results) {
      state.comparisonResults = results
    },
    
    /**
     * Imposta i risultati del confronto JSON
     * @param {Object} state - Stato corrente
     * @param {Object} result - Risultati del confronto JSON
     */
    SET_JSON_DIFF_RESULT(state, result) {
      state.jsonDiffResult = result
    },
    
    /**
     * Imposta lo stato di caricamento
     * @param {Object} state - Stato corrente
     * @param {boolean} isLoading - Stato di caricamento
     */
    SET_LOADING(state, isLoading) {
      state.isLoading = isLoading
    },
    
    /**
     * Imposta un errore
     * @param {Object} state - Stato corrente
     * @param {string} error - Messaggio di errore
     */
    SET_ERROR(state, error) {
      state.error = error
    },
    
    /**
     * Cancella eventuali errori
     * @param {Object} state - Stato corrente
     */
    CLEAR_ERROR(state) {
      state.error = null
    }
  },
  
  actions: {
    /**
     * Confronta richieste HTTP tramite i loro ID
     * @param {Object} context - Contesto Vuex
     * @param {Array<number>} requestIds - Lista di ID delle richieste da confrontare
     * @returns {Promise<Object|null>} - Risultati del confronto o null in caso di errore
     */
    async compareRequests({ commit }, requestIds) {
      // Validazione parametri
      if (!requestIds || requestIds.length < 2) {
        commit('SET_ERROR', 'Seleziona almeno due richieste da confrontare')
        return null
      }
      
      // Imposta stato di caricamento e pulisce eventuali errori precedenti
      commit('SET_LOADING', true)
      commit('CLEAR_ERROR')
      
      try {
        // Chiamata API
        const response = await api.post('/comparison/requests', requestIds)
        commit('SET_COMPARISON_RESULTS', response.data)
        return response.data
      } catch (error) {
        // Gestione errori
        console.error('Errore durante il confronto delle richieste:', error)
        
        // Estrae il messaggio di errore dalla risposta se disponibile
        const errorMessage = error.response?.data || 'Si è verificato un errore durante il confronto'
        commit('SET_ERROR', errorMessage)
        return null
      } finally {
        // Ripristina lo stato di caricamento
        commit('SET_LOADING', false)
      }
    },
    
    /**
     * Confronta due stringhe JSON personalizzate
     * @param {Object} context - Contesto Vuex
     * @param {Object} payload - Oggetto contenente json1 e json2
     * @param {string} payload.json1 - Prima stringa JSON
     * @param {string} payload.json2 - Seconda stringa JSON
     * @returns {Promise<Object|null>} - Risultati del confronto o null in caso di errore
     */
    async compareJson({ commit }, { json1, json2 }) {
      // Validazione parametri
      if (!json1 || !json2) {
        commit('SET_ERROR', 'Inserisci entrambi i JSON da confrontare')
        return null
      }
      
      // Imposta stato di caricamento e pulisce eventuali errori precedenti
      commit('SET_LOADING', true)
      commit('CLEAR_ERROR')
      
      try {
        // Chiamata API
        const response = await api.post('/comparison/json', { json1, json2 })
        commit('SET_JSON_DIFF_RESULT', response.data)
        return response.data
      } catch (error) {
        // Gestione errori
        console.error('Errore durante il confronto JSON:', error)
        
        // Estrae il messaggio di errore dalla risposta se disponibile
        const errorMessage = error.response?.data || 'Si è verificato un errore durante il confronto JSON'
        commit('SET_ERROR', errorMessage)
        return null
      } finally {
        // Ripristina lo stato di caricamento
        commit('SET_LOADING', false)
      }
    },
    
    /**
     * Pulisce tutti i risultati di confronto
     * @param {Object} context - Contesto Vuex
     */
    clearResults({ commit }) {
      commit('SET_COMPARISON_RESULTS', null)
      commit('SET_JSON_DIFF_RESULT', null)
      commit('CLEAR_ERROR')
    }
  },
  
  // Getters per accedere allo stato in modo più strutturato
  getters: {
    /**
     * Verifica se ci sono risultati di confronto disponibili
     * @param {Object} state - Stato corrente
     * @returns {boolean} - true se ci sono risultati disponibili
     */
    hasComparisonResults: state => !!state.comparisonResults,
    
    /**
     * Verifica se ci sono risultati di confronto JSON disponibili
     * @param {Object} state - Stato corrente
     * @returns {boolean} - true se ci sono risultati disponibili
     */
    hasJsonDiffResults: state => !!state.jsonDiffResult
  }
}