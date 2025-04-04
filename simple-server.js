const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const path = require('path');
const app = express();
const port = 5000;

// Abilita il CORS per tutti gli endpoint
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*');
  res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization');
  res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  if (req.method === 'OPTIONS') {
    return res.sendStatus(200);
  }
  next();
});

// Proxy per le richieste API al backend
// Usiamo .net per l'API, quindi il server Express serve solo la pagina di test
// e non il proxy delle API.
/*
app.use('/api', createProxyMiddleware({
  target: 'http://localhost:5000',
  changeOrigin: true,
  pathRewrite: {
    '^/api': '/api'
  }
}));
*/

// Pagina di status e test per verificare il funzionamento
app.get('/', (req, res) => {
  res.send(`
    <html>
      <head>
        <title>MockAMMT API Status</title>
        <style>
          body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
          .status { padding: 10px; border-radius: 5px; margin-bottom: 10px; }
          .ok { background-color: #d4edda; }
          .error { background-color: #f8d7da; }
          pre { background-color: #f5f5f5; padding: 10px; border-radius: 5px; overflow: auto; }
          button { padding: 8px 12px; background-color: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; }
          button:hover { background-color: #0069d9; }
        </style>
      </head>
      <body>
        <h1>MockAMMT API Status</h1>
        <div class="status ok">Proxy server running on port ${port}</div>
        
        <h2>Test API Endpoints</h2>
        <button onclick="testEndpoint('/api/requests')">Test Requests API</button>
        <button onclick="testEndpoint('/api/performance/metrics')">Test Performance API</button>
        <button onclick="testEndpoint('/api/comparison/requests', 'POST', [1, 2])">Test Comparison API</button>
        
        <h3>Response:</h3>
        <pre id="response">Click a button to test an endpoint...</pre>
        
        <script>
          async function testEndpoint(url, method = 'GET', body = null) {
            const responseEl = document.getElementById('response');
            responseEl.innerText = 'Loading...';
            
            try {
              const options = {
                method,
                headers: { 'Content-Type': 'application/json' }
              };
              
              if (body) {
                options.body = JSON.stringify(body);
              }
              
              const response = await fetch(url, options);
              const data = await response.json();
              responseEl.innerText = JSON.stringify(data, null, 2);
            } catch (error) {
              responseEl.innerText = 'Error: ' + error.message;
            }
          }
        </script>
      </body>
    </html>
  `);
});

app.listen(port, '0.0.0.0', () => {
  console.log(`Server proxy in esecuzione su http://0.0.0.0:${port}`);
});