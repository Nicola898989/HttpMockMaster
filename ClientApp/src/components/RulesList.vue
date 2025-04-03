<template>
  <div class="rules-list card">
    <div class="card-header">
      <h5 class="mb-0">Rules ({{ rules.length }})</h5>
    </div>
    
    <div class="list-group list-group-flush rules-container">
      <div v-if="rules.length === 0" class="text-center p-4">
        <i data-feather="filter" class="feather-large mb-3"></i>
        <p class="text-muted">No rules defined</p>
        <p class="small">
          Create rules to automatically respond to specific requests.
        </p>
      </div>
      
      <a v-for="rule in rules" 
         :key="rule.id" 
         href="#" 
         class="list-group-item list-group-item-action py-3 hover-row"
         @click.prevent="editRule(rule)"
         :class="{ 'disabled-rule': !rule.isActive }">
        
        <div class="d-flex w-100 justify-content-between">
          <h6 class="mb-0">{{ rule.name }}</h6>
          <div>
            <span v-if="rule.method" class="badge rounded-pill http-method" 
                  :class="'method-' + rule.method.toLowerCase()">
              {{ rule.method }}
            </span>
            <span class="badge bg-secondary ms-1">Priority: {{ rule.priority }}</span>
          </div>
        </div>
        
        <div class="text-truncate-container">
          <small class="text-muted">
            {{ rule.pathPattern || '/*' }}
          </small>
        </div>
        
        <div class="d-flex mt-2">
          <div>
            <span class="badge rounded-pill" 
                  :class="getStatusClass(rule.response?.statusCode)">
              {{ rule.response?.statusCode || '200' }}
            </span>
            <small class="text-muted ms-2">
              {{ rule.description || 'No description' }}
            </small>
          </div>
          
          <div class="ms-auto">
            <div class="form-check form-switch d-inline-block me-2">
              <input class="form-check-input" type="checkbox" role="switch" 
                     :id="'rule-active-' + rule.id" 
                     :checked="rule.isActive"
                     @click.stop="toggleRule(rule)">
            </div>
            <button class="btn btn-sm btn-outline-danger" 
                    @click.stop="deleteRule(rule.id)"
                    title="Delete rule">
              <i data-feather="trash-2" class="feather-sm"></i>
            </button>
          </div>
        </div>
      </a>
    </div>
  </div>
</template>

<script>
export default {
  name: 'RulesList',
  
  props: {
    rules: {
      type: Array,
      required: true
    }
  },
  
  emits: ['edit-rule', 'delete-rule', 'toggle-rule'],
  
  updated() {
    feather.replace()
  },
  
  methods: {
    editRule(rule) {
      this.$emit('edit-rule', rule)
    },
    
    deleteRule(id) {
      this.$emit('delete-rule', id)
    },
    
    toggleRule(rule) {
      this.$emit('toggle-rule', rule)
    },
    
    getStatusClass(statusCode) {
      if (!statusCode) return 'status-2xx'
      
      if (statusCode >= 200 && statusCode < 300) {
        return 'status-2xx'
      } else if (statusCode >= 300 && statusCode < 400) {
        return 'status-3xx'
      } else if (statusCode >= 400 && statusCode < 500) {
        return 'status-4xx'
      } else if (statusCode >= 500) {
        return 'status-5xx'
      }
      return ''
    }
  }
}
</script>

<style scoped>
.rules-container {
  max-height: 600px;
  overflow-y: auto;
}

.feather-sm {
  width: 14px;
  height: 14px;
}

.feather-large {
  width: 48px;
  height: 48px;
  stroke: #adb5bd;
}

.disabled-rule {
  opacity: 0.6;
}
</style>
