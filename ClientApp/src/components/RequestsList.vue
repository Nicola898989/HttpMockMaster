<template>
  <div class="requests-list card">
    <div class="card-header d-flex justify-content-between align-items-center">
      <h5 class="mb-0">Requests ({{ requests.length }})</h5>
      
      <div class="btn-group btn-group-sm" role="group">
        <button type="button" class="btn btn-outline-secondary" title="Group by host">
          <i data-feather="globe"></i>
        </button>
        <button type="button" class="btn btn-outline-secondary" title="Sort by time">
          <i data-feather="clock"></i>
        </button>
      </div>
    </div>
    
    <div class="list-group list-group-flush requests-container">
      <div v-if="requests.length === 0" class="text-center p-4">
        <i data-feather="inbox" class="feather-large mb-3"></i>
        <p class="text-muted">No requests captured</p>
        <p class="small">
          Configure your application to use the proxy at 
          <strong>http://localhost:8888</strong> to see requests here.
        </p>
      </div>
      
      <a v-for="request in requests" 
         :key="request.id" 
         href="#" 
         class="list-group-item list-group-item-action py-3 hover-row"
         @click.prevent="selectRequest(request.id)">
        <div class="d-flex w-100 justify-content-between">
          <div class="d-flex align-items-center">
            <span class="badge rounded-pill http-method" 
                  :class="'method-' + request.method.toLowerCase()">
              {{ request.method }}
            </span>
            <h6 class="ms-2 mb-0 text-truncate-container" :title="getPathFromUrl(request.url)">
              {{ getPathFromUrl(request.url) }}
            </h6>
          </div>
          <small class="text-muted">
            {{ formatTime(request.timestamp) }}
          </small>
        </div>
        
        <div class="d-flex mt-1">
          <small class="text-truncate-container" :title="request.url">
            {{ getHostFromUrl(request.url) }}
          </small>
          
          <div class="ms-auto">
            <span v-if="request.isProxied" class="badge bg-info">Proxied</span>
            <button class="btn btn-sm btn-outline-danger ms-2" 
                    @click.stop="deleteRequest(request.id)"
                    title="Delete request">
              <i data-feather="trash-2" class="feather-sm"></i>
            </button>
          </div>
        </div>
      </a>
    </div>
  </div>
</template>

<script>
export default {
  name: 'RequestsList',
  
  props: {
    requests: {
      type: Array,
      required: true
    }
  },
  
  emits: ['select-request', 'delete-request'],
  
  updated() {
    feather.replace()
  },
  
  methods: {
    selectRequest(id) {
      this.$emit('select-request', id)
    },
    
    deleteRequest(id) {
      this.$emit('delete-request', id)
    },
    
    formatTime(timestamp) {
      const date = new Date(timestamp)
      return date.toLocaleTimeString()
    },
    
    getPathFromUrl(url) {
      try {
        const parsedUrl = new URL(url)
        return parsedUrl.pathname + parsedUrl.search
      } catch (e) {
        return url
      }
    },
    
    getHostFromUrl(url) {
      try {
        const parsedUrl = new URL(url)
        return parsedUrl.host
      } catch (e) {
        return 'unknown'
      }
    }
  }
}
</script>

<style scoped>
.requests-container {
  max-height: 550px;
  overflow-y: auto;
}

.feather-sm {
  width: 14px;
  height: 14px;
}

.feather-large {
  width: 48px;
  height: 48px;
  stroke: #adb5bd;
}
</style>
