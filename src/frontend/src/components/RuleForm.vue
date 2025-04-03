<template>
  <form @submit.prevent="handleSubmit">
    <div class="mb-3">
      <label for="ruleName" class="form-label">Rule Name</label>
      <input 
        type="text" 
        class="form-control" 
        id="ruleName" 
        v-model="formData.name" 
        required
        maxlength="100"
      >
    </div>
    
    <div class="mb-3">
      <label for="pathPattern" class="form-label">Path Pattern</label>
      <input 
        type="text" 
        class="form-control" 
        id="pathPattern" 
        v-model="formData.pathPattern" 
        required
        maxlength="255"
        placeholder="/api/users/:id"
      >
      <div class="form-text">
        The path pattern to match against incoming requests
      </div>
    </div>
    
    <div class="row mb-3">
      <div class="col-md-4">
        <label for="statusCode" class="form-label">Status Code</label>
        <input 
          type="number" 
          class="form-control" 
          id="statusCode" 
          v-model.number="formData.statusCode" 
          required
          min="100"
          max="599"
        >
      </div>
      
      <div class="col-md-8">
        <label for="contentType" class="form-label">Content Type</label>
        <input 
          type="text" 
          class="form-control" 
          id="contentType" 
          v-model="formData.contentType" 
          placeholder="application/json"
        >
      </div>
    </div>
    
    <div class="mb-3">
      <label for="headers" class="form-label">Response Headers (JSON)</label>
      <textarea 
        class="form-control" 
        id="headers" 
        v-model="formData.headers" 
        rows="3"
        :class="{ 'is-invalid': headersError }"
      ></textarea>
      <div class="invalid-feedback" v-if="headersError">
        {{ headersError }}
      </div>
      <div class="form-text">
        Example: {"Content-Type": "application/json", "X-Custom-Header": "Value"}
      </div>
    </div>
    
    <div class="mb-3">
      <label for="responseBody" class="form-label">Response Body</label>
      <textarea 
        class="form-control" 
        id="responseBody" 
        v-model="formData.responseBody" 
        rows="5"
        :class="{ 'is-invalid': bodyError }"
      ></textarea>
      <div class="invalid-feedback" v-if="bodyError">
        {{ bodyError }}
      </div>
      <div class="form-text">
        For JSON, example: {"message": "Success", "data": {"id": 123}}
      </div>
    </div>
    
    <div class="mb-3 form-check">
      <input type="checkbox" class="form-check-input" id="isActive" v-model="formData.isActive">
      <label class="form-check-label" for="isActive">Rule is active</label>
    </div>
    
    <div class="d-flex justify-content-end mt-4">
      <button type="button" class="btn btn-secondary me-2" @click="$emit('cancel')">
        Cancel
      </button>
      <button type="submit" class="btn btn-primary" :disabled="!isFormValid || isSaving">
        <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
        {{ isEditMode ? 'Update' : 'Create' }} Rule
      </button>
    </div>
  </form>
</template>

<script>
export default {
  name: 'RuleForm',
  
  props: {
    rule: {
      type: Object,
      required: true
    },
    isEditMode: {
      type: Boolean,
      default: false
    }
  },
  
  data() {
    return {
      formData: {
        id: 0,
        name: '',
        pathPattern: '',
        statusCode: 200,
        contentType: 'application/json',
        headers: '{}',
        responseBody: '{}',
        isActive: true
      },
      headersError: '',
      bodyError: '',
      isSaving: false
    };
  },
  
  computed: {
    isFormValid() {
      return this.formData.name.trim() !== '' && 
             this.formData.pathPattern.trim() !== '' && 
             !this.headersError && 
             !this.bodyError;
    }
  },
  
  watch: {
    rule: {
      handler(newRule) {
        if (newRule) {
          this.formData = { ...newRule };
        }
      },
      immediate: true
    },
    'formData.headers': {
      handler(newHeaders) {
        this.validateHeaders(newHeaders);
      }
    },
    'formData.responseBody': {
      handler(newBody) {
        this.validateBody(newBody);
      }
    }
  },
  
  methods: {
    validateHeaders(headers) {
      if (!headers) {
        this.formData.headers = '{}';
        return;
      }
      
      try {
        JSON.parse(headers);
        this.headersError = '';
      } catch (e) {
        this.headersError = 'Invalid JSON format for headers';
      }
    },
    
    validateBody(body) {
      if (!body) {
        this.bodyError = '';
        return;
      }
      
      if (this.formData.contentType && this.formData.contentType.includes('json')) {
        try {
          JSON.parse(body);
          this.bodyError = '';
        } catch (e) {
          this.bodyError = 'Invalid JSON format for response body';
        }
      } else {
        this.bodyError = '';
      }
    },
    
    async handleSubmit() {
      if (!this.isFormValid) return;
      
      this.isSaving = true;
      
      try {
        await this.$emit('save', this.formData);
      } catch (error) {
        console.error('Error saving rule:', error);
      } finally {
        this.isSaving = false;
      }
    }
  }
};
</script>
