<template>
  <div class="rule-form card">
    <div class="card-header">
      <h5 class="mb-0">{{ isEditing ? 'Edit Rule' : 'Create New Rule' }}</h5>
    </div>
    
    <div class="card-body">
      <form @submit.prevent="saveRule">
        <!-- Basic Information -->
        <div class="mb-3">
          <label for="ruleName" class="form-label">Rule Name *</label>
          <input type="text" 
                 class="form-control" 
                 id="ruleName" 
                 v-model="formData.name"
                 required>
        </div>
        
        <div class="mb-3">
          <label for="ruleDescription" class="form-label">Description</label>
          <textarea class="form-control" 
                    id="ruleDescription" 
                    v-model="formData.description"
                    rows="2"></textarea>
        </div>
        
        <div class="row mb-3">
          <div class="col-md-6">
            <label for="ruleMethod" class="form-label">HTTP Method</label>
            <select class="form-select" id="ruleMethod" v-model="formData.method">
              <option value="">Any</option>
              <option value="GET">GET</option>
              <option value="POST">POST</option>
              <option value="PUT">PUT</option>
              <option value="DELETE">DELETE</option>
              <option value="PATCH">PATCH</option>
              <option value="OPTIONS">OPTIONS</option>
              <option value="HEAD">HEAD</option>
            </select>
            <div class="form-text">Leave empty to match any method</div>
          </div>
          
          <div class="col-md-6">
            <label for="rulePriority" class="form-label">Priority</label>
            <input type="number" 
                   class="form-control" 
                   id="rulePriority" 
                   v-model="formData.priority"
                   min="0"
                   max="100">
            <div class="form-text">Higher values are evaluated first</div>
          </div>
        </div>
        
        <!-- Matching Criteria -->
        <div class="card mb-3">
          <div class="card-header bg-light">
            <h6 class="mb-0">Matching Criteria</h6>
          </div>
          <div class="card-body">
            <div class="mb-3">
              <label for="pathPattern" class="form-label">Path Pattern</label>
              <input type="text" 
                     class="form-control" 
                     id="pathPattern" 
                     v-model="formData.pathPattern"
                     placeholder="/api/users/:id">
              <div class="form-text">
                URL path to match. Can be a specific path, wildcard, or regex (starts with ^ or ends with $)
              </div>
            </div>
            
            <div class="mb-3">
              <label for="queryPattern" class="form-label">Query Parameters</label>
              <input type="text" 
                     class="form-control" 
                     id="queryPattern" 
                     v-model="formData.queryPattern"
                     placeholder="id=123&type=user">
              <div class="form-text">
                Query parameters to match, e.g. "id=123&type=user" or regex pattern
              </div>
            </div>
            
            <div class="mb-3">
              <label for="headerPattern" class="form-label">Header Pattern</label>
              <input type="text" 
                     class="form-control" 
                     id="headerPattern" 
                     v-model="formData.headerPattern"
                     placeholder="Content-Type: application/json">
              <div class="form-text">
                Headers to match. Format: "Header-Name: value" (use ; for multiple)
              </div>
            </div>
            
            <div class="mb-3">
              <label for="bodyPattern" class="form-label">Body Pattern</label>
              <textarea class="form-control" 
                        id="bodyPattern" 
                        v-model="formData.bodyPattern"
                        rows="2"
                        placeholder='{"user": "admin"}'></textarea>
              <div class="form-text">
                Request body to match. Can be a specific string or regex pattern
              </div>
            </div>
          </div>
        </div>
        
        <!-- Response Configuration -->
        <div class="card mb-3">
          <div class="card-header bg-light">
            <h6 class="mb-0">Response Configuration</h6>
          </div>
          <div class="card-body">
            <div class="mb-3">
              <label for="statusCode" class="form-label">Status Code</label>
              <select class="form-select" id="statusCode" v-model="formData.response.statusCode">
                <option value="200">200 OK</option>
                <option value="201">201 Created</option>
                <option value="204">204 No Content</option>
                <option value="400">400 Bad Request</option>
                <option value="401">401 Unauthorized</option>
                <option value="403">403 Forbidden</option>
                <option value="404">404 Not Found</option>
                <option value="500">500 Internal Server Error</option>
                <option value="503">503 Service Unavailable</option>
              </select>
            </div>
            
            <div class="mb-3">
              <label for="responseHeaders" class="form-label">Response Headers</label>
              <textarea class="form-control" 
                        id="responseHeaders" 
                        v-model="formData.response.headers"
                        rows="3"
                        placeholder="Content-Type: application/json
