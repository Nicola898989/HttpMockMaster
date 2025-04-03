import { createApp } from 'vue';
import App from './App.vue';
import store from './store';
import router from './router';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap/dist/js/bootstrap.bundle.min.js';
import 'bootstrap-icons/font/bootstrap-icons.css';

// Create and configure the Vue app
const app = createApp(App);

// Get backend port from Electron
let backendPort = 8000; // Default port

// Function to set the API base URL
const setApiBaseUrl = (port) => {
    store.commit('setApiBaseUrl', `http://localhost:${port}/api`);
    console.log(`API Base URL set to: http://localhost:${port}/api`);
};

// Try to get the backend port from Electron if we're in Electron context
if (window.api) {
    window.api.getBackendPort().then(port => {
        backendPort = port;
        setApiBaseUrl(port);
    }).catch(err => {
        console.error('Error getting backend port from Electron:', err);
        setApiBaseUrl(backendPort); // Use default port
    });

    // Listen for backend port updates
    window.api.onBackendPort((port) => {
        backendPort = port;
        setApiBaseUrl(port);
    });
} else {
    // We're running in a browser, not Electron
    console.log('Running in browser mode, using default backend port');
    setApiBaseUrl(backendPort);
}

// Mount the app
app.use(store).use(router).mount('#app');
