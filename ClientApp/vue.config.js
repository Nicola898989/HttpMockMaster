module.exports = {
  devServer: {
    port: 8080,
    host: '0.0.0.0',
    proxy: {
      '^/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true
      }
    }
  },
  css: {
    sourceMap: true
  },
  chainWebpack: config => {
    // Rimuovi il plugin progress che sta causando problemi
    config.plugins.delete('progress')
  },
  pluginOptions: {
    electronBuilder: {
      nodeIntegration: false,
      preload: 'src/preload.js'
    }
  }
}
