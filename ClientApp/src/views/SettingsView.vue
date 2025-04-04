<template>
  <div class="settings-view">
    <h1>Impostazioni e Strumenti</h1>
    
    <div class="settings-sections">
      <section class="settings-section">
        <h2>Esportazione Dati</h2>
        <export-options />
      </section>
      
      <section class="settings-section">
        <h2>Configurazione Applicazione</h2>
        <div class="form-group">
          <label for="maxRequests">Numero massimo di richieste da conservare:</label>
          <input 
            type="number" 
            id="maxRequests" 
            v-model="maxRequests"
            min="100"
            max="10000"
            step="100"
            class="form-control"
          />
          <small class="form-text text-muted">
            Imposta il numero massimo di richieste che verranno memorizzate nel database.
            Le richieste più vecchie verranno eliminate automaticamente quando si raggiunge questo limite.
          </small>
        </div>
        
        <div class="form-group">
          <label for="logLevel">Livello di log:</label>
          <select id="logLevel" v-model="logLevel" class="form-control">
            <option value="debug">Debug (Dettagliato)</option>
            <option value="info">Info (Standard)</option>
            <option value="warning">Warning (Solo avvisi)</option>
            <option value="error">Error (Solo errori)</option>
          </select>
        </div>
        
        <button @click="saveSettings" class="btn btn-primary">
          Salva Configurazione
        </button>
      </section>
      
      <section class="settings-section">
        <h2>Simulazione di Rete</h2>
        <network-simulation-settings />
      </section>
      
      <section class="settings-section danger-zone">
        <h2>Zona Pericolo</h2>
        
        <div class="danger-action">
          <div>
            <h3>Elimina tutte le richieste</h3>
            <p>Questa azione eliminerà definitivamente tutte le richieste HTTP registrate nel database.</p>
          </div>
          <button @click="confirmClearRequests" class="btn btn-danger">
            Elimina Dati
          </button>
        </div>
        
        <div class="danger-action">
          <div>
            <h3>Reset Completo</h3>
            <p>Questa azione ripristinerà l'applicazione alle impostazioni predefinite, eliminando tutti i dati e le configurazioni.</p>
          </div>
          <button @click="confirmReset" class="btn btn-danger">
            Reset
          </button>
        </div>
      </section>
    </div>
    
    <!-- Modal di conferma -->
    <div v-if="showConfirmModal" class="confirm-modal">
      <div class="modal-content">
        <h3>Conferma Azione</h3>
        <p>{{ confirmMessage }}</p>
        <div class="modal-actions">
          <button @click="executeConfirmedAction" class="btn btn-danger">Conferma</button>
          <button @click="cancelConfirmAction" class="btn btn-secondary">Annulla</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import ExportOptions from '@/components/ExportOptions.vue';
import NetworkSimulationSettings from '@/components/NetworkSimulationSettings.vue';
import axios from 'axios';

