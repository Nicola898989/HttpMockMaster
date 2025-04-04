<template>
  <div class="network-topology-container">
    <div class="topology-header">
      <h2>Topologia di Rete</h2>
      <div class="topology-controls">
        <button @click="addNode('client')" class="control-btn">
          <i class="feather-icon" data-feather="monitor"></i> Aggiungi Client
        </button>
        <button @click="addNode('server')" class="control-btn">
          <i class="feather-icon" data-feather="server"></i> Aggiungi Server
        </button>
        <button @click="addNode('proxy')" class="control-btn">
          <i class="feather-icon" data-feather="shield"></i> Aggiungi Proxy
        </button>
        <button @click="undoLastAction" class="control-btn warning">
          <i class="feather-icon" data-feather="rotate-ccw"></i> Annulla
        </button>
        <button @click="clearTopology" class="control-btn danger">
          <i class="feather-icon" data-feather="trash-2"></i> Pulisci
        </button>
        <button @click="saveTopology" class="control-btn success">
          <i class="feather-icon" data-feather="save"></i> Salva
        </button>
      </div>
    </div>
    
    <div 
      ref="topologyCanvas" 
      class="topology-canvas"
      @dragover.prevent
      @drop.prevent="onDrop">
      <!-- Nodi della rete -->
      <div
        v-for="(node, index) in nodes"
        :key="`node-${index}`"
        class="network-node"
        :class="[`node-type-${node.type}`, { 'selected': selectedNode === index }]"
        :style="{ top: `${node.y}px`, left: `${node.x}px` }"
        draggable="true"
        @dragstart="onDragStart($event, index)"
        @click="selectNode(index)"
      >
        <div class="node-icon">
          <i class="feather-icon" :data-feather="getNodeIcon(node.type)"></i>
        </div>
        <div class="node-name">{{ node.name }}</div>
        <div class="node-actions" v-if="selectedNode === index">
          <button @click.stop="editNode(index)" class="node-btn">
            <i class="feather-icon" data-feather="edit-2"></i>
          </button>
          <button @click.stop="deleteNode(index)" class="node-btn delete">
            <i class="feather-icon" data-feather="x"></i>
          </button>
          <button @click.stop="startConnection(index)" class="node-btn connect">
            <i class="feather-icon" data-feather="link"></i>
          </button>
        </div>
      </div>

      <!-- Connessioni tra nodi -->
      <svg class="connections-layer">
        <g v-for="(connection, index) in connections" :key="`connection-${index}`">
          <path 
            :d="getConnectionPath(connection)" 
            :class="['connection-path', { 'selected': selectedConnection === index }]" 
            @click="selectConnection(index)"
          />
          <circle 
            v-if="connection.animated"
            :cx="0"
            :cy="0"
            r="5"
            class="connection-animation"
            :class="`animation-${connection.type}`"
          >
            <animateMotion 
              :path="getConnectionPath(connection)" 
              dur="2s" 
              repeatCount="indefinite"
            />
          </circle>
        </g>
      </svg>

      <!-- Indicatore di nuova connessione durante il trascinamento -->
      <svg v-if="connectingNodes" class="temp-connection-layer">
        <path 
          :d="tempConnectionPath" 
          class="temp-connection-path" 
        />
      </svg>
    </div>

    <!-- Modal per modifica nodo -->
    <div v-if="showNodeModal" class="node-edit-modal">
      <div class="modal-content">
        <h3>{{ isNewNode ? 'Nuovo nodo' : 'Modifica nodo' }}</h3>
        <div class="form-group">
          <label>Nome:</label>
          <input v-model="editingNode.name" type="text" class="form-control">
        </div>
        <div class="form-group">
          <label>Tipo:</label>
          <select v-model="editingNode.type" class="form-control">
            <option value="client">Client</option>
            <option value="server">Server</option>
            <option value="proxy">Proxy</option>
          </select>
        </div>
        <div class="form-group" v-if="editingNode.type === 'server'">
          <label>URL:</label>
          <input v-model="editingNode.url" type="text" class="form-control">
        </div>
        <div class="form-group" v-if="editingNode.type === 'proxy'">
          <label>Porta:</label>
          <input v-model="editingNode.port" type="number" class="form-control">
        </div>
        <div class="modal-actions">
          <button @click="saveNode" class="btn success">Salva</button>
          <button @click="cancelNodeEdit" class="btn">Annulla</button>
        </div>
      </div>
    </div>

    <!-- Modal per modifica connessione -->
    <div v-if="showConnectionModal" class="connection-edit-modal">
      <div class="modal-content">
        <h3>Modifica connessione</h3>
        <div class="form-group">
          <label>Tipo:</label>
          <select v-model="editingConnection.type" class="form-control">
            <option value="request">Richiesta</option>
            <option value="response">Risposta</option>
            <option value="proxy">Proxy</option>
          </select>
        </div>
        <div class="form-group">
          <label>Descrizione:</label>
          <input v-model="editingConnection.label" type="text" class="form-control">
        </div>
        <div class="form-group">
          <label>Animato:</label>
          <input v-model="editingConnection.animated" type="checkbox" class="form-control-checkbox">
        </div>
        <div class="modal-actions">
          <button @click="saveConnection" class="btn success">Salva</button>
          <button @click="cancelConnectionEdit" class="btn">Annulla</button>
          <button @click="deleteConnection" class="btn danger">Elimina</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import feather from 'feather-icons';
