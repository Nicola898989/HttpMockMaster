<template>
  <div class="rules-list">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h3><i class="bi bi-gear"></i> Response Rules</h3>
      <div>
        <button @click="refresh" class="btn btn-outline-primary me-2" :disabled="loading">
          <i class="bi bi-arrow-repeat"></i> Refresh
        </button>
        <button @click="showAddRule" class="btn btn-success">
          <i class="bi bi-plus-circle"></i> Add Rule
        </button>
      </div>
    </div>

    <div class="alert alert-info" v-if="!rules.length && !loading">
      <i class="bi bi-info-circle"></i> No rules configured yet. 
      Rules allow you to automatically respond to specific HTTP requests.
    </div>
    
    <div v-if="loading" class="text-center p-3">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>
    
    <div class="list-group mb-3" v-else>
      <div v-for="rule in rules" :key="rule.id" class="rule-item">
        <div class="d-flex justify-content-between align-items-center">
          <div>
            <h5 class="mb-1">{{ rule.name }}</h5>
            <p class="mb-1"><strong>Path Pattern:</strong> {{ rule.pathPattern }}</p>
            <p class="mb-1"><strong>Status Code:</strong> {{ rule.statusCode }}</p>
            <div>
              <span class="badge" :class="rule.isActive ? 'bg-success' : 'bg-secondary'">
                {{ rule.isActive ? 'Active' : 'Inactive' }}
              </span>
            </div>
          </div>
          <div>
            <button @click="editRule(rule)" class="btn btn-sm btn-outline-primary me-2">
              <i class="bi bi-pencil"></i> Edit
            </button>
            <button @click="confirmDeleteRule(rule)" class="btn btn-sm btn-outline-danger">
              <i class="bi bi-trash"></i> Delete
            </button>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Rule Form Modal -->
    <div class="modal fade" id="ruleFormModal" tabindex="-1" aria-labelledby="ruleFormModalLabel" aria-hidden="true">
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="ruleFormModalLabel">{{ isEditMode ? 'Edit Rule' : 'Add New Rule' }}</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <RuleForm 
              :rule="currentRule" 
              :is-edit-mode="isEditMode"
              @save="saveRule"
              @cancel="closeModal"
            />
          </div>
        </div>
      </div>
    </div>
    
    <!-- Delete Confirmation Modal -->
    <div class="modal fade" id="deleteRuleModal" tabindex="-1" aria-labelledby="deleteRuleModalLabel" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="deleteRuleModalLabel">Delete Rule</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body" v-if="ruleToDelete">
            Are you sure you want to delete the rule "{{ ruleToDelete.name }}"?
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-danger" @click="deleteRule">Delete</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { mapState } from 'vuex';
import { Modal } from 'bootstrap';
import RuleForm from './RuleForm.vue';

export default {
  name: 'RulesList',
  components: {
    RuleForm
  },
  
  computed: {
    ...mapState({
      rules: state => state.rules,
      loading: state => state.loading.rules
    })
  },
  
  data() {
    return {
      isEditMode: false,
      currentRule: null,
      ruleToDelete: null,
      formModal: null,
      deleteModal: null
    };
  },
  
  mounted() {
    this.fetchRules();
    this.formModal = new Modal(document.getElementById('ruleFormModal'));
    this.deleteModal = new Modal(document.getElementById('deleteRuleModal'));
  },
  
  methods: {
    async fetchRules() {
      await this.$store.dispatch('fetchRules');
    },
    
    refresh() {
      this.fetchRules();
    },
    
    showAddRule() {
      this.isEditMode = false;
      this.currentRule = {
        name: '',
        pathPattern: '',
        statusCode: 200,
        contentType: 'application/json',
        headers: '{}',
        responseBody: '{}',
        isActive: true
      };
      this.formModal.show();
    },
    
    editRule(rule) {
      this.isEditMode = true;
      // Create a copy to avoid mutating the store directly
      this.currentRule = { ...rule };
      this.formModal.show();
    },
    
    async saveRule(rule) {
      try {
        if (this.isEditMode) {
          await this.$store.dispatch('updateRule', rule);
        } else {
          await this.$store.dispatch('createRule', rule);
        }
        this.closeModal();
        this.fetchRules();
      } catch (error) {
        console.error('Error saving rule:', error);
      }
    },
    
    confirmDeleteRule(rule) {
      this.ruleToDelete = rule;
      this.deleteModal.show();
    },
    
    async deleteRule() {
      if (!this.ruleToDelete) return;
      
      try {
        await this.$store.dispatch('deleteRule', this.ruleToDelete.id);
        this.deleteModal.hide();
        this.ruleToDelete = null;
      } catch (error) {
        console.error('Error deleting rule:', error);
      }
    },
    
    closeModal() {
      this.formModal.hide();
    }
  }
};
</script>

<style scoped>
.rules-list {
  height: 100%;
  overflow-y: auto;
}

.rule-item {
  padding: 15px;
  margin-bottom: 10px;
  border: 1px solid #dee2e6;
  border-radius: 4px;
}

.rule-item:hover {
  background-color: #f8f9fa;
}
</style>
