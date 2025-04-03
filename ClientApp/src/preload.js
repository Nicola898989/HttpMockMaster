const { contextBridge, ipcRenderer } = require('electron')

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld(
  'api', {
    // System information
    getSystemInfo: () => {
      return {
        platform: process.platform, // win32, darwin, linux
        arch: process.arch,
        electronVersion: process.versions.electron,
        chromeVersion: process.versions.chrome,
        nodeVersion: process.versions.node
      }
    },
    
    // App/window controls
    minimize: () => ipcRenderer.send('minimize'),
    maximize: () => ipcRenderer.send('maximize'),
    close: () => ipcRenderer.send('close'),
    
    // File system access
    exportData: (fileName, data) => ipcRenderer.invoke('export-data', fileName, data),
    importData: (fileType) => ipcRenderer.invoke('import-data', fileType),
    
    // App settings
    getAppSettings: () => ipcRenderer.invoke('get-app-settings'),
    saveAppSettings: (settings) => ipcRenderer.invoke('save-app-settings', settings),
    
    // Listen for events from main process
    on: (channel, callback) => {
      // Whitelist channels to listen to
      const validChannels = ['backend-status', 'app-update', 'notify']
      if (validChannels.includes(channel)) {
        // Strip event as it includes `sender` and other internal electron properties
        ipcRenderer.on(channel, (_, ...args) => callback(...args))
      }
    },
    
    // Remove event listener
    removeAllListeners: (channel) => {
      const validChannels = ['backend-status', 'app-update', 'notify']
      if (validChannels.includes(channel)) {
        ipcRenderer.removeAllListeners(channel)
      }
    }
  }
)
