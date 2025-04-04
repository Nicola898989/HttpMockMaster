<template>
  <nav class="navbar navbar-expand-lg navbar-light bg-white border-bottom">
    <div class="container-fluid">
      <a class="navbar-brand d-flex align-items-center" href="#">
        <i data-feather="activity" class="me-2"></i>
        HTTP Interceptor
      </a>
      
      <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav" 
              aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
        <span class="navbar-toggler-icon"></span>
      </button>
      
      <div class="collapse navbar-collapse" id="navbarNav">
        <ul class="navbar-nav">
          <li class="nav-item">
            <router-link class="nav-link" to="/requests" active-class="active">
              <i data-feather="inbox" class="feather-icon"></i>
              Requests
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/rules" active-class="active">
              <i data-feather="filter" class="feather-icon"></i>
              Rules
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/proxy" active-class="active">
              <i data-feather="repeat" class="feather-icon"></i>
              Proxy
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/topology" active-class="active">
              <i data-feather="share-2" class="feather-icon"></i>
              Topology
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/performance" active-class="active">
              <i data-feather="bar-chart-2" class="feather-icon"></i>
              Performance
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/comparison" active-class="active">
              <i data-feather="git-pull-request" class="feather-icon"></i>
              Comparison
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/settings" active-class="active">
              <i data-feather="settings" class="feather-icon"></i>
              Settings
            </router-link>
          </li>
        </ul>
        
        <div class="ms-auto d-flex align-items-center">
          <div class="px-3">
            <span :class="proxyStatusClass" class="d-flex align-items-center">
              <i data-feather="circle" class="feather-icon-sm me-1"></i>
              <span v-if="isProxyEnabled">Proxy active</span>
              <span v-else>Proxy inactive</span>
            </span>
          </div>
          <span class="badge bg-primary">Port: 8888</span>
        </div>
      </div>
    </div>
  </nav>
</template>

<script>
import { mapState } from 'vuex'

export default {
  name: 'NavigationBar',
  
  computed: {
    ...mapState('proxy', [
      'isProxyEnabled',
      'proxyStatus'
    ]),
    
    proxyStatusClass() {
      if (this.isProxyEnabled) {
        return 'text-success'
      } else {
        return 'text-secondary'
      }
    }
  },
  
  mounted() {
    feather.replace()
  },
  
  updated() {
    feather.replace()
  }
}
</script>

<style scoped>
.feather-icon {
  width: 16px;
  height: 16px;
  margin-right: 5px;
  vertical-align: text-bottom;
}

.feather-icon-sm {
  width: 12px;
  height: 12px;
}

.navbar-brand i {
  width: 24px;
  height: 24px;
}
</style>
