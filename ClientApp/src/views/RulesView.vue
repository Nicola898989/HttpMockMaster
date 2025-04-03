<template>
  <div class="rules-view">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Response Rules</h2>
      
      <button class="btn btn-primary" @click="startNewRule">
        <i data-feather="plus"></i> New Rule
      </button>
    </div>
    
    <div class="row">
      <!-- Rules List -->
      <div class="col-md-5">
        <RulesList 
          :rules="rules" 
          @edit-rule="editRule" 
          @delete-rule="confirmDeleteRule" 
          @toggle-rule="toggleRuleStatus" 
        />
      </div>
      
      <!-- Rule Form -->
      <div class="col-md-7">
        <RuleForm 
          v-if="currentRule" 
          :rule="currentRule" 
          :is-editing="isEditing" 
          @save="saveRule" 
          @cancel="cancelEdit" 
        />
        <div v-else class="card">
          <div class="card-body text-center p-5">
            <i data-feather="sliders" class="feather-large mb-3"></i>
            <h4>No Rule Selected</h4>
            <p class="text-muted">
              Create a new rule or select an existing one to edit.
            </p>
            <button class="btn btn-primary" @click="startNewRule">Create New Rule</button>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Confirm Delete Modal -->
    <div class="modal fade" id="deleteRuleModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Confirm Delete</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            Are you sure you want to delete this rule?
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
import { mapState, mapActions } from 'vuex'
import { Modal } from 'bootstrap'
import RulesList from '../components/RulesList.vue'
import RuleForm from '../components/RuleForm.vue'

export default {
  name: 'RulesView',
  
  components: {
    RulesList,
    RuleForm
  },
  
  data() {
    return {
      ruleToDelete: null,
      deleteModal: null
    }
  },
  
  computed: {
    ...mapState('rules', [
      'rules',
      'currentRule',
      'isEditing'
    ])
  },
  
  mounted() {
    this.fetchRules()
    this.deleteModal = new Modal(document.getElementById('deleteRuleModal'))
  },
  
  updated() {
    feather.replace()
  },
  
  methods: {
    ...mapActions('rules', [
      'fetchRules',
      'startNewRule',
      'editRule',
      'cancelEdit',
      'createRule',
      'updateRule',
      'deleteRule'
    ]),
    
    async saveRule(rule) {
      let success
      
      if (this.isEditing) {
        success = await this.updateRule(rule)
      } else {
        success = await this.createRule(rule)
      }
      
      if (success) {
        this.cancelEdit()
      }
    },
    
    confirmDeleteRule(ruleId) {
      this.ruleToDelete = ruleId
      this.deleteModal.show()
    },
    
    async deleteRule() {
      if (this.ruleToDelete) {
        const success = await this.deleteRule(this.ruleToDelete)
        if (success) {
          this.ruleToDelete = null
          this.deleteModal.hide()
        }
      }
    },
    
    async toggleRuleStatus(rule) {
      const updatedRule = { ...rule, isActive: !rule.isActive }
      await this.updateRule(updatedRule)
    }
  }
}
</script>

<style scoped>
.feather-large {
  width: 48px;
  height: 48px;
  stroke: #6c757d;
}
</style>
