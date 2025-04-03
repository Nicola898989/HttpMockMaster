import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import 'feather-icons/dist/feather.min.js'

const app = createApp(App)

app.use(router)
app.use(store)

// Global error handler
app.config.errorHandler = (err, vm, info) => {
  console.error('Global error:', err)
  console.error('Info:', info)
  // Could add error reporting service here
}

// Mount the app
app.mount('#app')

// Initialize Feather icons after the app has been mounted
document.addEventListener('DOMContentLoaded', () => {
  feather.replace()
})