export default {
  name: 'SettingsView',
  components: {
    ExportOptions,
    NetworkSimulationSettings
  },
  data() {
    return {
      maxRequests: 1000,
      logLevel: 'info',
      showConfirmModal: false,
      confirmMessage: '',
      pendingAction: null
    };
  },
  mounted() {
    // Carica le impostazioni attuali
    this.loadSettings();
  },
  methods: {
    async loadSettings() {
      try {
        const response = await axios.get('/api/settings');
        this.maxRequests = response.data.maxRequests || 1000;
        this.logLevel = response.data.logLevel || 'info';
      } catch (error) {
        console.error('Errore durante il caricamento delle impostazioni:', error);
        // Usa valori predefiniti se il caricamento fallisce
      }
    },
    
    async saveSettings() {
      try {
        await axios.post('/api/settings', {
          maxRequests: this.maxRequests,
          logLevel: this.logLevel
        });
        
        // Feedback all'utente
        this.$store.dispatch('setMessage', {
          text: 'Impostazioni salvate con successo',
          type: 'success'
        });
      } catch (error) {
        console.error('Errore durante il salvataggio delle impostazioni:', error);
        this.$store.dispatch('setMessage', {
          text: `Errore durante il salvataggio: ${error.message || 'Errore sconosciuto'}`,
          type: 'error'
        });
      }
    },
    
    confirmClearRequests() {
      this.confirmMessage = 'Sei sicuro di voler eliminare tutte le richieste HTTP registrate? Questa azione non può essere annullata.';
      this.pendingAction = 'clearRequests';
      this.showConfirmModal = true;
    },
    
    confirmReset() {
      this.confirmMessage = 'Sei sicuro di voler ripristinare tutte le impostazioni predefinite e cancellare tutti i dati? Questa azione non può essere annullata.';
      this.pendingAction = 'resetAll';
      this.showConfirmModal = true;
    },
    
    cancelConfirmAction() {
      this.showConfirmModal = false;
      this.pendingAction = null;
    },
    
    async executeConfirmedAction() {
      if (this.pendingAction === 'clearRequests') {
        await this.clearAllRequests();
      } else if (this.pendingAction === 'resetAll') {
        await this.resetApplication();
      }
      
      this.showConfirmModal = false;
      this.pendingAction = null;
    },
    
    async clearAllRequests() {
      try {
        await this.$store.dispatch('requests/clearAllRequests');
        this.$store.dispatch('setMessage', {
          text: 'Tutte le richieste sono state eliminate con successo',
          type: 'success'
        });
      } catch (error) {
        console.error('Errore durante l\'eliminazione delle richieste:', error);
        this.$store.dispatch('setMessage', {
          text: `Errore durante l'eliminazione: ${error.message || 'Errore sconosciuto'}`,
          type: 'error'
        });
      }
    },
    
    async resetApplication() {
      try {
        // Elimina tutti i dati
        await axios.post('/api/reset');
        
        // Feedback all'utente
        this.$store.dispatch('setMessage', {
          text: 'Applicazione ripristinata alle impostazioni di fabbrica',
          type: 'success'
        });
        
        // Ricarica le impostazioni
        this.loadSettings();
        
        // Ricarica anche gli altri dati dell'applicazione
        this.$store.dispatch('requests/fetchRequests');
        this.$store.dispatch('rules/fetchRules');
      } catch (error) {
        console.error('Errore durante il reset dell\'applicazione:', error);
        this.$store.dispatch('setMessage', {
          text: `Errore durante il reset: ${error.message || 'Errore sconosciuto'}`,
          type: 'error'
        });
      }
    }
  }
};
</script>

<style scoped>
.settings-view {
  padding: 20px;
}

h1 {
  margin-bottom: 30px;
  color: #343a40;
}

.settings-sections {
  display: flex;
  flex-direction: column;
  gap: 30px;
}

.settings-section {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.settings-section h2 {
  margin-top: 0;
  margin-bottom: 20px;
  font-size: 1.5rem;
  color: #495057;
  border-bottom: 1px solid #dee2e6;
  padding-bottom: 10px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

.form-control {
  width: 100%;
  max-width: 400px;
  padding: 8px 12px;
  border: 1px solid #ced4da;
  border-radius: 4px;
  font-size: 14px;
}

.form-text {
  display: block;
  margin-top: 5px;
  font-size: 12px;
  color: #6c757d;
}

.btn {
  padding: 8px 16px;
  border-radius: 4px;
  font-weight: 500;
  border: none;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-primary:hover {
  background-color: #0069d9;
}

.btn-danger {
  background-color: #dc3545;
  color: white;
}

.btn-danger:hover {
  background-color: #c82333;
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background-color: #5a6268;
}

.danger-zone {
  border: 1px solid #dc3545;
}

.danger-zone h2 {
  color: #dc3545;
}

.danger-action {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 0;
  border-bottom: 1px solid #dee2e6;
}

.danger-action:last-child {
  border-bottom: none;
}

.danger-action h3 {
  margin: 0 0 5px 0;
  font-size: 1.1rem;
  color: #343a40;
}

.danger-action p {
  margin: 0;
  color: #6c757d;
  font-size: 14px;
}

/* Modal di conferma */
.confirm-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background-color: white;
  padding: 20px;
  border-radius: 8px;
  max-width: 500px;
  width: 90%;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
}

.modal-content h3 {
  margin-top: 0;
  color: #343a40;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}
</style>