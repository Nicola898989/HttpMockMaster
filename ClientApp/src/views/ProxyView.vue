<template>
  <div class="proxy-view">
    <h2 class="mb-4">Proxy Configuration</h2>
    
    <div class="row">
      <div class="col-md-6">
        <ProxySettings />
      </div>
      
      <div class="col-md-6">
        <div class="card">
          <div class="card-header">
            <h5 class="mb-0">Information</h5>
          </div>
          <div class="card-body">
            <h6>How to use the proxy</h6>
            <p>
              Configure your application to use the following proxy address:
            </p>
            <div class="bg-light p-2 rounded mb-3">
              <code>http://localhost:8888</code>
            </div>
            
            <p>The proxy will:</p>
            <ul>
              <li>Forward all requests to the configured target domain</li>
              <li>Record the request and response details</li>
              <li>Apply any matching rules before forwarding</li>
            </ul>
            
            <h6 class="mt-4">Testing locally</h6>
            <p>
              For local applications, configure them to use this proxy. All HTTP requests will be 
              visible in the Requests tab.
            </p>
            
            <h6 class="mt-4">Browser testing</h6>
            <p>
              To test with a browser, configure your browser's proxy settings to use 
              <code>localhost:8888</code> as the HTTP proxy.
            </p>
          </div>
        </div>
        
        <div class="card mt-3">
          <div class="card-header">
            <h5 class="mb-0">About HTTPS</h5>
          </div>
          <div class="card-body">
            <p class="text-warning">
              <i data-feather="alert-triangle" class="feather-sm"></i>
              HTTPS proxying requires additional setup for SSL inspection.
            </p>
            <p>
              To proxy HTTPS requests, you'll need to:
            </p>
            <ol>
              <li>Generate a root certificate</li>
              <li>Install the certificate on your system/browser</li>
              <li>Enable SSL inspection in the application settings</li>
            </ol>
            <p class="small text-muted">
              For security reasons, browsers and applications will display warnings 
              when using self-signed certificates for SSL inspection.
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import ProxySettings from '../components/ProxySettings.vue'

export default {
  name: 'ProxyView',
  
  components: {
    ProxySettings
  },
  
  updated() {
    feather.replace()
  }
}
</script>

<style scoped>
.feather-sm {
  width: 18px;
  height: 18px;
  vertical-align: text-bottom;
}
</style>
