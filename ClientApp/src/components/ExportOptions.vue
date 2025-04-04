<template>
  <div class="export-container">
    <h3>Esportazione Dati</h3>
    
    <div class="filter-section">
      <h4>Filtri</h4>
      <div class="filter-row">
        <div class="filter-group">
          <label>Da data:</label>
          <input type="datetime-local" v-model="filters.fromDate" class="form-control" />
        </div>
        
        <div class="filter-group">
          <label>A data:</label>
          <input type="datetime-local" v-model="filters.toDate" class="form-control" />
        </div>
      </div>
      
      <div class="filter-row">
        <div class="filter-group">
          <label>Metodo HTTP:</label>
          <select v-model="filters.method" class="form-control">
            <option value="">Tutti</option>
            <option value="GET">GET</option>
            <option value="POST">POST</option>
            <option value="PUT">PUT</option>
            <option value="DELETE">DELETE</option>
            <option value="PATCH">PATCH</option>
          </select>
        </div>
        
        <div class="filter-group">
          <label>Host:</label>
          <input type="text" v-model="filters.host" class="form-control" placeholder="es. localhost" />
        </div>
      </div>
      
      <div class="filter-actions">
        <button @click="resetFilters" class="reset-btn">Resetta Filtri</button>
      </div>
    </div>
    
    <div class="export-actions">
      <h4>Esporta Come:</h4>
      <div class="action-buttons">
        <button @click="exportAsJson" class="export-btn json-btn" :disabled="isExporting">
          <span v-if="isExporting && exportType === 'json'">
            <i class="spinning-icon"></i> Esportazione in corso...
          </span>
          <span v-else>JSON</span>
        </button>
        
        <button @click="exportAsCsv" class="export-btn csv-btn" :disabled="isExporting">
          <span v-if="isExporting && exportType === 'csv'">
            <i class="spinning-icon"></i> Esportazione in corso...
          </span>
          <span v-else>CSV</span>
        </button>
      </div>
    </div>
    
    <div v-if="currentRequest" class="export-single">
      <h4>Esporta dettagli richiesta attuale:</h4>
      <button @click="exportCurrentRequest" class="export-btn detail-btn" :disabled="isExporting">
        <span v-if="isExporting && exportType === 'detail'">
          <i class="spinning-icon"></i> Esportazione in corso...
        </span>
        <span v-else>Esporta dettagli</span>
      </button>
    </div>
    
    <div v-if="error" class="error-message">
      {{ error }}
    </div>
  </div>
</template>

<script>
import { exportService } from '../services/exportService';

export default {
  name: 'ExportOptions',
  props: {
    currentRequest: {
      type: Object,
      default: null
    }
  },
  data() {
    return {
      filters: {
        fromDate: null,
        toDate: null,
        method: '',
        host: ''
      },
      isExporting: false,
      exportType: null,
      error: null
    };
  },
  methods: {
    resetFilters() {
      this.filters = {
        fromDate: null,
        toDate: null,
        method: '',
        host: ''
      };
    },
    
    async exportAsJson() {
      this.error = null;
      this.isExporting = true;
      this.exportType = 'json';
      
      try {
        await exportService.exportAsJson(this.filters);
      } catch (error) {
        this.error = `Errore durante l'esportazione JSON: ${error.message}`;
        console.error(error);
      } finally {
        this.isExporting = false;
      }
    },
    
    async exportAsCsv() {
      this.error = null;
      this.isExporting = true;
      this.exportType = 'csv';
      
      try {
        await exportService.exportAsCsv(this.filters);
      } catch (error) {
        this.error = `Errore durante l'esportazione CSV: ${error.message}`;
        console.error(error);
      } finally {
        this.isExporting = false;
      }
    },
    
    async exportCurrentRequest() {
      if (!this.currentRequest) return;
      
      this.error = null;
      this.isExporting = true;
      this.exportType = 'detail';
      
      try {
        await exportService.exportRequestDetails(this.currentRequest.id);
      } catch (error) {
        this.error = `Errore durante l'esportazione dei dettagli: ${error.message}`;
        console.error(error);
      } finally {
        this.isExporting = false;
      }
    }
  }
};
</script>

<style scoped>
.export-container {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 1.5rem;
  box-shadow: 0 2px 5px rgba(0,0,0,0.05);
}

h3 {
  margin-top: 0;
  margin-bottom: 1.25rem;
  font-size: 1.4rem;
  color: #343a40;
}

h4 {
  margin-top: 0;
  margin-bottom: 1rem;
  font-size: 1.1rem;
  color: #495057;
}

.filter-section {
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid #e9ecef;
}

.filter-row {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
}

.filter-group {
  flex: 1;
}

label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  font-size: 0.9rem;
  color: #495057;
}

.form-control {
  width: 100%;
  padding: 0.5rem;
  border: 1px solid #ced4da;
  border-radius: 4px;
  font-size: 0.9rem;
}

.filter-actions {
  margin-top: 1rem;
  text-align: right;
}

.reset-btn {
  background-color: #f1f3f5;
  border: 1px solid #ced4da;
  border-radius: 4px;
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.reset-btn:hover {
  background-color: #e9ecef;
}

.export-actions, .export-single {
  margin-top: 1.5rem;
}

.action-buttons {
  display: flex;
  gap: 1rem;
}

.export-btn {
  padding: 0.625rem 1.25rem;
  border: none;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  min-width: 120px;
  text-align: center;
}

.json-btn {
  background-color: #228be6;
  color: white;
}

.json-btn:hover:not(:disabled) {
  background-color: #1c7ed6;
}

.csv-btn {
  background-color: #40c057;
  color: white;
}

.csv-btn:hover:not(:disabled) {
  background-color: #37b24d;
}

.detail-btn {
  background-color: #7950f2;
  color: white;
}

.detail-btn:hover:not(:disabled) {
  background-color: #6741d9;
}

button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.spinning-icon {
  display: inline-block;
  width: 1rem;
  height: 1rem;
  border: 2px solid rgba(255,255,255,0.3);
  border-radius: 50%;
  border-top-color: #fff;
  animation: spin 1s ease-in-out infinite;
  margin-right: 0.5rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-message {
  margin-top: 1.5rem;
  padding: 0.75rem;
  background-color: #fff5f5;
  color: #e03131;
  border-radius: 4px;
  border-left: 4px solid #e03131;
  font-size: 0.9rem;
}

@media (max-width: 768px) {
  .filter-row {
    flex-direction: column;
  }
  
  .action-buttons {
    flex-direction: column;
  }
  
  .export-btn {
    margin-bottom: 0.5rem;
  }
}
</style>