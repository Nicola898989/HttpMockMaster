<template>
  <div class="home">
    <div class="jumbotron text-center mb-4">
      <h1 class="display-4">HTTP Interceptor</h1>
      <p class="lead">
        A cross-platform HTTP interceptor, proxy and mock server application.
      </p>
      <hr class="my-4">
      <p>
        HTTP Interceptor is listening on port <code>8888</code>
      </p>
    </div>
    
    <div class="row">
      <div class="col-md-4 mb-3">
        <div class="card h-100">
          <div class="card-body">
            <h5 class="card-title"><i class="bi bi-list-ul"></i> HTTP Requests</h5>
            <p class="card-text">
              View and analyze all captured HTTP requests and responses.
            </p>
            <router-link to="/requests" class="btn btn-primary">
              View Requests
            </router-link>
          </div>
        </div>
      </div>
      
      <div class="col-md-4 mb-3">
        <div class="card h-100">
          <div class="card-body">
            <h5 class="card-title"><i class="bi bi-gear"></i> Response Rules</h5>
            <p class="card-text">
              Create rules to automatically respond to specific requests.
            </p>
            <router-link to="/rules" class="btn btn-primary">
              Configure Rules
            </router-link>
          </div>
        </div>
      </div>
      
      <div class="col-md-4 mb-3">
        <div class="card h-100">
          <div class="card-body">
            <h5 class="card-title"><i class="bi bi-arrow-left-right"></i> Proxy Settings</h5>
            <p class="card-text">
              Configure proxy target and enable/disable mock mode.
            </p>
            <router-link to="/proxy" class="btn btn-primary">
              Setup Proxy
            </router-link>
          </div>
        </div>
      </div>
    </div>
    
    <div class="row mt-4">
      <div class="col-12">
        <div class="card">
          <div class="card-header">
            <h5><i class="bi bi-question-circle"></i> Quick Guide</h5>
          </div>
          <div class="card-body">
            <h6>HTTP Interception</h6>
            <p>
              All HTTP requests to port <code>8888</code> are automatically captured and logged.
              You can view the details including headers, body, and response in the Requests tab.
            </p>
            
            <h6>Response Rules</h6>
            <p>
              Configure response rules to automatically respond to specific HTTP requests 
              without forwarding them to any server.
              Rules match based on the request path and can return custom status codes, headers and body.
            </p>
            
            <h6>Proxy Mode</h6>
            <p>
              In proxy mode, requests are forwarded to the configured target domain.
              All requests and responses are still captured for analysis.
            </p>
            
            <h6>Mock Mode</h6>
            <p>
              In mock mode, the application doesn't forward requests to the real server.
              Instead, it responds according to the configured rules.
              This is useful for development and testing.
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'Home'
}
</script>

<style scoped>
.home {
  max-width: 1200px;
  margin: 0 auto;
}

.jumbotron {
  padding: 2rem 1rem;
  background-color: #f8f9fa;
  border-radius: 0.3rem;
}
</style>
