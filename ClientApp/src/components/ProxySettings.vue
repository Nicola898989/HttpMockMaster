<template>
  <div class="proxy-settings card">
    <div class="card-header">
      <h5 class="mb-0">Proxy Configuration</h5>
    </div>
    
    <div class="card-body">
      <div class="mb-3">
        <div class="d-flex justify-content-between align-items-center mb-2">
          <label for="proxyStatus" class="form-label mb-0">Status</label>
          <span :class="statusClass">{{ statusText }}</span>
        </div>
        <div class="progress">
          <div class="progress-bar" 
               :class="progressBarClass" 
               role="progressbar" 
               style="width: 100%"></div>
        </div>
      </div>
      
      <form @submit.prevent="applyProxySettings">
        <div class="mb-3">
          <label for="targetDomain" class="form-label">Target Domain</label>
          <div class="input-group">
            <select class="form-select flex-grow-0 w-auto" v-model="protocol">
              <option value="http://">http://</option>
              <option value="https://">https://</option>
            </select>
            <input type="text" 
                   class="form-control" 
                   id="targetDomain" 
                   v-model="domain"
                   placeholder="example.com"
                   :disabled="proxyStatus === 'pending'">
          </div>
          <div class="form-text">
            Domain to forward requests to (e.g. example.com)
          </div>
        </div>
        
        <div class="mb-3 form-check">
          <input type="checkbox" 
                 class="form-check-input" 
                 id="preserveHostHeader" 
                 v-model="preserveHostHeader">
          <label class="form-check-label" for="preserveHostHeader">
            Preserve original Host header
          </label>
          <div class="form-text">
            When enabled, the original Host header will be forwarded to the target
          </div>
        </div>
        
        <div class="d-grid gap-2">
          <button v-if="!isProxyEnabled" 
                  type="submit" 
                  class="btn btn-primary"
                  :disabled="!isValidDomain || proxyStatus === 'pending'">
            <i data-feather="play" v-if="proxyStatus !== 'pending'"></i>
            <span v-if="proxyStatus === 'pending'" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
            Start Proxy
          </button>
          <button v-else 
                  type="button" 
                  class="btn btn-danger"
                  @click="disableProxy"
                  :disabled="proxyStatus === 'pending'">
            <i data-feather="stop" v-if="proxyStatus !== 'pending'"></i>
            <span v-if="proxyStatus === 'pending'" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
            Stop Proxy
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'

export default {
  name: 'ProxySettings',
  
  data() {
    return {
      protocol: 'http://',
      domain: '',
      preserveHostHeader: false
    }
  },
  
  computed: {
    ...mapState('proxy', [
      'isProxyEnabled',
      'targetDomain',
      'proxyStatus'
    ]),
    
    isValidDomain() {
      // Basic validation for domain
      return this.domain.trim().length > 0
    },
    
    fullDomain() {
      return this.protocol + this.domain.trim()
    },
    
    statusClass() {
      switch (this.proxyStatus) {
        case 'active':
          return 'text-success'
        case 'error':
          return 'text-danger'
        case 'pending':
          return 'text-warning'
        default: // inactive
          return 'text-secondary'
      }
    },
    
    progressBarClass() {
      switch (this.proxyStatus) {
        case 'active':
          return 'bg-success'
        case 'error':
          return 'bg-danger'
        case 'pending':
          return 'bg-warning progress-bar-striped progress-bar-animated'
        default: // inactive
          return 'bg-secondary'
      }
    },
    
    statusText() {
      switch (this.proxyStatus) {
        case 'active':
          return 'Running'
        case 'error':
          return 'Error'
        case 'pending':
          return 'Connecting...'
        default: // inactive
          return 'Stopped'
      }
    }
  },
  
  created() {
    // If there's an existing target domain, parse it
    if (this.targetDomain) {
      if (this.targetDomain.startsWith('https://')) {
        this.protocol = 'https://'
        this.domain = this.targetDomain.replace('https://', '')
      } else {
        this.protocol = 'http://'
        this.domain = this.targetDomain.replace('http://', '')
      }
    }
  },
  
  updated() {
    feather.replace()
  },
  
  methods: {
    ...mapActions('proxy', [
      'enableProxy',
      'disableProxy'
    ]),
    
    async applyProxySettings() {
      if (!this.isValidDomain) return
      
      // Add headers option if needed later
      const options = {
        preserveHostHeader: this.preserveHostHeader
      }
      
      const success = await this.enableProxy(this.fullDomain)
      if (!success) {
        // Handle error
        console.error('Failed to start proxy')
      }
    }
  }
}
</script>

<style scoped>
.progress {
  height: 6px;
}
</style>
