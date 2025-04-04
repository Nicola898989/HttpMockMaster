<template>
  <div class="requests-view">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>HTTP Requests</h2>
      
      <div class="d-flex">
        <button class="btn btn-danger me-2" @click="confirmClearRequests">
          <i data-feather="trash-2"></i> Clear All
        </button>
        <button class="btn btn-primary" @click="refreshRequests">
          <i data-feather="refresh-cw"></i> Refresh
        </button>
      </div>
    </div>
    
    <!-- Filters -->
    <div class="card mb-3">
      <div class="card-header">
        <h5 class="mb-0">Filters</h5>
      </div>
      <div class="card-body">
        <div class="row g-3">
          <div class="col-md-3">
            <label class="form-label">Method</label>
            <select class="form-select" v-model="filters.method">
              <option value="">Any</option>
              <option value="GET">GET</option>
              <option value="POST">POST</option>
              <option value="PUT">PUT</option>
              <option value="DELETE">DELETE</option>
              <option value="PATCH">PATCH</option>
              <option value="OPTIONS">OPTIONS</option>
              <option value="HEAD">HEAD</option>
            </select>
          </div>
          
          <div class="col-md-6">
            <label class="form-label">URL contains</label>
            <input type="text" class="form-control" v-model="filters.url" placeholder="Filter by URL">
          </div>
          
          <div class="col-md-3">
            <label class="form-label">Request Type</label>
            <select class="form-select" v-model="filters.isProxied">
              <option :value="null">All</option>
              <option :value="true">Proxied</option>
              <option :value="false">Direct</option>
            </select>
          </div>
        </div>
        
        <div class="d-flex justify-content-end mt-3">
          <button class="btn btn-secondary me-2" @click="clearFilters">Clear</button>
          <button class="btn btn-primary" @click="applyFilters">Apply Filters</button>
        </div>
      </div>
    </div>
    
    <!-- Request Flow Visualization -->
    <RequestFlowVisualization 
      v-if="currentRequest"
      :request="currentRequest" 
      :matched-rule="matchedRule"
    />
    
    <div class="row">
      <!-- Requests List -->
      <div class="col-md-5">
        <RequestsList 
          :requests="requests" 
          @select-request="selectRequest" 
          @delete-request="confirmDeleteRequest" 
        />
        
        <!-- Pagination -->
        <nav aria-label="Requests pagination" class="mt-3">
          <ul class="pagination justify-content-center">
            <li class="page-item" :class="{ disabled: currentPage === 1 }">
              <a class="page-link" href="#" @click.prevent="changePage(currentPage - 1)">Previous</a>
            </li>
            
            <li v-for="page in paginationPages" :key="page" 
                class="page-item" :class="{ active: page === currentPage }">
              <a class="page-link" href="#" @click.prevent="changePage(page)">{{ page }}</a>
            </li>
            
            <li class="page-item" :class="{ disabled: currentPage === totalPages }">
              <a class="page-link" href="#" @click.prevent="changePage(currentPage + 1)">Next</a>
            </li>
          </ul>
        </nav>
      </div>
      
      <!-- Request Detail -->
      <div class="col-md-7">
        <RequestDetail 
          :request="currentRequest" 
          :response="currentResponse" 
          @create-rule="createRuleFromRequest" 
        />
      </div>
    </div>
    
    <!-- Confirm Delete Modal -->
    <div class="modal fade" id="deleteRequestModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Confirm Delete</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            Are you sure you want to delete this request?
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-danger" @click="deleteRequest">Delete</button>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Confirm Clear All Modal -->
    <div class="modal fade" id="clearRequestsModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Confirm Clear All</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            Are you sure you want to delete all requests? This action cannot be undone.
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-danger" @click="clearAllRequests">Clear All</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'
import { Modal } from 'bootstrap'
import RequestsList from '../components/RequestsList.vue'
import RequestDetail from '../components/RequestDetail.vue'
import RequestFlowVisualization from '../components/RequestFlowVisualization.vue'