import { gsap } from 'gsap';
import { mapState, mapActions, mapGetters } from 'vuex';

export default {
  name: 'NetworkTopology',
  data() {
    return {
      connectingNodes: false,
      connectingFrom: null,
      mousePosition: { x: 0, y: 0 },
      showNodeModal: false,
      showConnectionModal: false,
      editingNode: { name: '', type: 'client', x: 0, y: 0 },
      editingConnection: { source: null, target: null, type: 'request', label: '', animated: true },
      editingNodeIndex: null,
      editingConnectionIndex: null,
      isNewNode: true,
      tempConnectionPath: '',
      undoStack: []
    };
  },
  computed: {
    ...mapState('topology', [
      'nodes', 
      'connections', 
      'selectedNode', 
      'selectedConnection',
      'isLoading',
      'error',
      'historyState'
    ]),
    ...mapGetters('topology', [
      'clientNodes',
      'serverNodes',
      'proxyNodes',
      'connectionsByType'
    ])
  },
  mounted() {
    this.renderIcons();
    this.loadTopology();
    
    // Aggiungi event listener per il mouse move per la nuova connessione
    window.addEventListener('mousemove', this.handleMouseMove);
    
    // Aggiungi event listener per completare una connessione
    window.addEventListener('mouseup', this.completeConnection);
  },
  updated() {
    this.renderIcons();
  },
  beforeDestroy() {
    window.removeEventListener('mousemove', this.handleMouseMove);
    window.removeEventListener('mouseup', this.completeConnection);
  },
  methods: {
    renderIcons() {
      this.$nextTick(() => {
        feather.replace({ class: 'feather-icon' });
      });
    },
    addNode(type) {
      this.isNewNode = true;
      this.editingNode = {
        name: this.getDefaultName(type),
        type: type,
        x: 100,
        y: 100,
        url: type === 'server' ? 'https://example.com' : '',
        port: type === 'proxy' ? 8888 : null
      };
      this.showNodeModal = true;
    },
    getDefaultName(type) {
      const count = this.nodes.filter(n => n.type === type).length + 1;
      const typeMap = {
        'client': 'Client',
        'server': 'Server',
        'proxy': 'Proxy'
      };
      return `${typeMap[type]} ${count}`;
    },
    getNodeIcon(type) {
      const iconMap = {
        'client': 'monitor',
        'server': 'server',
        'proxy': 'shield'
      };
      return iconMap[type] || 'circle';
    },
    saveNode() {
      if (this.isNewNode) {
        // Posiziona il nuovo nodo in un punto visibile
        const canvas = this.$refs.topologyCanvas;
        this.editingNode.x = Math.random() * (canvas.clientWidth - 100) + 50;
        this.editingNode.y = Math.random() * (canvas.clientHeight - 100) + 50;
        
        this.$store.dispatch('topology/addNode', { ...this.editingNode });
        
        // Animazione per enfatizzare il nuovo nodo
        this.$nextTick(() => {
          const lastIndex = this.nodes.length - 1;
          const nodeElement = this.$el.querySelectorAll('.network-node')[lastIndex];
          gsap.from(nodeElement, {
            scale: 0,
            opacity: 0,
            duration: 0.5,
            ease: 'back.out(1.7)'
          });
        });
      } else {
        // Aggiorna il nodo esistente
        const updatedNode = { ...this.editingNode };
        this.$store.dispatch('topology/updateNode', { 
          index: this.editingNodeIndex, 
          node: updatedNode 
        });
      }
      
      this.showNodeModal = false;
    },
    cancelNodeEdit() {
      this.showNodeModal = false;
    },
    editNode(index) {
      this.isNewNode = false;
      this.editingNodeIndex = index;
      this.editingNode = { ...this.nodes[index] };
      this.showNodeModal = true;
    },
    deleteNode(index) {
      // Rimuovi il nodo utilizzando lo store
      this.$store.dispatch('topology/removeNode', index);
      this.selectedNode = null;
    },
    selectNode(index) {
      this.selectedNode = index;
      this.selectedConnection = null;
      
      // Anima la selezione
      const nodeElement = this.$el.querySelectorAll('.network-node')[index];
      gsap.to(nodeElement, {
        scale: 1.05,
        duration: 0.2,
        yoyo: true,
        repeat: 1,
        ease: 'power1.inOut'
      });
    },
    startConnection(index) {
      this.connectingNodes = true;
      this.connectingFrom = index;
    },
    completeConnection(event) {
      if (!this.connectingNodes) return;
      
      // Determina se ci troviamo sopra un nodo quando rilasciamo il mouse
      const targetElement = document.elementFromPoint(event.clientX, event.clientY);
      const targetNode = targetElement?.closest('.network-node');
      
      if (targetNode) {
        const nodeElements = Array.from(this.$el.querySelectorAll('.network-node'));
        const targetIndex = nodeElements.indexOf(targetNode);
        
        if (targetIndex !== -1 && targetIndex !== this.connectingFrom) {
          // Controlla se la connessione già esiste
          const existingConnection = this.connections.find(conn =>
            (conn.source === this.connectingFrom && conn.target === targetIndex) ||
            (conn.source === targetIndex && conn.target === this.connectingFrom)
          );
          
          if (!existingConnection) {
            // Crea una nuova connessione
            const newConnection = {
              source: this.connectingFrom,
              target: targetIndex,
              type: this.determineConnectionType(this.nodes[this.connectingFrom].type, this.nodes[targetIndex].type),
              label: '',
              animated: true
            };
            
            // Aggiungi la connessione usando l'action dello store
            this.$store.dispatch('topology/addConnection', newConnection);
            
            // Trova l'indice della nuova connessione
            const newConnectionIndex = this.connections.findIndex(conn => 
              conn.source === newConnection.source && 
              conn.target === newConnection.target
            );
            
            if (newConnectionIndex !== -1) {
              this.editConnection(newConnectionIndex);
            
              // Anima la nuova connessione
              this.$nextTick(() => {
                const paths = this.$el.querySelectorAll('.connection-path');
                const lastPath = paths[newConnectionIndex];
                if (lastPath) {
                  gsap.from(lastPath, {
                    strokeDashoffset: lastPath.getTotalLength(),
                    strokeDasharray: lastPath.getTotalLength(),
                    duration: 0.8,
                    ease: 'power1.inOut'
                  });
                }
              });
            }
          } else {
            // Se la connessione esiste già, selezionala
            const existingIndex = this.connections.indexOf(existingConnection);
            this.selectConnection(existingIndex);
          }
        }
      }
      
      this.connectingNodes = false;
      this.connectingFrom = null;
      this.tempConnectionPath = '';
    },
    determineConnectionType(sourceType, targetType) {
      if (sourceType === 'client' && targetType === 'server') return 'request';
      if (sourceType === 'server' && targetType === 'client') return 'response';
      if (sourceType === 'proxy' || targetType === 'proxy') return 'proxy';
      return 'request';
    },
    handleMouseMove(event) {
      if (this.connectingNodes) {
        // Aggiorna la posizione del mouse
        const canvasRect = this.$refs.topologyCanvas.getBoundingClientRect();
        const mouseX = event.clientX - canvasRect.left;
        const mouseY = event.clientY - canvasRect.top;
        
        // Aggiorna il percorso della connessione temporanea
        const sourceNode = this.nodes[this.connectingFrom];
        const sourceX = sourceNode.x + 50; // Centro del nodo
        const sourceY = sourceNode.y + 50;
        
        this.tempConnectionPath = `M ${sourceX},${sourceY} L ${mouseX},${mouseY}`;
      }
    },
    getConnectionPath(connection) {
      const source = this.nodes[connection.source];
      const target = this.nodes[connection.target];
      
      if (!source || !target) return '';
      
      // Calcola i punti di partenza e arrivo (centri dei nodi)
      const sourceX = source.x + 50;
      const sourceY = source.y + 50;
      const targetX = target.x + 50;
      const targetY = target.y + 50;
      
      // Crea una curva Bezier per una connessione più elegante
      const dx = targetX - sourceX;
      const dy = targetY - sourceY;
      const distance = Math.sqrt(dx * dx + dy * dy);
      
      // Aggiunge una curva più pronunciata per distanze maggiori
      const curvature = Math.min(0.5, distance / 500);
      
      // Calcola i punti di controllo
      const controlX1 = sourceX + dx * 0.25 + dy * curvature;
      const controlY1 = sourceY + dy * 0.25 - dx * curvature;
      const controlX2 = sourceX + dx * 0.75 - dy * curvature;
      const controlY2 = sourceY + dy * 0.75 + dx * curvature;
      
      return `M ${sourceX},${sourceY} C ${controlX1},${controlY1} ${controlX2},${controlY2} ${targetX},${targetY}`;
    },
    selectConnection(index) {
      this.selectedConnection = index;
      this.selectedNode = null;
      
      // Anima la selezione
      const connectionPath = this.$el.querySelectorAll('.connection-path')[index];
      const originalStrokeWidth = getComputedStyle(connectionPath).strokeWidth;
      
      gsap.fromTo(connectionPath, 
        { strokeWidth: originalStrokeWidth },
        { 
          strokeWidth: '6px', 
          duration: 0.2,
          yoyo: true,
          repeat: 1,
          ease: 'power1.inOut',
          onComplete: () => {
            connectionPath.style.strokeWidth = originalStrokeWidth;
          }
        }
      );
    },
    editConnection(index) {
      this.editingConnectionIndex = index;
      this.editingConnection = { ...this.connections[index] };
      this.showConnectionModal = true;
    },
    saveConnection() {
      // Aggiorna la connessione esistente
      const updatedConnection = { ...this.editingConnection };
      this.$store.dispatch('topology/updateConnection', {
        index: this.editingConnectionIndex,
        connection: updatedConnection
      });
      this.showConnectionModal = false;
    },
    cancelConnectionEdit() {
      this.showConnectionModal = false;
    },
    deleteConnection() {
      if (this.editingConnectionIndex !== null) {
        this.$store.dispatch('topology/removeConnection', this.editingConnectionIndex);
        this.selectedConnection = null;
        this.showConnectionModal = false;
      }
    },
    onDragStart(event, index) {
      event.dataTransfer.setData('node-index', index);
      event.dataTransfer.effectAllowed = 'move';
      
      // Usa un'immagine trasparente per il drag ghost personalizzato
      const img = new Image();
      img.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';
      event.dataTransfer.setDragImage(img, 0, 0);
      
      // Salva lo stato corrente per poterlo annullare
      this.pushToUndoStack();
    },
    onDrop(event) {
      const index = event.dataTransfer.getData('node-index');
      if (index !== '') {
        const canvasRect = this.$refs.topologyCanvas.getBoundingClientRect();
        const nodeIndex = parseInt(index);
        
        // Calcola la nuova posizione tenendo conto dell'offset del mouse all'interno del nodo
        const newX = event.clientX - canvasRect.left - 50;
        const newY = event.clientY - canvasRect.top - 50;
        
        // Aggiorna la posizione del nodo usando lo store
        const updatedNode = { ...this.nodes[nodeIndex], x: newX, y: newY };
        this.$store.dispatch('topology/updateNode', { index: nodeIndex, node: updatedNode });
        
        // Animazione per uno spostamento più fluido
        const nodeElement = this.$el.querySelectorAll('.network-node')[nodeIndex];
        gsap.to(nodeElement, {
          scale: 1.05,
          duration: 0.1,
          yoyo: true,
          repeat: 1,
          ease: 'power1.inOut'
        });
      }
    },
    clearTopology() {
      // Conferma prima di eliminare tutto
      if (confirm('Sei sicuro di voler eliminare tutti gli elementi della topologia?')) {
        this.pushToUndoStack();
        this.$store.dispatch('topology/clearTopology');
        this.selectedNode = null;
        this.selectedConnection = null;
      }
    },
    pushToUndoStack() {
      // Salva lo stato corrente per l'annullamento
      const currentState = {
        nodes: JSON.parse(JSON.stringify(this.nodes)),
        connections: JSON.parse(JSON.stringify(this.connections)),
      };
      this.undoStack.push(currentState);
      
      // Limita la dimensione dello stack
      if (this.undoStack.length > 10) {
        this.undoStack.shift();
      }
    },
    undo() {
      if (this.undoStack.length > 0) {
        const previousState = this.undoStack.pop();
        this.$store.dispatch('topology/setNodes', previousState.nodes);
        this.$store.dispatch('topology/setConnections', previousState.connections);
      }
    },
    undoLastAction() {
      // Prima prova a usare la funzionalità di undo dello store
      this.$store.dispatch('topology/undo').then(() => {
        // Mostra una piccola animazione per indicare l'azione completata
        const topologyCanvas = this.$refs.topologyCanvas;
        if (topologyCanvas) {
          gsap.fromTo(topologyCanvas, 
            { boxShadow: '0 0 0 3px rgba(52, 152, 219, 0.5)' },
            { 
              boxShadow: '0 0 0 0px rgba(52, 152, 219, 0)', 
              duration: 0.5,
              ease: 'power1.out'
            }
          );
        }
      });
    },
    saveTopologyToLocalStorage() {
      this.$store.dispatch('topology/saveTopology');
    },
    loadTopology() {
      this.$store.dispatch('topology/loadTopology');
    },
    saveTopology() {
      // Richiedi il nome per la topologia
      const name = prompt('Inserisci un nome per questa topologia:', 'Topologia di rete');
      
      if (name) {
        this.$store.dispatch('topology/saveTopology', name);
        alert('Topologia salvata con successo!');
      }
    }
  }
};
</script>

