import { createStore } from 'vuex';
import axios from 'axios';

export default createStore({
  state: {
    requests: [],
    rules: [],
    selectedRequest: null,
    apiBaseUrl: 'http://localhost:8000/api',
    proxyConfig: {
      targetDomain: '',
      mockMode: false
    },
    loading: {
      requests: false,
      rules: false,
      proxy: false
    },
    error: null
  },
  
  mutations: {
    setRequests(state, requests) {
      state.requests = requests;
    },
    setRules(state, rules) {
      state.rules = rules;
    },
    setSelectedRequest(state, request) {
      state.selectedRequest = request;
    },
    setApiBaseUrl(state, url) {
      state.apiBaseUrl = url;
    },
    setProxyConfig(state, config) {
      state.proxyConfig = config;
    },
    setLoading(state, { key, value }) {
      state.loading[key] = value;
    },
    setError(state, error) {
      state.error = error;
    },
    addRule(state, rule) {
      state.rules.push(rule);
    },
    updateRule(state, updatedRule) {
      const index = state.rules.findIndex(r => r.id === updatedRule.id);
      if (index !== -1) {
        state.rules.splice(index, 1, updatedRule);
      }
    },
    removeRule(state, ruleId) {
      state.rules = state.rules.filter(r => r.id !== ruleId);
    },
    clearRequests(state) {
      state.requests = [];
      state.selectedRequest = null;
    }
  },
  
  actions: {
    async fetchRequests({ commit, state }) {
      commit('setLoading', { key: 'requests', value: true });
      try {
        const response = await axios.get(`${state.apiBaseUrl}/requests`);
        commit('setRequests', response.data);
      } catch (error) {
        console.error('Error fetching requests:', error);
        commit('setError', 'Failed to fetch requests. ' + error.message);
      } finally {
        commit('setLoading', { key: 'requests', value: false });
      }
    },
    
    async fetchRequest({ commit, state }, requestId) {
      commit('setLoading', { key: 'requests', value: true });
      try {
        const response = await axios.get(`${state.apiBaseUrl}/requests/${requestId}`);
        commit('setSelectedRequest', response.data);
      } catch (error) {
        console.error('Error fetching request:', error);
        commit('setError', 'Failed to fetch request details. ' + error.message);
      } finally {
        commit('setLoading', { key: 'requests', value: false });
      }
    },
    
    async fetchRules({ commit, state }) {
      commit('setLoading', { key: 'rules', value: true });
      try {
        const response = await axios.get(`${state.apiBaseUrl}/rules`);
        commit('setRules', response.data);
      } catch (error) {
        console.error('Error fetching rules:', error);
        commit('setError', 'Failed to fetch rules. ' + error.message);
      } finally {
        commit('setLoading', { key: 'rules', value: false });
      }
    },
    
    async createRule({ commit, state }, rule) {
      commit('setLoading', { key: 'rules', value: true });
      try {
        const response = await axios.post(`${state.apiBaseUrl}/rules`, rule);
        commit('addRule', response.data);
        return response.data;
      } catch (error) {
        console.error('Error creating rule:', error);
        commit('setError', 'Failed to create rule. ' + error.message);
        throw error;
      } finally {
        commit('setLoading', { key: 'rules', value: false });
      }
    },
    
    async updateRule({ commit, state }, rule) {
      commit('setLoading', { key: 'rules', value: true });
      try {
        await axios.put(`${state.apiBaseUrl}/rules/${rule.id}`, rule);
        commit('updateRule', rule);
      } catch (error) {
        console.error('Error updating rule:', error);
        commit('setError', 'Failed to update rule. ' + error.message);
        throw error;
      } finally {
        commit('setLoading', { key: 'rules', value: false });
      }
    },
    
    async deleteRule({ commit, state }, ruleId) {
      commit('setLoading', { key: 'rules', value: true });
      try {
        await axios.delete(`${state.apiBaseUrl}/rules/${ruleId}`);
        commit('removeRule', ruleId);
      } catch (error) {
        console.error('Error deleting rule:', error);
        commit('setError', 'Failed to delete rule. ' + error.message);
      } finally {
        commit('setLoading', { key: 'rules', value: false });
      }
    },
    
    async updateProxyConfig({ commit, state }, config) {
      commit('setLoading', { key: 'proxy', value: true });
      try {
        await axios.post(`${state.apiBaseUrl}/proxy/config`, config);
        commit('setProxyConfig', config);
      } catch (error) {
        console.error('Error updating proxy config:', error);
        commit('setError', 'Failed to update proxy configuration. ' + error.message);
      } finally {
        commit('setLoading', { key: 'proxy', value: false });
      }
    },
    
    async clearAllRequests({ commit, state }) {
      commit('setLoading', { key: 'requests', value: true });
      try {
        await axios.delete(`${state.apiBaseUrl}/requests`);
        commit('clearRequests');
      } catch (error) {
        console.error('Error clearing requests:', error);
        commit('setError', 'Failed to clear requests. ' + error.message);
      } finally {
        commit('setLoading', { key: 'requests', value: false });
      }
    }
  },
  
  getters: {
    sortedRequests: state => {
      return [...state.requests].sort((a, b) => {
        return new Date(b.timestamp) - new Date(a.timestamp);
      });
    },
    
    activeRules: state => {
      return state.rules.filter(rule => rule.isActive);
    },
    
    requestsCount: state => {
      return state.requests.length;
    },
    
    rulesCount: state => {
      return state.rules.length;
    },
    
    isLoading: state => {
      return Object.values(state.loading).some(value => value === true);
    }
  }
});
