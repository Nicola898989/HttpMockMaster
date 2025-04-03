const { app, BrowserWindow, ipcMain, dialog, Menu } = require('electron');
const path = require('path');
const url = require('url');
const os = require('os');
const { spawn } = require('child_process');
const portfinder = require('portfinder');
const { autoUpdater } = require('electron-updater');

// Keep a global reference of the window object to avoid garbage collection
let mainWindow;
let backendProcess;
let backendPort = 8000;

// Set environment variables
process.env.ELECTRON_DISABLE_SECURITY_WARNINGS = 'true';

function createWindow() {
  // Create the browser window
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js'),
      webSecurity: false
    },
    icon: path.join(__dirname, '../public/icon.png')
  });

  // Load the app
  if (process.env.NODE_ENV === 'development') {
    // In development, load from dev server
    mainWindow.loadURL('http://localhost:5000');
    mainWindow.webContents.openDevTools();
  } else {
    // In production, load local files
    mainWindow.loadURL(url.format({
      pathname: path.join(__dirname, '../dist/index.html'),
      protocol: 'file:',
      slashes: true
    }));
  }

  // Set up the menu
  const template = [
    {
      label: 'File',
      submenu: [
        { role: 'quit' }
      ]
    },
    {
      label: 'Edit',
      submenu: [
        { role: 'undo' },
        { role: 'redo' },
        { type: 'separator' },
        { role: 'cut' },
        { role: 'copy' },
        { role: 'paste' },
        { role: 'delete' }
      ]
    },
    {
      label: 'View',
      submenu: [
        { role: 'reload' },
        { role: 'forceReload' },
        { role: 'toggleDevTools' },
        { type: 'separator' },
        { role: 'resetZoom' },
        { role: 'zoomIn' },
        { role: 'zoomOut' },
        { type: 'separator' },
        { role: 'togglefullscreen' }
      ]
    },
    {
      label: 'Help',
      submenu: [
        {
          label: 'About',
          click: async () => {
            dialog.showMessageBox(mainWindow, {
              title: 'About HTTP Interceptor',
              message: 'HTTP Interceptor v1.0.0',
              detail: 'A cross-platform HTTP interceptor, proxy and mock server application.\nBuilt with .NET 8 and Vue.js/Electron.'
            });
          }
        }
      ]
    }
  ];

  const menu = Menu.buildFromTemplate(template);
  Menu.setApplicationMenu(menu);

  // Emitted when the window is closed
  mainWindow.on('closed', function () {
    mainWindow = null;
  });

  // Check for updates
  if (process.env.NODE_ENV !== 'development') {
    autoUpdater.checkForUpdatesAndNotify();
  }
}

// Start the .NET backend process
async function startBackend() {
  try {
    const platform = os.platform();
    let executable;

    if (platform === 'win32') {
      executable = path.join(process.resourcesPath, 'backend', 'HttpInterceptor.exe');
    } else if (platform === 'darwin') {
      executable = path.join(process.resourcesPath, 'backend', 'HttpInterceptor');
    } else if (platform === 'linux') {
      executable = path.join(process.resourcesPath, 'backend', 'HttpInterceptor');
    } else {
      console.error('Unsupported platform:', platform);
      return;
    }

    // In development, use local executable
    if (process.env.NODE_ENV === 'development') {
      if (platform === 'win32') {
        executable = path.join(__dirname, '../../../backend/bin/Debug/net8.0/HttpInterceptor.exe');
      } else if (platform === 'darwin' || platform === 'linux') {
        executable = 'dotnet';
      }
    }

    // Find an available port
    backendPort = await portfinder.getPortPromise({
      port: 8000,
      stopPort: 8100
    });

    const args = [];
    if (process.env.NODE_ENV === 'development' && (platform === 'darwin' || platform === 'linux')) {
      args.push(path.join(__dirname, '../../../backend/bin/Debug/net8.0/HttpInterceptor.dll'));
    }
    args.push(`--urls=http://0.0.0.0:${backendPort}`);

    console.log(`Starting backend on port ${backendPort}`);
    
    backendProcess = spawn(executable, args, {
      stdio: 'pipe',
    });

    backendProcess.stdout.on('data', (data) => {
      console.log(`Backend stdout: ${data}`);
    });

    backendProcess.stderr.on('data', (data) => {
      console.error(`Backend stderr: ${data}`);
    });

    backendProcess.on('error', (error) => {
      console.error(`Failed to start backend process: ${error}`);
      dialog.showErrorBox('Backend Error', `Failed to start the backend service: ${error.message}`);
    });

    backendProcess.on('close', (code) => {
      console.log(`Backend process exited with code ${code}`);
      if (code !== 0) {
        dialog.showErrorBox('Backend Error', `The backend service unexpectedly exited with code ${code}`);
      }
      backendProcess = null;
    });

    // Wait for backend to start
    await new Promise(resolve => setTimeout(resolve, 2000));

    // Notify the renderer of the backend port
    if (mainWindow) {
      mainWindow.webContents.send('backend-port', backendPort);
    }
  } catch (error) {
    console.error('Error starting backend:', error);
    dialog.showErrorBox('Backend Error', `Failed to start the backend service: ${error.message}`);
  }
}

// This method will be called when Electron has finished initialization
app.whenReady().then(() => {
  createWindow();
  startBackend();

  app.on('activate', function () {
    // On macOS it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (mainWindow === null) createWindow();
  });
});

// Quit when all windows are closed, except on macOS
app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') app.quit();
});

// Clean up the backend process on exit
app.on('will-quit', () => {
  if (backendProcess) {
    console.log('Terminating backend process');
    if (process.platform === 'win32') {
      spawn('taskkill', ['/pid', backendProcess.pid, '/f', '/t']);
    } else {
      backendProcess.kill();
    }
    backendProcess = null;
  }
});

// Handle IPC messages from renderer
ipcMain.handle('get-backend-port', async () => {
  return backendPort;
});

// Auto updater events
autoUpdater.on('update-available', () => {
  mainWindow.webContents.send('update-available');
});

autoUpdater.on('update-downloaded', () => {
  mainWindow.webContents.send('update-downloaded');
});

ipcMain.on('install-update', () => {
  autoUpdater.quitAndInstall();
});
