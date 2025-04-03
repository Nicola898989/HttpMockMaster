'use strict'

import { app, protocol, BrowserWindow, ipcMain } from 'electron'
import { createProtocol } from 'vue-cli-plugin-electron-builder/lib'
import installExtension, { VUEJS3_DEVTOOLS } from 'electron-devtools-installer'
import path from 'path'
import { spawn } from 'child_process'

const isDevelopment = process.env.NODE_ENV !== 'production'

// Keep a global reference of the window object
let win
// Keep a reference to the .NET process
let dotnetProcess

// Scheme must be registered before the app is ready
protocol.registerSchemesAsPrivileged([
  { scheme: 'app', privileges: { secure: true, standard: true } }
])

async function createWindow() {
  // Create the browser window
  win = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      // Required for Electron 12+
      contextIsolation: true,
      // Use preload script to safely expose Node.js functionality
      preload: path.join(__dirname, 'preload.js'),
      nodeIntegration: process.env.ELECTRON_NODE_INTEGRATION
    }
  })

  if (process.env.WEBPACK_DEV_SERVER_URL) {
    // Load the url of the dev server if in development mode
    await win.loadURL(process.env.WEBPACK_DEV_SERVER_URL)
    if (!process.env.IS_TEST) win.webContents.openDevTools()
  } else {
    createProtocol('app')
    // Load the index.html when not in development
    win.loadURL('app://./index.html')
  }
}

// Start the .NET backend process
function startBackendProcess() {
  let backendPath
  
  if (isDevelopment) {
    // In development, use the BackendService project directly
    backendPath = path.join(__dirname, '..', '..', 'BackendService', 'bin', 'Debug', 'net8.0', 'BackendService.dll')
  } else {
    // In production, use the published backend included with the app
    backendPath = path.join(process.resourcesPath, 'backend', 'BackendService.dll')
  }

  console.log(`Starting backend from: ${backendPath}`)
  
  dotnetProcess = spawn('dotnet', [backendPath], {
    stdio: 'pipe'
  })

  dotnetProcess.stdout.on('data', (data) => {
    console.log(`Backend stdout: ${data}`)
  })

  dotnetProcess.stderr.on('data', (data) => {
    console.error(`Backend stderr: ${data}`)
  })

  dotnetProcess.on('close', (code) => {
    console.log(`Backend process exited with code ${code}`)
    if (code !== 0 && !app.isQuitting) {
      // Try to restart the process if it crashes and the app is still running
      setTimeout(startBackendProcess, 1000)
    }
  })
}

// Quit when all windows are closed.
app.on('window-all-closed', () => {
  // On macOS it is common for applications and their menu bar
  // to stay active until the user quits explicitly with Cmd + Q
  if (process.platform !== 'darwin') {
    app.quit()
  }
})

app.on('activate', () => {
  // On macOS it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) createWindow()
})

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', async () => {
  if (isDevelopment && !process.env.IS_TEST) {
    // Install Vue Devtools
    try {
      await installExtension(VUEJS3_DEVTOOLS)
    } catch (e) {
      console.error('Vue Devtools failed to install:', e.toString())
    }
  }
  
  // Start the .NET backend process
  startBackendProcess()
  
  // Create the application window
  createWindow()
})

// Flag to track if we're quitting intentionally
app.isQuitting = false

// Handle app quit - make sure to shut down the .NET process
app.on('before-quit', () => {
  app.isQuitting = true
  if (dotnetProcess && !dotnetProcess.killed) {
    dotnetProcess.kill()
  }
})

// Exit cleanly on request from parent process in development mode.
if (isDevelopment) {
  if (process.platform === 'win32') {
    process.on('message', (data) => {
      if (data === 'graceful-exit') {
        app.quit()
      }
    })
  } else {
    process.on('SIGTERM', () => {
      app.quit()
    })
  }
}
