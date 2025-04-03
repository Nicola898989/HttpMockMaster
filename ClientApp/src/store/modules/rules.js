import api from '../../services/api'

export default {
  namespaced: true,
  
  state: {
    rules: [],
    currentRule: null,
    isEditing: false
  },
  
  mutations: {
    SET_RULES(state, rules) {
      state.rules = rules
    },
    SET_CURRENT_RULE(state, rule) {
      state.currentRule = rule
    },
    SET_IS_EDITING(state, isEditing) {
      state.isEditing = isEditing
    },
    RESET_CURRENT_RULE(state) {
      state.currentRule = {
        name: '',
        description: '',
        method: '',
        pathPattern: '',
        queryPattern: '',
        headerPattern: '',
        bodyPattern: '',
        priority: 0,
        isActive: true,
        response: {
          statusCode: 200,
          headers: 'Content-Type: application/json',
          body: '{}'
        }
      }
    }
  },
  
  actions: {
    async fetchRules({ commit, rootState }) {
      try {
        rootState.isLoading = true
        const response = await api.get('/rules')
        commit('SET_RULES', response.data)
      } catch (error) {
        console.error('Error fetching rules:', error)
        commit('SET_RULES', [])
      } finally {
        rootState.isLoading = false
      }
    },
    
    async fetchRuleById({ commit, rootState }, ruleId) {
      try {
        rootState.isLoading = true
        const response = await api.get(`/rules/${ruleId}`)
        commit('SET_CURRENT_RULE', response.data)
        commit('SET_IS_EDITING', true)
      } catch (error) {
        console.error('Error fetching rule:', error)
        commit('SET_CURRENT_RULE', null)
      } finally {
        rootState.isLoading = false
      }
    },
    
    async createRule({ dispatch, commit, rootState }, rule) {
      try {
        rootState.isLoading = true
        await api.post('/rules', rule)
        dispatch('fetchRules')
        return true
      } catch (error) {
        console.error('Error creating rule:', error)
        return false
      } finally {
        rootState.isLoading = false
      }
    },
    
    async updateRule({ dispatch, rootState }, rule) {
      try {
        rootState.isLoading = true
        await api.put(`/rules/${rule.id}`, rule)
        dispatch('fetchRules')
        return true
      } catch (error) {
        console.error('Error updating rule:', error)
        return false
      } finally {
        rootState.isLoading = false
      }
    },
    
    async deleteRule({ dispatch, rootState }, ruleId) {
      try {
        rootState.isLoading = true
        await api.delete(`/rules/${ruleId}`)
        dispatch('fetchRules')
        return true
      } catch (error) {
        console.error('Error deleting rule:', error)
        return false
      } finally {
        rootState.isLoading = false
      }
    },
    
    startNewRule({ commit }) {
      commit('RESET_CURRENT_RULE')
      commit('SET_IS_EDITING', false)
    },
    
    editRule({ commit }, rule) {
      commit('SET_CURRENT_RULE', rule)
      commit('SET_IS_EDITING', true)
    },
    
    cancelEdit({ commit }) {
      commit('SET_CURRENT_RULE', null)
      commit('SET_IS_EDITING', false)
    }
  },
  
  getters: {
    activeRules(state) {
      return state.rules.filter(rule => rule.isActive)
    },
    
    inactiveRules(state) {
      return state.rules.filter(rule => !rule.isActive)
    },
    
    rulesByPriority(state) {
      return [...state.rules].sort((a, b) => b.priority - a.priority)
    }
  }
}
