<template>
  <div id="app" class="d-flex flex-column vh-100">
    <NavigationBar />
    <div class="flex-grow-1 container-fluid">
      <router-view />
    </div>
    <footer class="bg-light py-2 text-center">
      <p class="text-muted mb-0">HTTP Interceptor & Proxy v1.0</p>
    </footer>
  </div>
</template>

<script>
import NavigationBar from './components/NavigationBar.vue'

export default {
  name: 'App',
  components: {
    NavigationBar
  },
  created() {
    // Initialize the app on startup
    this.$store.dispatch('requests/fetchRequests')
    this.$store.dispatch('rules/fetchRules')
  }
}
</script>

<style>
body {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  overflow-x: hidden;
}

.text-truncate-container {
  max-width: 250px;
}

.json-viewer {
  max-height: 300px;
  overflow-y: auto;
  background-color: #f8f9fa;
  padding: 10px;
  border-radius: 4px;
}

.status-badge {
  width: 80px;
}

.http-method {
  width: 70px;
  display: inline-block;
  text-align: center;
}

.method-get { background-color: #8bc34a; color: white; }
.method-post { background-color: #ff9800; color: white; }
.method-put { background-color: #03a9f4; color: white; }
.method-delete { background-color: #f44336; color: white; }
.method-patch { background-color: #9c27b0; color: white; }
.method-options { background-color: #607d8b; color: white; }
.method-head { background-color: #795548; color: white; }

.status-2xx { background-color: #4caf50; color: white; }
.status-3xx { background-color: #ff9800; color: white; }
.status-4xx { background-color: #f44336; color: white; }
.status-5xx { background-color: #9c27b0; color: white; }

.headers-table th {
  width: 30%;
}

.hover-row:hover {
  background-color: rgba(0, 0, 0, 0.05);
  cursor: pointer;
}
</style>
