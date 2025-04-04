<template>
  <div class="export-options">
    <h3>Opzioni di esportazione</h3>
    
    <div class="export-filters">
      <div class="form-group">
        <label for="exportFormat">Formato</label>
        <select id="exportFormat" v-model="exportFormat" class="form-control">
          <option value="json">JSON</option>
          <option value="csv">CSV</option>
        </select>
      </div>
      
      <div class="form-group">
        <label for="startDate">Data di inizio</label>
        <input 
          type="date" 
          id="startDate" 
          v-model="filters.startDate" 
          class="form-control"
        />
      </div>
      
      <div class="form-group">
        <label for="endDate">Data di fine</label>
        <input 
          type="date" 
          id="endDate" 
          v-model="filters.endDate" 
          class="form-control"
        />
      </div>
      
      <div class="form-group">
        <label for="method">Metodo HTTP</label>
        <select id="method" v-model="filters.method" class="form-control">
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
        <label for="host">Host</label>
        <input 
          type="text" 
          id="host" 
          v-model="filters.host" 
          class="form-control" 
          placeholder="Es. example.com"
        />
      </div>
    </div>
    
    <div class="export-actions">
      <button 
        @click="exportData" 
        class="btn btn-primary"
        :disabled="isExporting"
      >
        <span v-if="isExporting">
          <i class="fas fa-spinner fa-spin"></i> Esportazione...
        </span>
        <span v-else>
          <i class="fas fa-download"></i> Esporta
        </span>
      </button>
      
      <button 
        @click="resetFilters" 
        class="btn btn-outline-secondary ml-2"
        :disabled="isExporting"
      >
        <i class="fas fa-sync-alt"></i> Reset filtri
      </button>
    </div>
    
    <div v-if="error" class="alert alert-danger mt-3">
      <i class="fas fa-exclamation-triangle"></i> {{ error }}
    </div>
    
    <div v-if="success" class="alert alert-success mt-3">
      <i class="fas fa-check-circle"></i> {{ success }}
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
        startDate: '',
        endDate: '',
        method: '',
        host: ''
      },
      isExporting: false,
      error: null,
      success: null
    };
  },
  methods: {
    async exportData() {
      this.error = null;
      this.success = null;
      this.isExporting = true;
      
      try {
        let result;
        let filename;
        const formattedDate = new Date().toISOString().slice(0, 10).replace(/-/g, '');
        
        // Preparazione dei filtri
        const exportFilters = {};
        
        if (this.filters.startDate) {
          exportFilters.fromDate = new Date(this.filters.startDate);
        }
        
        if (this.filters.endDate) {
          exportFilters.toDate = new Date(this.filters.endDate);
        }
        
        if (this.filters.method) {
          exportFilters.method = this.filters.method;
        }
        
        if (this.filters.host) {
          exportFilters.host = this.filters.host;
        }
        
        // Esportazione in base al formato scelto
        if (this.exportFormat === 'json') {
          result = await exportService.exportAsJson(exportFilters);
          filename = `http_requests_${formattedDate}.json`;
        } else {
          result = await exportService.exportAsCsv(exportFilters);
          filename = `http_requests_${formattedDate}.csv`;
        }
        
        // Download del file
        exportService.downloadFile(result, filename);
        
        this.success = `Esportazione completata con successo. File: ${filename}`;
      } catch (error) {
        console.error('Errore durante l\'esportazione:', error);
        this.error = `Si è verificato un errore durante l'esportazione: ${error.message || 'Errore sconosciuto'}`;
      } finally {
        this.isExporting = false;
      }
    },
    
    resetFilters() {
      this.filters = {
        startDate: '',
        endDate: '',
        method: '',
        host: ''
      };
      this.error = null;
      this.success = null;
    }
  }
};
</script>

<style scoped>
.export-options {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
}

.export-filters {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 15px;
  margin-bottom: 20px;
}

.form-group {
  margin-bottom: 0;
}

label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

.export-actions {
  display: flex;
  gap: 10px;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.ml-2 {
  margin-left: 0.5rem;
}

.mt-3 {
  margin-top: 1rem;
}
</style>