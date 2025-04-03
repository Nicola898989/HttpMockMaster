<template>
  <div class="settings-view">
    <h2 class="mb-4">Settings</h2>
    
    <div class="row">
      <div class="col-md-8">
        <div class="card">
          <div class="card-header">
            <h5 class="mb-0">Application Settings</h5>
          </div>
          <div class="card-body">
            <form @submit.prevent="saveSettings">
              <div class="mb-3">
                <label for="interceptorPort" class="form-label">Interceptor Port</label>
                <input type="number" 
                       class="form-control" 
                       id="interceptorPort" 
                       v-model="settings.interceptorPort"
                       min="1024"
                       max="65535">
                <div class="form-text">
                  HTTP interceptor will listen on this port. Default is 8888.
                  <strong>Requires application restart.</strong>
                </div>
              </div>
              
              <div class="mb-3">
                <label for="apiPort" class="form-label">API Port</label>
                <input type="number" 
                       class="form-control" 
                       id="apiPort" 
                       v-model="settings.apiPort"
                       min="1024"
                       max="65535">
                <div class="form-text">
                  Backend API will listen on this port. Default is 8000.
                  <strong>Requires application restart.</strong>
                </div>
              </div>
              
              <div class="mb-3 form-check">
                <input type="checkbox" 
                       class="form-check-input" 
                       id="autoStart" 
                       v-model="settings.autoStart">
                <label class="form-check-label" for="autoStart">
                  Start interceptor automatically
                </label>
              </div>
              
              <div class="mb-3 form-check">
                <input type="checkbox" 
                       class="form-check-input" 
                       id="darkMode" 
                       v-model="settings.darkMode">
                <label class="form-check-label" for="darkMode">
                  Dark Mode
                </label>
              </div>
              
              <div class="mb-3">
                <label for="maxRequests" class="form-label">Maximum Requests to Store</label>
                <input type="number" 
                       class="form-control" 
                       id="maxRequests" 
                       v-model="settings.maxRequests"
                       min="100"
                       max="10000">
                <div class="form-text">
                  Older requests will be deleted automatically. Set to 0 to disable limiting.
                </div>
              </div>
              
              <button type="submit" class="btn btn-primary">Save Settings</button>
            </form>
          </div>
        </div>
        
        <div class="card mt-4">
          <div class="card-header">
            <h5 class="mb-0">Data Management</h5>
          </div>
          <div class="card-body">
            <div class="row">
              <div class="col-md-6">
                <div class="mb-3">
                  <label class="form-label">Export Rules</label>
                  <button class="btn btn-outline-primary w-100" @click="exportRules">
                    <i data-feather="download"></i> Export Rules
                  </button>
                  <div class="form-text">Export all rules as JSON file</div>
                </div>
              </div>
              
              <div class="col-md-6">
                <div class="mb-3">
                  <label class="form-label">Import Rules</label>
                  <input type="file" 
                         class="form-control" 
                         id="importRules"
                         accept=".json"
                         @change="handleFileUpload">
                  <div class="form-text">Import rules from a JSON file</div>
                </div>
              </div>
            </div>
            
            <div class="alert alert-danger mt-3">
              <h6>Danger Zone</h6>
              <div class="d-flex justify-content-between align-items-center">
                <div>
                  <p class="mb-0">
                    Clear all data including requests, responses and rules
                  </p>
                </div>
                <button class="btn btn-outline-danger" @click="confirmResetData">
                  Reset All Data
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <div class="col-md-4">
        <div class="card">
          <div class="card-header">
            <h5 class="mb-0">About</h5>
          </div>
          <div class="card-body">
            <h6>HTTP Interceptor & Proxy</h6>
            <p>Version 1.0.0</p>
            <p>
              A cross-platform tool for intercepting, mocking, and proxying HTTP requests.
            </p>
            <p>
              Built with .NET 8, Vue.js, and Electron.
            </p>
            
            <h6 class="mt-4">System Information</h6>
            <table class="table table-sm">
              <tbody>
                <tr>
                  <th>OS</th>
                  <td>{{ systemInfo.os }}</td>
                </tr>
                <tr>
                  <th>.NET Version</th>
                  <td>{{ systemInfo.dotnetVersion }}</td>
                </tr>
                <tr>
                  <th>Electron</th>
                  <td>{{ systemInfo.electronVersion }}</td>
                </tr>
                <tr>
                  <th>Database</th>
                  <td>{{ systemInfo.dbPath }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Confirm Reset Modal -->
    <div class="modal fade" id="resetDataModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Confirm Reset</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <p class="text-danger">
              <strong>Warning:</strong> This action cannot be undone!
            </p>
            <p>
              This will delete all stored requests, responses, and rules. The application will be reset 
              to its initial state.
            </p>
            <p>
              Are you sure you want to proceed?
            </p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-danger" @click="resetAllData">
              Yes, Reset Everything
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { Modal } from 'bootstrap'
import { mapState, mapActions } from 'vuex'
import api from '../services/api'

export default {
  name: 'SettingsView',
  
  data() {
    return {
      settings: {
        interceptorPort: 8888,
        apiPort: 8000,
        autoStart: true,
        darkMode: false,
        maxRequests: 1000
      },
      systemInfo: {
        os: 'Unknown',
        dotnetVersion: '.NET 8.0',
        electronVersion: 'Unknown',
        dbPath: 'Local Storage'
      },
      resetModal: null
    }
  },
  
  mounted() {
    this.resetModal = new Modal(document.getElementById('resetDataModal'))
    
    // In a real app, we would load settings from persistent storage
    // Detect OS
    const userAgent = navigator.userAgent
    if (userAgent.includes('Windows')) {
      this.systemInfo.os = 'Windows'
    } else if (userAgent.includes('Mac')) {
      this.systemInfo.os = 'macOS'
    } else if (userAgent.includes('Linux')) {
      this.systemInfo.os = 'Linux'
    }
    
    // Electron version would be retrieved from the window.versions object
    // that would be exposed by the preload script
    this.systemInfo.electronVersion = 'Electron 25.x'
  },
  
  updated() {
    feather.replace()
  },
  
  methods: {
    ...mapActions('rules', ['fetchRules']),
    ...mapActions('requests', ['clearAllRequests']),
    
    saveSettings() {
      // In a real app, save settings to persistent storage
      alert('Settings saved!')
    },
    
    exportRules() {
      // Export rules to a JSON file
      const rules = this.$store.state.rules.rules
      const dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(rules, null, 2))
      const downloadAnchorNode = document.createElement('a')
      downloadAnchorNode.setAttribute("href", dataStr)
      downloadAnchorNode.setAttribute("download", "http-interceptor-rules.json")
      document.body.appendChild(downloadAnchorNode)
      downloadAnchorNode.click()
      downloadAnchorNode.remove()
    },
    
    handleFileUpload(event) {
      const file = event.target.files[0]
      if (!file) return
      
      const reader = new FileReader()
      reader.onload = async (e) => {
        try {
          const rules = JSON.parse(e.target.result)
          
          // Validate rules structure
          if (!Array.isArray(rules)) {
            alert('Invalid rules file format!')
            return
          }
          
          // Import each rule
          let imported = 0
          for (const rule of rules) {
            try {
              await api.post('/rules', rule)
              imported++
            } catch (error) {
              console.error('Error importing rule:', error)
            }
          }
          
          alert(`Successfully imported ${imported} rules!`)
          await this.fetchRules()
          
          // Reset file input
          event.target.value = ''
        } catch (error) {
          alert('Error parsing rules file!')
          console.error(error)
        }
      }
      reader.readAsText(file)
    },
    
    confirmResetData() {
      this.resetModal.show()
    },
    
    async resetAllData() {
      try {
        // Clear all requests
        await this.clearAllRequests()
        
        // Delete all rules
        const rules = this.$store.state.rules.rules
        for (const rule of rules) {
          await api.delete(`/rules/${rule.id}`)
        }
        
        // Refresh data
        await this.fetchRules()
        
        this.resetModal.hide()
        alert('All data has been reset!')
      } catch (error) {
        console.error('Error resetting data:', error)
        alert('Error resetting data: ' + error.message)
      }
    }
  }
}
</script>
