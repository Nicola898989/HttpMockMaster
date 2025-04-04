<template>
  <div class="export-options">
    <h3>Esporta Richieste</h3>
    
    <div class="export-form">
      <div class="form-group">
        <label for="exportFormat">Formato:</label>
        <select id="exportFormat" v-model="exportFormat" class="form-control">
          <option value="json">JSON</option>
          <option value="csv">CSV</option>
        </select>
      </div>
      
      <div class="filter-section">
        <h4>Filtri</h4>
        
        <div class="form-group">
          <label for="methodFilter">Metodo HTTP:</label>
          <select id="methodFilter" v-model="filters.method" class="form-control">
            <option value="">Tutti</option>
            <option value="GET">GET</option>
            <option value="POST">POST</option>
            <option value="PUT">PUT</option>
            <option value="DELETE">DELETE</option>
            <option value="PATCH">PATCH</option>
            <option value="OPTIONS">OPTIONS</option>
            <option value="HEAD">HEAD</option>
          </select>
        </div>
        
        <div class="form-group">
          <label for="hostFilter">Host:</label>
          <input id="hostFilter" type="text" v-model="filters.host" class="form-control" placeholder="es. api.example.com" />
        </div>
        
        <div class="form-group">
          <label for="fromDateFilter">Da data:</label>
          <input id="fromDateFilter" type="datetime-local" v-model="filters.fromDate" class="form-control" />
        </div>
        
        <div class="form-group">
          <label for="toDateFilter">A data:</label>
          <input id="toDateFilter" type="datetime-local" v-model="filters.toDate" class="form-control" />
        </div>
      </div>
      
      <div class="error-container" v-if="error">
        <div class="alert alert-danger">{{ error }}</div>
      </div>
      
      <div class="btn-container">
        <button class="btn btn-primary" @click="exportData" :disabled="isExporting">
          <span v-if="isExporting">
            <i class="spinner"></i> Esportazione in corso...
          </span>
          <span v-else>
            Esporta
          </span>
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import { exportService } from '@/services/exportService';

export default {
  name: 'ExportOptions',
  data() {
    return {
      exportFormat: 'json',
      filters: {
        method: '',
        host: '',
        fromDate: '',
        toDate: ''
      },
      isExporting: false,
      error: null
    };
  },
  methods: {
    exportData() {
      this.isExporting = true;
      this.error = null;
      
      try {
        // Prepariamo i filtri
        const filters = { ...this.filters };
        
        // Convertiamo il formato delle date se fornite
        if (filters.fromDate) {
          filters.fromDate = new Date(filters.fromDate);
        }
        
        if (filters.toDate) {
          filters.toDate = new Date(filters.toDate);
        }
        
        // In base al formato scelto, chiamiamo il metodo appropriato
        if (this.exportFormat === 'json') {
          exportService.exportAsJson(filters)
            .then(() => {
              this.isExporting = false;
            })
            .catch(error => {
              this.error = `Errore durante l'esportazione JSON: ${error.message}`;
              this.isExporting = false;
            });
        } else if (this.exportFormat === 'csv') {
          exportService.exportAsCsv(filters)
            .then(() => {
              this.isExporting = false;
            })
            .catch(error => {
              this.error = `Errore durante l'esportazione CSV: ${error.message}`;
              this.isExporting = false;
            });
        }
      } catch (err) {
        this.error = `Errore imprevisto: ${err.message}`;
        this.isExporting = false;
      }
    }
  }
};
</script>

<style scoped>
.export-options {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  margin-bottom: 20px;
}

h3 {
  margin-bottom: 20px;
  color: #343a40;
}

h4 {
  margin: 15px 0;
  color: #495057;
  font-size: 1.1rem;
}

.form-group {
  margin-bottom: 15px;
}

label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
  color: #495057;
}

.form-control {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #ced4da;
  border-radius: 4px;
  font-size: 14px;
}

.filter-section {
  margin-top: 20px;
  padding-top: 10px;
  border-top: 1px solid #dee2e6;
}

.error-container {
  margin: 15px 0;
}

.btn-container {
  margin-top: 20px;
}

.btn {
  padding: 8px 16px;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-primary {
  background-color: #007bff;
  color: white;
  border: none;
}

.btn-primary:hover {
  background-color: #0069d9;
}

.btn-primary:disabled {
  background-color: #80bdff;
  cursor: not-allowed;
}

.spinner {
  display: inline-block;
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: #fff;
  animation: spin 1s ease-in-out infinite;
  margin-right: 8px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>