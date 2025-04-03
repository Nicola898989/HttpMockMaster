<template>
  <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
    <div class="container-fluid">
      <router-link class="navbar-brand" to="/">
        <i class="bi bi-braces"></i> HTTP Interceptor
      </router-link>
      
      <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav" 
              aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
        <span class="navbar-toggler-icon"></span>
      </button>
      
      <div class="collapse navbar-collapse" id="navbarNav">
        <ul class="navbar-nav">
          <li class="nav-item">
            <router-link class="nav-link" to="/" exact>
              <i class="bi bi-house"></i> Home
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/requests">
              <i class="bi bi-list-ul"></i> Requests
              <span class="badge bg-primary ms-1" v-if="requestsCount > 0">{{ requestsCount }}</span>
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/rules">
              <i class="bi bi-gear"></i> Rules
              <span class="badge bg-success ms-1" v-if="rulesCount > 0">{{ rulesCount }}</span>
            </router-link>
          </li>
          <li class="nav-item">
            <router-link class="nav-link" to="/proxy">
              <i class="bi bi-arrow-left-right"></i> Proxy
            </router-link>
          </li>
        </ul>
        
        <ul class="navbar-nav ms-auto">
          <li class="nav-item">
            <span class="nav-link">
              <i class="bi bi-broadcast" :class="{ 'text-success': serverStatus, 'text-danger': !serverStatus }"></i>
              Interceptor on port 8888
            </span>
          </li>
        </ul>
      </div>
    </div>
  </nav>
</template>

<script>
import { mapGetters } from 'vuex';

export default {
  name: 'NavigationBar',
  data() {
    return {
      serverStatus: true, // Assume server is running
      connectionCheckInterval: null
    };
  },
  computed: {
    ...mapGetters(['requestsCount', 'rulesCount'])
  },
  mounted() {
    // Check connection to backend periodically
    this.connectionCheckInterval = setInterval(this.checkServerStatus, 5000);
    this.checkServerStatus();
  },
  beforeUnmount() {
    if (this.connectionCheckInterval) {
      clearInterval(this.connectionCheckInterval);
    }
  },
  methods: {
    async checkServerStatus() {
      try {
        // Make a HEAD request to backend API to check if it's running
        await fetch(this.$store.state.apiBaseUrl, { method: 'HEAD' });
        this.serverStatus = true;
      } catch (error) {
        this.serverStatus = false;
      }
    }
  }
};
</script>

<style scoped>
.navbar {
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.router-link-active {
  font-weight: bold;
}
</style>