export default {
  name: 'RequestsView',
  
  components: {
    RequestsList,
    RequestDetail,
    RequestFlowVisualization
  },
  
  data() {
    return {
      requestToDelete: null,
      deleteModal: null,
      clearModal: null,
      filters: {
        method: '',
        url: '',
        isProxied: null
      }
    }
  },
  
  computed: {
    ...mapState('requests', [
      'requests',
      'currentRequest',
      'currentResponse',
      'pagination'
    ]),
    
    currentPage() {
      return this.pagination.currentPage
    },
    
    totalPages() {
      return this.pagination.totalPages
    },
    
    matchedRule() {
      // In futuro, questa proprietà verrà popolata dal server quando una richiesta
      // viene abbinata a una regola. Per ora, creiamo una regola di esempio se la richiesta
      // è stata intercettata (non è proxied)
      if (this.currentRequest && !this.currentRequest.isProxied) {
        // Simuliamo una regola abbinata solo per richieste con status 200 OK
        if (this.currentResponse && this.currentResponse.statusCode >= 200 && this.currentResponse.statusCode < 300) {
          return {
            id: 'sample-rule',
            name: `Rule for ${this.currentRequest.method} ${this.getPathFromUrl(this.currentRequest.url)}`,
            isActive: true
          }
        }
      }
      return null
    },
    
    // Generate pagination array with ellipsis
    paginationPages() {
      const totalPages = this.totalPages
      const currentPage = this.currentPage
      
      if (totalPages <= 5) {
        return Array.from({ length: totalPages }, (_, i) => i + 1)
      }
      
      if (currentPage <= 3) {
        return [1, 2, 3, 4, '...', totalPages]
      }
      
      if (currentPage >= totalPages - 2) {
        return [1, '...', totalPages - 3, totalPages - 2, totalPages - 1, totalPages]
      }
      
      return [1, '...', currentPage - 1, currentPage, currentPage + 1, '...', totalPages]
    }
  },
  
  mounted() {
    this.fetchRequests()
    this.deleteModal = new Modal(document.getElementById('deleteRequestModal'))
    this.clearModal = new Modal(document.getElementById('clearRequestsModal'))
    
    // Initialize filters from store
    this.filters = { ...this.$store.state.requests.filters }
  },
  
  updated() {
    feather.replace()
  },
  
  methods: {
    ...mapActions('requests', [
      'fetchRequests',
      'fetchRequestDetails',
      'deleteRequest',
      'clearAllRequests',
      'setPage',
      'applyFilters',
      'clearFilters'
    ]),
    
    refreshRequests() {
      this.fetchRequests()
    },
    
    selectRequest(requestId) {
      this.fetchRequestDetails(requestId)
    },
    
    confirmDeleteRequest(requestId) {
      this.requestToDelete = requestId
      this.deleteModal.show()
    },
    
    async confirmClearRequests() {
      this.clearModal.show()
    },
    
    async deleteRequest() {
      if (this.requestToDelete) {
        await this.deleteRequest(this.requestToDelete)
        this.requestToDelete = null
        this.deleteModal.hide()
      }
    },
    
    async clearAllRequests() {
      await this.clearAllRequests()
      this.clearModal.hide()
    },
    
    changePage(page) {
      if (page >= 1 && page <= this.totalPages) {
        this.setPage(page)
      }
    },
    
    applyFilters() {
      this.$store.dispatch('requests/applyFilters', this.filters)
    },
    
    clearFilters() {
      this.filters = {
        method: '',
        url: '',
        isProxied: null
      }
      this.$store.dispatch('requests/clearFilters')
    },
    
    getPathFromUrl(url) {
      if (!url) return ''
      
      try {
        const urlObj = new URL(url)
        return urlObj.pathname
      } catch (e) {
        return url
      }
    },
    
    createRuleFromRequest() {
      if (!this.currentRequest) return
      
      const { url, method } = this.currentRequest
      let pathPattern = this.getPathFromUrl(url)
      
      // Create rule template
      const rule = {
        name: `Rule for ${method} ${pathPattern}`,
        description: `Automatically created from request ${this.currentRequest.id}`,
        method,
        pathPattern,
        queryPattern: '',
        headerPattern: '',
        bodyPattern: '',
        priority: 10,
        isActive: true,
        response: this.currentResponse ? {
          statusCode: this.currentResponse.statusCode,
          headers: this.currentResponse.headers,
          body: this.currentResponse.body
        } : {
          statusCode: 200,
          headers: 'Content-Type: application/json',
          body: '{}'
        }
      }
      
      // Navigate to rules page and set up for editing
      this.$store.commit('rules/SET_CURRENT_RULE', rule)
      this.$store.commit('rules/SET_IS_EDITING', false)
      this.$router.push('/rules')
    }
  }
}
</script>