<style scoped>
.network-topology-container {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
  width: 100%;
  overflow: hidden;
  background-color: #f5f7fa;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.topology-header {
  padding: 15px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #e0e6ed;
  background-color: #fff;
}

.topology-controls {
  display: flex;
  gap: 10px;
}

.control-btn {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 6px 12px;
  border: 1px solid #d1d9e6;
  border-radius: 4px;
  background-color: #fff;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.control-btn:hover {
  background-color: #f0f3f8;
}

.control-btn.danger {
  border-color: #e74c3c;
  color: #e74c3c;
}

.control-btn.danger:hover {
  background-color: #fde8e6;
}

.control-btn.success {
  border-color: #2ecc71;
  color: #2ecc71;
}

.control-btn.success:hover {
  background-color: #e6f7ef;
}

.control-btn.warning {
  border-color: #f39c12;
  color: #f39c12;
}

.control-btn.warning:hover {
  background-color: #fef5e7;
}

.topology-canvas {
  position: relative;
  flex: 1;
  overflow: auto;
  padding: 20px;
  background-color: #f5f7fa;
  background-image: radial-gradient(#e0e6ed 1px, transparent 0);
  background-size: 20px 20px;
}

.network-node {
  position: absolute;
  width: 100px;
  height: 100px;
  border-radius: 8px;
  background-color: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  cursor: move;
  transition: all 0.2s ease;
  user-select: none;
  z-index: 10;
}

.network-node.selected {
  box-shadow: 0 0 0 2px #3498db, 0 2px 10px rgba(0, 0, 0, 0.2);
  z-index: 20;
}

.node-type-client {
  border-top: 4px solid #3498db;
}

.node-type-server {
  border-top: 4px solid #2ecc71;
}

.node-type-proxy {
  border-top: 4px solid #f39c12;
}

.node-icon {
  font-size: 24px;
  color: #4a5568;
  margin-bottom: 8px;
}

.node-name {
  font-size: 12px;
  text-align: center;
  color: #4a5568;
  word-break: break-word;
  max-width: 90px;
}

.node-actions {
  position: absolute;
  top: -10px;
  right: -10px;
  display: flex;
  gap: 5px;
  background-color: #fff;
  border-radius: 4px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
  padding: 2px;
}

.node-btn {
  width: 24px;
  height: 24px;
  border: none;
  background-color: transparent;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
}

.node-btn:hover {
  background-color: #f0f3f8;
}

.node-btn.delete:hover {
  background-color: #fde8e6;
  color: #e74c3c;
}

.node-btn.connect:hover {
  background-color: #e6f7ef;
  color: #2ecc71;
}

.connections-layer {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 5;
}

.connection-path {
  fill: none;
  stroke: #718096;
  stroke-width: 2px;
  pointer-events: stroke;
  transition: stroke 0.2s;
}

.connection-path.selected {
  stroke: #3498db;
  stroke-width: 3px;
}

.connection-animation {
  fill: #3498db;
}

.animation-request {
  fill: #3498db;
}

.animation-response {
  fill: #2ecc71;
}

.animation-proxy {
  fill: #f39c12;
}

.temp-connection-layer {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 6;
}

.temp-connection-path {
  fill: none;
  stroke: #3498db;
  stroke-width: 2px;
  stroke-dasharray: 5;
}

.node-edit-modal,
.connection-edit-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background-color: #fff;
  padding: 20px;
  border-radius: 8px;
  width: 100%;
  max-width: 400px;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.2);
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

.form-control {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #d1d9e6;
  border-radius: 4px;
  font-size: 14px;
}

.form-control-checkbox {
  width: auto;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.btn {
  padding: 8px 15px;
  border: 1px solid #d1d9e6;
  border-radius: 4px;
  background-color: #fff;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s;
}

.btn:hover {
  background-color: #f0f3f8;
}

.btn.success {
  background-color: #2ecc71;
  border-color: #2ecc71;
  color: white;
}

.btn.success:hover {
  background-color: #27ae60;
}

.btn.danger {
  background-color: #e74c3c;
  border-color: #e74c3c;
  color: white;
}

.btn.danger:hover {
  background-color: #c0392b;
}

/* Stili responsive */
@media (max-width: 768px) {
  .topology-header {
    flex-direction: column;
    gap: 10px;
  }
  
  .topology-controls {
    flex-wrap: wrap;
    justify-content: center;
  }
  
  .modal-content {
    max-width: 90%;
  }
}
</style>