Access-Control-Allow-Origin: *"></textarea>
              <div class="form-text">
                One header per line in format "Header-Name: value"
              </div>
            </div>
            
            <div class="mb-3">
              <label for="responseBody" class="form-label">Response Body</label>
              <div class="input-group mb-2">
                <button class="btn btn-outline-secondary" type="button" @click="formatJson">Format JSON</button>
                <button class="btn btn-outline-secondary" type="button" @click="generateJsonSchema">Generate Schema</button>
              </div>
              <textarea class="form-control code-editor" 
                        id="responseBody" 
                        v-model="formData.response.body"
                        rows="8"></textarea>
            </div>
          </div>
        </div>
        
        <div class="form-check mb-3">
          <input class="form-check-input" type="checkbox" id="isActive" v-model="formData.isActive">
          <label class="form-check-label" for="isActive">
            Rule is active
          </label>
        </div>
        
        <div class="d-flex justify-content-end">
          <button type="button" class="btn btn-secondary me-2" @click="cancelEdit">
            Cancel
          </button>
          <button type="submit" class="btn btn-primary">
            {{ isEditing ? 'Update Rule' : 'Create Rule' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script>
export default {
  name: 'RuleForm',
  
  props: {
    rule: {
      type: Object,
      required: true
    },
    isEditing: {
      type: Boolean,
      default: false
    }
  },
  
  emits: ['save', 'cancel'],
  
  data() {
    return {
      formData: {
        id: null,
        name: '',
        description: '',
        method: '',
        pathPattern: '',
        queryPattern: '',
        headerPattern: '',
        bodyPattern: '',
        priority: 10,
        isActive: true,
        response: {
          statusCode: 200,
          headers: 'Content-Type: application/json',
          body: '{}'
        }
      }
    }
  },
  
  created() {
    this.initForm()
  },
  
  methods: {
    initForm() {
      // Deep copy to avoid modifying the original rule
      if (this.rule) {
        this.formData = {
          id: this.rule.id,
          name: this.rule.name || '',
          description: this.rule.description || '',
          method: this.rule.method || '',
          pathPattern: this.rule.pathPattern || '',
          queryPattern: this.rule.queryPattern || '',
          headerPattern: this.rule.headerPattern || '',
          bodyPattern: this.rule.bodyPattern || '',
          priority: this.rule.priority || 10,
          isActive: this.rule.isActive !== undefined ? this.rule.isActive : true,
          response: {
            id: this.rule.response?.id,
            statusCode: this.rule.response?.statusCode || 200,
            headers: this.rule.response?.headers || 'Content-Type: application/json',
            body: this.rule.response?.body || '{}'
          }
        }
      }
    },
    
    saveRule() {
      this.$emit('save', { ...this.formData })
    },
    
    cancelEdit() {
      this.$emit('cancel')
    },
    
    formatJson() {
      try {
        const parsed = JSON.parse(this.formData.response.body)
        this.formData.response.body = JSON.stringify(parsed, null, 2)
      } catch (e) {
        // Not valid JSON, ignore
        alert('The current body is not valid JSON.')
      }
    },
    
    generateJsonSchema() {
      // Simple JSON schema generator based on the current body
      try {
        const parsed = JSON.parse(this.formData.response.body)
        const schema = this.createSchemaFromObject(parsed)
        this.formData.response.body = JSON.stringify(schema, null, 2)
      } catch (e) {
        alert('The current body is not valid JSON.')
      }
    },
    
    createSchemaFromObject(obj) {
      if (Array.isArray(obj)) {
        if (obj.length > 0) {
          return [this.createSchemaFromObject(obj[0])]
        } else {
          return []
        }
      } else if (typeof obj === 'object' && obj !== null) {
        const result = {}
        for (const [key, value] of Object.entries(obj)) {
          result[key] = this.createSchemaFromObject(value)
        }
        return result
      } else {
        return typeof obj
      }
    }
  },
  
  watch: {
    rule: {
      handler() {
        this.initForm()
      },
      deep: true
    }
  }
}
</script>

<style scoped>
.code-editor {
  font-family: monospace;
  font-size: 0.9rem;
}
</style>
