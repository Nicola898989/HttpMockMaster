import api from '../../services/api'

export default {
  namespaced: true,
  
  state: {
    isProxyEnabled: false,
    targetDomain: '',
    proxyStatus: 'inactive' // 'inactive', 'active', 'error'
  },
  
  mutations: {
    SET_PROXY_STATUS(state, status) {
      state.proxyStatus = status
    },
    SET_TARGET_DOMAIN(state, domain) {
      state.targetDomain = domain
    },
    SET_PROXY_ENABLED(state, isEnabled) {
      state.isProxyEnabled = isEnabled
    }
  },
  
  actions: {
    async enableProxy({ commit, rootState }, targetDomain) {
      try {
        rootState.isLoading = true
        commit('SET_PROXY_STATUS', 'pending')
        
        await api.post('/proxy/configure', { targetDomain })
        
        commit('SET_TARGET_DOMAIN', targetDomain)
        commit('SET_PROXY_ENABLED', true)
        commit('SET_PROXY_STATUS', 'active')
        
        return true
      } catch (error) {
        console.error('Error enabling proxy:', error)
        commit('SET_PROXY_STATUS', 'error')
        return false
      } finally {
        rootState.isLoading = false
      }
    },
    
    async disableProxy({ commit, rootState }) {
      try {
        rootState.isLoading = true
        
        await api.post('/proxy/disable')
        
        commit('SET_TARGET_DOMAIN', '')
        commit('SET_PROXY_ENABLED', false)
        commit('SET_PROXY_STATUS', 'inactive')
        
        return true
      } catch (error) {
        console.error('Error disabling proxy:', error)
        commit('SET_PROXY_STATUS', 'error')
        return false
      } finally {
        rootState.isLoading = false
      }
    }
  }
}
