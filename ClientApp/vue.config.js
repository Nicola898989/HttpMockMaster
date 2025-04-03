module.exports = {
  pluginOptions: {
    electronBuilder: {
      nodeIntegration: false,
      preload: 'src/preload.js',
      builderOptions: {
        appId: 'com.mockammt.app',
        productName: 'MockAMMT',
        copyright: 'Copyright © 2025',
        publish: null,
        mac: {
          category: 'public.app-category.developer-tools',
          target: ['dmg', 'zip'],
          darkModeSupport: true
        },
        win: {
          target: ['nsis']
        },
        linux: {
          target: ['AppImage', 'deb'],
          category: 'Development'
        },
        nsis: {
          oneClick: false,
          allowToChangeInstallationDirectory: true
        },
        extraResources: [
          {
            from: 'backend',
            to: 'backend',
            filter: ['**/*']
          }
        ]
      },
      // Handle custom backend packaging
      chainWebpackMainProcess: config => {
        // Only include minimal required dependencies
        config.externals([
          'electron-devtools-installer',
          'electron-debug'
        ])
      }
    }
  },
  // Development server configuration
  devServer: {
    port: 5000,
    host: '0.0.0.0',
    proxy: {
      '^/api': {
        target: 'http://localhost:8000',
        changeOrigin: true,
        ws: true
      }
    }
  }
}
