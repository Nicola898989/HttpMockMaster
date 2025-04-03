<template>
  <div class="request-detail">
    <div v-if="!request" class="card">
      <div class="card-body text-center p-5">
        <i data-feather="arrow-left" class="feather-large mb-3"></i>
        <h4>No Request Selected</h4>
        <p class="text-muted">
          Select a request from the list to view its details
        </p>
      </div>
    </div>
    
    <div v-else class="card">
      <div class="card-header d-flex justify-content-between align-items-center">
        <h5 class="mb-0">Request Details</h5>
        
        <div>
          <button class="btn btn-sm btn-outline-primary me-2" @click="createRule">
            <i data-feather="plus-circle"></i> Create Rule
          </button>
        </div>
      </div>
      
      <div class="card-body">
        <!-- Request Info -->
        <div class="d-flex justify-content-between mb-3">
          <div>
            <span class="badge rounded-pill http-method" 
                  :class="'method-' + request.method.toLowerCase()">
              {{ request.method }}
            </span>
            <span class="ms-2 fw-bold">{{ getPathFromUrl(request.url) }}</span>
          </div>
          <div>
            <span v-if="request.isProxied" class="badge bg-info">
              Proxied to {{ request.targetDomain }}
            </span>
            <span class="ms-2 text-muted">{{ formatDateTime(request.timestamp) }}</span>
          </div>
        </div>
        
        <div class="mb-3">
          <label class="form-label">URL</label>
          <input type="text" class="form-control" :value="request.url" readonly>
        </div>
        
        <!-- Tabs for Request/Response -->
        <ul class="nav nav-tabs" id="requestDetailTabs" role="tablist">
          <li class="nav-item" role="presentation">
            <button class="nav-link active" id="request-tab" data-bs-toggle="tab" 
                    data-bs-target="#request-tab-pane" type="button" role="tab" 
                    aria-controls="request-tab-pane" aria-selected="true">
              Request
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button class="nav-link" id="response-tab" data-bs-toggle="tab" 
                    data-bs-target="#response-tab-pane" type="button" role="tab" 
                    aria-controls="response-tab-pane" aria-selected="false">
              Response
            </button>
          </li>
        </ul>
        
        <div class="tab-content p-3 border border-top-0 rounded-bottom" id="requestDetailTabsContent">
          <!-- Request Tab -->
          <div class="tab-pane fade show active" id="request-tab-pane" role="tabpanel" 
               aria-labelledby="request-tab" tabindex="0">
            
            <h6>Headers</h6>
            <div v-if="request.headers" class="headers-table mb-4">
              <table class="table table-sm table-hover">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Value</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(value, name) in parseHeaders(request.headers)" :key="name">
                    <td>{{ name }}</td>
                    <td class="text-break">{{ value }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div v-else class="text-muted">No headers</div>
            
            <h6>Body</h6>
            <div v-if="request.body" class="json-viewer">
              <pre v-if="isJsonString(request.body)">{{ formatJson(request.body) }}</pre>
              <pre v-else>{{ request.body }}</pre>
            </div>
            <div v-else class="text-muted">No body</div>
          </div>
          
          <!-- Response Tab -->
          <div class="tab-pane fade" id="response-tab-pane" role="tabpanel" 
               aria-labelledby="response-tab" tabindex="0">
            
            <div v-if="response">
              <div class="d-flex justify-content-between mb-3">
                <h6>
                  Status: 
                  <span class="badge rounded-pill" :class="statusClass">
                    {{ response.statusCode }}
                  </span>
                </h6>
                <span class="text-muted">{{ formatDateTime(response.timestamp) }}</span>
              </div>
              
              <h6>Headers</h6>
              <div v-if="response.headers" class="headers-table mb-4">
                <table class="table table-sm table-hover">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(value, name) in parseHeaders(response.headers)" :key="name">
                      <td>{{ name }}</td>
                      <td class="text-break">{{ value }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div v-else class="text-muted">No headers</div>
              
              <h6>Body</h6>
              <div v-if="response.body" class="json-viewer">
                <pre v-if="isJsonString(response.body)">{{ formatJson(response.body) }}</pre>
                <pre v-else>{{ response.body }}</pre>
              </div>
              <div v-else class="text-muted">No body</div>
            </div>
            
            <div v-else class="text-center p-3">
              <i data-feather="loader" class="feather-large mb-3"></i>
              <p class="text-muted">No response received yet</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'RequestDetail',
  
  props: {
    request: {
      type: Object,
      required: false,
      default: null
    },
    response: {
      type: Object,
      required: false,
      default: null
    }
  },
  
  emits: ['create-rule'],
  
  computed: {
    statusClass() {
      if (!this.response) return ''
      
      const statusCode = this.response.statusCode
      if (statusCode >= 200 && statusCode < 300) {
        return 'status-2xx'
      } else if (statusCode >= 300 && statusCode < 400) {
        return 'status-3xx'
      } else if (statusCode >= 400 && statusCode < 500) {
        return 'status-4xx'
      } else if (statusCode >= 500) {
        return 'status-5xx'
      }
      return ''
    }
  },
  
  updated() {
    feather.replace()
  },
  
  methods: {
    formatDateTime(timestamp) {
      const date = new Date(timestamp)
      return date.toLocaleString()
    },
    
    getPathFromUrl(url) {
      try {
        const parsedUrl = new URL(url)
        return parsedUrl.pathname + parsedUrl.search
      } catch (e) {
        return url
      }
    },
    
    parseHeaders(headersString) {
      if (!headersString) return {}
      
      const headers = {}
      const lines = headersString.split(/\r?\n/)
      
      for (const line of lines) {
        if (!line.trim()) continue
        
        const colonIndex = line.indexOf(':')
        if (colonIndex > 0) {
          const name = line.substring(0, colonIndex).trim()
          const value = line.substring(colonIndex + 1).trim()
          headers[name] = value
        }
      }
      
      return headers
    },
    
    isJsonString(str) {
      try {
        JSON.parse(str)
        return true
      } catch (e) {
        return false
      }
    },
    
    formatJson(jsonString) {
      try {
        const obj = JSON.parse(jsonString)
        return JSON.stringify(obj, null, 2)
      } catch (e) {
        return jsonString
      }
    },
    
    createRule() {
      this.$emit('create-rule')
    }
  }
}
</script>

<style scoped>
.feather-large {
  width: 48px;
  height: 48px;
  stroke: #adb5bd;
}
</style>
