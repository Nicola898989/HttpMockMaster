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
        // Configurazioni Windows
        win: {
          target: ['nsis'],
          icon: 'build/icon.ico',
          artifactName: '${productName}-Setup-${version}.${ext}'
        },
        // Configurazioni Mac
        mac: {
          category: 'public.app-category.developer-tools',
          target: ['dmg', 'zip'],
          darkModeSupport: true,
          icon: 'build/icon.icns',
          hardenedRuntime: true,
          gatekeeperAssess: false,
          entitlements: 'build/entitlements.mac.plist',
          entitlementsInherit: 'build/entitlements.mac.plist'
        },
        // Configurazioni Linux
        linux: {
          target: ['AppImage', 'deb'],
          category: 'Development',
          icon: 'build/icons',
          desktop: {
            StartupNotify: 'true',
            StartupWMClass: 'MockAMMT'
          }
        },
        // Configurazioni NSIS (Windows installer)
        nsis: {
          oneClick: false,
          allowToChangeInstallationDirectory: true,
          createDesktopShortcut: true,
          createStartMenuShortcut: true,
          shortcutName: 'MockAMMT',
          deleteAppDataOnUninstall: true,
          displayLanguageSelector: true,
          menuCategory: 'MockAMMT',
          installerIcon: 'build/icon.ico',
          uninstallerIcon: 'build/icon.ico',
          installerHeaderIcon: 'build/icon.ico'
        },
        // File e cartelle aggiuntive da includere
        extraResources: [
          {
            from: 'backend-dist',
            to: 'backend',
            filter: ['**/*']
          }
        ],
        // Configurazione per il file
        files: [
          "**/*",
          "!**/node_modules/*/{CHANGELOG.md,README.md,README,readme.md,readme}",
          "!**/node_modules/*/{test,__tests__,tests,powered-test,example,examples}",
          "!**/node_modules/*.d.ts",
          "!**/node_modules/.bin",
          "!src/",
          "!**/*.{iml,o,hprof,orig,pyc,pyo,rbc,swp,csproj,sln,xproj}",
          "!.editorconfig",
          "!**/._*",
          "!**/{.DS_Store,.git,.hg,.svn,CVS,RCS,SCCS,.gitignore,.gitattributes}",
          "!**/{__pycache__,thumbs.db,.flowconfig,.idea,.vs,.nyc_output}"
        ],
        // Setup automatico per l'avvio
        fileAssociations: [
          {
            ext: "mammt",
            name: "MockAMMT File",
            description: "MockAMMT Network Profile",
            icon: "build/fileicon.ico",
            role: "Editor"
          }
        ],
        // Configurazione per l'aggiornamento automatico
        afterSign: "electron-builder-notarize",
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
