<template>
  <div class="requests-list">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h3><i class="bi bi-list-ul"></i> HTTP Requests</h3>
      <div>
        <button @click="refresh" class="btn btn-outline-primary me-2" :disabled="loading">
          <i class="bi bi-arrow-repeat"></i> Refresh
        </button>
        <button @click="confirmClearAll" class="btn btn-outline-danger" :disabled="loading || !requests.length">
          <i class="bi bi-trash"></i> Clear All
        </button>
      </div>
    </div>

    <div class="alert alert-info" v-if="!requests.length && !loading">
      <i class="bi bi-info-circle"></i> No HTTP requests captured yet. 
      The interceptor is listening on port 8888.
    </div>
    
    <div v-if="loading" class="text-center p-3">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>
    
    <div class="list-group" v-else>
      <a v-for="request in requests" :key="request.id" 
         href="#" 
         class="list-group-item list-group-item-action"
         :class="{ 'active': selectedRequest && selectedRequest.id === request.id }"
         @click.prevent="selectRequest(request.id)">
        <div class="d-flex justify-content-between align-items-center">
          <div>
            <span class="method-badge" :class="getMethodClass(request.method)">{{ request.method }}</span>
            <span class="path">{{ request.path }}</span>
          </div>
          <small class="text-muted">{{ formatTime(request.timestamp) }}</small>
        </div>
      </a>
    </div>
    
    <!-- Confirmation Modal -->
    <div class="modal fade" id="clearAllModal" tabindex="-1" aria-labelledby="clearAllModalLabel" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="clearAllModalLabel">Clear All Requests</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            Are you sure you want to delete all captured HTTP requests?
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-danger" @click="clearAll">Clear All</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { mapState } from 'vuex';
import { Modal } from 'bootstrap';

export default {
  name: 'RequestsList',
  
  computed: {
    ...mapState({
      requests: state => state.requests,
      selectedRequest: state => state.selectedRequest,
      loading: state => state.loading.requests
    })
  },
  
  data() {
    return {
      clearModal: null
    };
  },
  
  mounted() {
    this.fetchRequests();
    this.clearModal = new Modal(document.getElementById('clearAllModal'));
  },
  
  methods: {
    async fetchRequests() {
      await this.$store.dispatch('fetchRequests');
    },
    
    async selectRequest(id) {
      await this.$store.dispatch('fetchRequest', id);
    },
    
    refresh() {
      this.fetchRequests();
    },
    
    confirmClearAll() {
      this.clearModal.show();
    },
    
    async clearAll() {
      try {
        await this.$store.dispatch('clearAllRequests');
        this.clearModal.hide();
      } catch (error) {
        console.error('Error clearing requests:', error);
      }
    },
    
    getMethodClass(method) {
      switch (method.toUpperCase()) {
        case 'GET':
          return 'method-get';
        case 'POST':
          return 'method-post';
        case 'PUT':
          return 'method-put';
        case 'DELETE':
          return 'method-delete';
        case 'PATCH':
          return 'method-patch';
        default:
          return 'method-other';
      }
    },
    
    formatTime(timestamp) {
      if (!timestamp) return '';
      const date = new Date(timestamp);
      return date.toLocaleTimeString();
    }
  }
};
</script>

<style scoped>
.requests-list {
  height: 100%;
  overflow-y: auto;
}

.method-badge {
  padding: 3px 6px;
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: bold;
  margin-right: 8px;
  width: 60px;
  display: inline-block;
  text-align: center;
}

.method-get {
  background-color: #61affe;
  color: white;
}

.method-post {
  background-color: #49cc90;
  color: white;
}

.method-put {
  background-color: #fca130;
  color: white;
}

.method-delete {
  background-color: #f93e3e;
  color: white;
}

.method-patch {
  background-color: #50e3c2;
  color: white;
}

.method-other {
  background-color: #747474;
  color: white;
}

.path {
  font-family: monospace;
  font-size: 0.9rem;
}

.list-group-item.active .text-muted {
  color: rgba(255, 255, 255, 0.75) !important;
}
</style>
