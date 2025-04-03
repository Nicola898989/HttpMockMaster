import axios from 'axios';
import store from '../store';

// Create axios instance
const api = axios.create({
  baseURL: 'http://localhost:8000/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// Update baseURL when apiBaseUrl changes in the store
store.watch(
  state => state.apiBaseUrl,
  (newBaseUrl) => {
    api.defaults.baseURL = newBaseUrl;
  }
);

// Request interceptor
api.interceptors.request.use(
  config => {
    // You can add auth tokens here if needed
    return config;
  },
  error => {
    return Promise.reject(error);
  }
);

// Response interceptor
api.interceptors.response.use(
  response => {
    return response;
  },
  error => {
    // Handle errors globally
    if (error.response) {
      // The request was made and the server responded with a status code
      // that falls out of the range of 2xx
      console.error('API error response:', error.response.data);
    } else if (error.request) {
      // The request was made but no response was received
      console.error('API no response received:', error.request);
    } else {
      // Something happened in setting up the request that triggered an Error
      console.error('API error:', error.message);
    }
    return Promise.reject(error);
  }
);

export default {
  // Requests
  getRequests(params = {}) {
    return api.get('/requests', { params });
  },
  
  getRequest(id) {
    return api.get(`/requests/${id}`);
  },
  
  searchRequests(params) {
    return api.get('/requests/search', { params });
  },
  
  deleteRequest(id) {
    return api.delete(`/requests/${id}`);
  },
  
  clearAllRequests() {
    return api.delete('/requests');
  },
  
  // Rules
  getRules() {
    return api.get('/rules');
  },
  
  getRule(id) {
    return api.get(`/rules/${id}`);
  },
  
  createRule(rule) {
    return api.post('/rules', rule);
  },
  
  updateRule(rule) {
    return api.put(`/rules/${rule.id}`, rule);
  },
  
  deleteRule(id) {
    return api.delete(`/rules/${id}`);
  },
  
  // Proxy
  updateProxyConfig(config) {
    return api.post('/proxy/config', config);
  }
};
