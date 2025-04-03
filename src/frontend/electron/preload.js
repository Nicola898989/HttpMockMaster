const { contextBridge, ipcRenderer } = require('electron');

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld(
  'api', {
    getBackendPort: () => ipcRenderer.invoke('get-backend-port'),
    onBackendPort: (callback) => ipcRenderer.on('backend-port', (_, port) => callback(port)),
    onUpdateAvailable: (callback) => {
      ipcRenderer.on('update-available', () => callback());
    },
    onUpdateDownloaded: (callback) => {
      ipcRenderer.on('update-downloaded', () => callback());
    },
    installUpdate: () => {
      ipcRenderer.send('install-update');
    }
  }
);
