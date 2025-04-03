import api from '../../services/api'

export default {
  namespaced: true,
  
  state: {
    requests: [],
    currentRequest: null,
    currentResponse: null,
    pagination: {
      currentPage: 1,
      totalPages: 1,
      totalCount: 0,
      pageSize: 20
    },
    filters: {
      method: '',
      url: '',
      isProxied: null
    }
  },
  
  mutations: {
    SET_REQUESTS(state, requests) {
      state.requests = requests
    },
    SET_CURRENT_REQUEST(state, request) {
      state.currentRequest = request
    },
    SET_CURRENT_RESPONSE(state, response) {
      state.currentResponse = response
    },
    SET_PAGINATION(state, pagination) {
      state.pagination = { ...state.pagination, ...pagination }
    },
    SET_FILTERS(state, filters) {
      state.filters = { ...state.filters, ...filters }
    },
    CLEAR_FILTERS(state) {
      state.filters = {
        method: '',
        url: '',
        isProxied: null
      }
    }
  },
  
  actions: {
    async fetchRequests({ commit, state, rootState }) {
      try {
        rootState.isLoading = true
        
        const { currentPage, pageSize } = state.pagination
        const { method, url, isProxied } = state.filters
        
        let queryParams = new URLSearchParams({
          page: currentPage,
          pageSize: pageSize
        })
        
        if (method) queryParams.append('method', method)
        if (url) queryParams.append('url', url)
        if (isProxied !== null) queryParams.append('isProxied', isProxied)
        
        const response = await api.get(`/requests?${queryParams.toString()}`)
        
        commit('SET_REQUESTS', response.data)
        
        // Get pagination info from headers
        const totalCount = parseInt(response.headers['x-total-count'] || '0')
        const totalPages = parseInt(response.headers['x-total-pages'] || '1')
        
        commit('SET_PAGINATION', { totalCount, totalPages })
      } catch (error) {
        console.error('Error fetching requests:', error)
        commit('SET_REQUESTS', [])
      } finally {
        rootState.isLoading = false
      }
    },
    
    async fetchRequestDetails({ commit, rootState }, requestId) {
      try {
        rootState.isLoading = true
        
        // Fetch the request details
        const requestResponse = await api.get(`/requests/${requestId}`)
        commit('SET_CURRENT_REQUEST', requestResponse.data)
        
        // Fetch the response associated with this request
        try {
          const responseData = await api.get(`/requests/${requestId}/response`)
          commit('SET_CURRENT_RESPONSE', responseData.data)
        } catch (error) {
          // It's okay if there's no response yet
          commit('SET_CURRENT_RESPONSE', null)
        }
      } catch (error) {
        console.error('Error fetching request details:', error)
        commit('SET_CURRENT_REQUEST', null)
        commit('SET_CURRENT_RESPONSE', null)
      } finally {
        rootState.isLoading = false
      }
    },
    
    async deleteRequest({ dispatch, rootState }, requestId) {
      try {
        rootState.isLoading = true
        await api.delete(`/requests/${requestId}`)
        dispatch('fetchRequests')
      } catch (error) {
        console.error('Error deleting request:', error)
      } finally {
        rootState.isLoading = false
      }
    },
    
    async clearAllRequests({ dispatch, rootState }) {
      try {
        rootState.isLoading = true
        await api.delete('/requests')
        dispatch('fetchRequests')
      } catch (error) {
        console.error('Error clearing requests:', error)
      } finally {
        rootState.isLoading = false
      }
    },
    
    setPage({ commit, dispatch }, page) {
      commit('SET_PAGINATION', { currentPage: page })
      dispatch('fetchRequests')
    },
    
    applyFilters({ commit, dispatch }, filters) {
      commit('SET_FILTERS', filters)
      commit('SET_PAGINATION', { currentPage: 1 }) // Reset to first page
      dispatch('fetchRequests')
    },
    
    clearFilters({ commit, dispatch }) {
      commit('CLEAR_FILTERS')
      commit('SET_PAGINATION', { currentPage: 1 }) // Reset to first page
      dispatch('fetchRequests')
    }
  },
  
  getters: {
    requestsGroupedByHost(state) {
      const groups = {}
      
      state.requests.forEach(request => {
        try {
          const url = new URL(request.url)
          const host = url.host
          
          if (!groups[host]) {
            groups[host] = []
          }
          
          groups[host].push(request)
        } catch (error) {
          // Handle invalid URLs
          const unknown = 'unknown'
          if (!groups[unknown]) {
            groups[unknown] = []
          }
          groups[unknown].push(request)
        }
      })
      
      return groups
    },
    
    getMethodStats(state) {
      const stats = {
        GET: 0,
        POST: 0,
        PUT: 0,
        DELETE: 0,
        PATCH: 0,
        OTHER: 0
      }
      
      state.requests.forEach(request => {
        const method = request.method.toUpperCase()
        if (stats[method] !== undefined) {
          stats[method]++
        } else {
          stats.OTHER++
        }
      })
      
      return stats
    }
  }
}
