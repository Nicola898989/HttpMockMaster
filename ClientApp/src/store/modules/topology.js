import Vue from 'vue'
import { api } from '@/services/api'

// Stato iniziale
const state = {
  nodes: [],
  connections: [],
  selectedNode: null,
  selectedConnection: null,
  isLoading: false,
  error: null,
  savedTopologies: [],
  historyState: []
}

// Mutations
const mutations = {
  SET_NODES(state, nodes) {
    state.nodes = nodes
  },
  SET_CONNECTIONS(state, connections) {
    state.connections = connections
  },
  SET_HISTORY_STATE(state, historyState) {
    state.historyState = historyState
  },
  ADD_NODE(state, node) {
    state.nodes.push(node)
  },
  UPDATE_NODE(state, { index, node }) {
    Vue.set(state.nodes, index, node)
  },
  REMOVE_NODE(state, index) {
    state.nodes.splice(index, 1)
    
    // Aggiorna le connessioni
    state.connections = state.connections.filter(conn => 
      conn.source !== index && conn.target !== index
    )
    
    // Aggiorna gli indici delle connessioni
    state.connections = state.connections.map(conn => {
      return {
        ...conn,
        source: conn.source > index ? conn.source - 1 : conn.source,
        target: conn.target > index ? conn.target - 1 : conn.target
      }
    })
  },
  ADD_CONNECTION(state, connection) {
    state.connections.push(connection)
  },
  UPDATE_CONNECTION(state, { index, connection }) {
    Vue.set(state.connections, index, connection)
  },
  REMOVE_CONNECTION(state, index) {
    state.connections.splice(index, 1)
  },
  SET_SELECTED_NODE(state, index) {
    state.selectedNode = index
    state.selectedConnection = null
  },
  SET_SELECTED_CONNECTION(state, index) {
    state.selectedConnection = index
    state.selectedNode = null
  },
  CLEAR_TOPOLOGY(state) {
    state.nodes = []
    state.connections = []
    state.selectedNode = null
    state.selectedConnection = null
  },
  SET_LOADING(state, isLoading) {
    state.isLoading = isLoading
  },
  SET_ERROR(state, error) {
    state.error = error
  },
  SET_SAVED_TOPOLOGIES(state, topologies) {
    state.savedTopologies = topologies
  }
}

// Actions
const actions = {
  async loadTopology({ commit }) {
    commit('SET_LOADING', true)
    try {
      // Carica dalla localStorage
      const savedData = localStorage.getItem('network-topology')
      if (savedData) {
        const parsedData = JSON.parse(savedData)
        commit('SET_NODES', parsedData.nodes || [])
        commit('SET_CONNECTIONS', parsedData.connections || [])
      }
    } catch (error) {
      commit('SET_ERROR', `Errore nel caricamento della topologia: ${error.message}`)
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  async saveTopology({ state, commit }, name) {
    commit('SET_LOADING', true)
    try {
      const topologyData = {
        nodes: state.nodes,
        connections: state.connections,
        name: name || 'Topologia di rete',
        timestamp: new Date().toISOString()
      }
      
      // Salva in localStorage
      localStorage.setItem('network-topology', JSON.stringify(topologyData))
      
      // In futuro: Salvataggio sul server tramite API
      // await api.saveTopology(topologyData)
      
      return true
    } catch (error) {
      commit('SET_ERROR', `Errore nel salvataggio della topologia: ${error.message}`)
      return false
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  // Aggiunge lo stato attuale allo storico prima di una modifica
  saveHistoryState({ state, commit }) {
    const currentState = {
      nodes: [...state.nodes],
      connections: [...state.connections]
    }
    const history = [...state.historyState]
    history.push(currentState)
    // Limita la dimensione dello storico a 20 elementi
    if (history.length > 20) {
      history.shift()
    }
    commit('SET_HISTORY_STATE', history)
  },
  
  // Annulla l'ultima azione ripristinando lo stato precedente
  undo({ state, commit }) {
    if (state.historyState.length === 0) {
      return false
    }
    
    const history = [...state.historyState]
    const previousState = history.pop()
    
    commit('SET_NODES', previousState.nodes)
    commit('SET_CONNECTIONS', previousState.connections)
    commit('SET_HISTORY_STATE', history)
    
    return true
  },
  
  addNode({ commit, dispatch }, node) {
    dispatch('saveHistoryState')
    commit('ADD_NODE', node)
  },
  
  updateNode({ commit, dispatch }, { index, node }) {
    dispatch('saveHistoryState')
    commit('UPDATE_NODE', { index, node })
  },
  
  removeNode({ commit, dispatch }, index) {
    dispatch('saveHistoryState')
    commit('REMOVE_NODE', index)
  },
  
  addConnection({ commit, dispatch }, connection) {
    dispatch('saveHistoryState')
    commit('ADD_CONNECTION', connection)
  },
  
  updateConnection({ commit, dispatch }, { index, connection }) {
    dispatch('saveHistoryState')
    commit('UPDATE_CONNECTION', { index, connection })
  },
  
  removeConnection({ commit, dispatch }, index) {
    dispatch('saveHistoryState')
    commit('REMOVE_CONNECTION', index)
  },
  
  selectNode({ commit }, index) {
    commit('SET_SELECTED_NODE', index)
  },
  
  selectConnection({ commit }, index) {
    commit('SET_SELECTED_CONNECTION', index)
  },
  
  clearTopology({ commit, dispatch }) {
    dispatch('saveHistoryState')
    commit('CLEAR_TOPOLOGY')
  }
}

// Getters
const getters = {
  clientNodes: state => {
    return state.nodes.filter(node => node.type === 'client')
  },
  
  serverNodes: state => {
    return state.nodes.filter(node => node.type === 'server')
  },
  
  proxyNodes: state => {
    return state.nodes.filter(node => node.type === 'proxy')
  },
  
  connectionsByType: state => (type) => {
    return state.connections.filter(conn => conn.type === type)
  }
}

export default {
  namespaced: true,
  state,
  mutations,
  actions,
  getters
}