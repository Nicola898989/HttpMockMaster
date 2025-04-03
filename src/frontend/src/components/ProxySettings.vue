<template>
  <div class="proxy-settings">
    <div class="card">
      <div class="card-header">
        <h5><i class="bi bi-arrow-left-right"></i> Proxy Configuration</h5>
      </div>
      <div class="card-body">
        <form @submit.prevent="saveConfig">
          <div class="mb-3">
            <label for="targetDomain" class="form-label">Target Domain</label>
            <input 
              type="text" 
              class="form-control" 
              id="targetDomain" 
              v-model="config.targetDomain" 
              placeholder="https://api.example.com"
              :class="{ 'is-invalid': errors.targetDomain }"
            >
            <div class="invalid-feedback" v-if="errors.targetDomain">
              {{ errors.targetDomain }}
            </div>
            <div class="form-text">
              The domain to which requests will be forwarded. Include protocol (http:// or https://).
            </div>
          </div>
          
          <div class="mb-3 form-check">
            <input type="checkbox" class="form-check-input" id="mockMode" v-model="config.mockMode">
            <label class="form-check-label" for="mockMode">Mock Mode</label>
            <div class="form-text">
              In mock mode, the proxy will not forward requests but will respond with matching rules.
            </div>
          </div>
          
          <div class="d-flex justify-content-end mt-4">
            <button type="submit" class="btn btn-primary" :disabled="isSaving">
              <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
              Save Configuration
            </button>
          </div>
        </form>
      </div>
    </div>
    
    <div class="mt-4">
      <div class="card">
        <div class="card-header">
          <h5><i class="bi bi-info-circle"></i> How to use the proxy</h5>
        </div>
        <div class="card-body">
          <p>
            The HTTP interceptor listens on port <code>8888</code>. To use it as a proxy for your application:
          </p>
          
          <h6>Direct usage</h6>
          <p>
            Make HTTP requests to <code>http://localhost:8888/your/path</code> instead of the actual API endpoint.
          </p>
          
          <h6>Browser configuration</h6>
          <p>
            Configure your browser to use <code>localhost:8888</code> as an HTTP proxy.
          </p>
          
          <h6>Application configuration</h6>
          <p>
            Configure your application to use <code>localhost:8888</code> as a proxy server.
          </p>
          
          <div class="alert alert-info">
            <i class="bi bi-lightbulb"></i> <strong>Tip:</strong> 
            When using the proxy, all requests will be recorded and can be viewed in the Requests tab.
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { mapState } from 'vuex';

export default {
  name: 'ProxySettings',
  
  computed: {
    ...mapState({
      storeConfig: state => state.proxyConfig,
      loading: state => state.loading.proxy
    })
  },
  
  data() {
    return {
      config: {
        targetDomain: '',
        mockMode: false
      },
      errors: {
        targetDomain: ''
      },
      isSaving: false
    };
  },
  
  watch: {
    storeConfig: {
      handler(newConfig) {
        this.config = { ...newConfig };
      },
      immediate: true
    },
    'config.targetDomain': {
      handler(value) {
        this.validateTargetDomain(value);
      }
    }
  },
  
  methods: {
    validateTargetDomain(domain) {
      this.errors.targetDomain = '';
      
      if (!domain && !this.config.mockMode) {
        this.errors.targetDomain = 'Target domain is required unless mock mode is enabled';
        return false;
      }
      
      if (domain && !domain.startsWith('http://') && !domain.startsWith('https://')) {
        this.errors.targetDomain = 'Target domain must start with http:// or https://';
        return false;
      }
      
      return true;
    },
    
    async saveConfig() {
      // Only validate if not in mock mode or if a domain is provided
      if (!this.validateTargetDomain(this.config.targetDomain) && !this.config.mockMode) {
        return;
      }
      
      this.isSaving = true;
      
      try {
        await this.$store.dispatch('updateProxyConfig', this.config);
        this.$store.commit('setError', null);
      } catch (error) {
        console.error('Error saving proxy config:', error);
      } finally {
        this.isSaving = false;
      }
    }
  }
};
</script>

<style scoped>
.proxy-settings {
  max-width: 800px;
  margin: 0 auto;
}
</style>
