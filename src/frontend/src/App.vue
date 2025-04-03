<template>
  <div class="app-container">
    <NavigationBar />
    <div class="content-container">
      <router-view />
    </div>
    
    <!-- Update notification -->
    <div v-if="updateAvailable" class="update-notification">
      <div class="alert alert-info" role="alert">
        <strong>Update Available!</strong> 
        <span v-if="updateDownloaded">
          Update has been downloaded. <button @click="installUpdate" class="btn btn-primary btn-sm">Install Now</button>
        </span>
        <span v-else>
          Downloading the latest version...
        </span>
      </div>
    </div>
  </div>
</template>

<script>
import NavigationBar from './components/NavigationBar.vue';

export default {
  name: 'App',
  components: {
    NavigationBar
  },
  data() {
    return {
      updateAvailable: false,
      updateDownloaded: false
    };
  },
  mounted() {
    // Listen for update events if we're in Electron
    if (window.api) {
      window.api.onUpdateAvailable(() => {
        this.updateAvailable = true;
      });
      
      window.api.onUpdateDownloaded(() => {
        this.updateDownloaded = true;
      });
    }
  },
  methods: {
    installUpdate() {
      if (window.api) {
        window.api.installUpdate();
      }
    }
  }
};
</script>

<style>
.app-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.content-container {
  flex: 1;
  overflow: auto;
  padding: 20px;
}

.update-notification {
  position: fixed;
  bottom: 20px;
  right: 20px;
  z-index: 1000;
  max-width: 400px;
}
</style>
