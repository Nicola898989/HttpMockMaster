<template>
  <div class="network-simulation-settings">
    <div class="settings-description">
      <p>
        Configura i parametri di simulazione di rete per testare il comportamento dell'applicazione 
        in condizioni di rete non ottimali. Queste impostazioni influenzano solo le richieste che 
        passano attraverso il proxy.
      </p>
    </div>

    <div class="settings-form">
      <div class="form-group">
        <label for="latencyMs">Latenza base (ms):</label>
        <input 
          type="number" 
          id="latencyMs" 
          v-model.number="settings.latencyMs"
          min="0"
          max="5000"
          class="form-control"
        />
        <small class="form-text text-muted">
          Il ritardo base da applicare ad ogni richiesta (in millisecondi).
        </small>
      </div>

      <div class="form-group">
        <label for="latencyVariationMs">Variazione latenza (ms):</label>
        <input 
          type="number" 
          id="latencyVariationMs" 
          v-model.number="settings.latencyVariationMs"
          min="0"
          max="5000"
          class="form-control"
        />
        <small class="form-text text-muted">
          La variazione casuale della latenza (in millisecondi). La latenza effettiva sarà un valore tra latenza base ± variazione.
        </small>
      </div>

      <div class="form-group">
        <label for="packetLossPercentage">Tasso di perdita pacchetti (%):</label>
        <input 
          type="number" 
          id="packetLossPercentage" 
          v-model.number="settings.packetLossPercentage"
          min="0"
          max="100"
          step="1"
          class="form-control"
        />
        <small class="form-text text-muted">
          Percentuale di richieste che verranno perse simulando problemi di rete.
        </small>
      </div>

      <div class="form-group">
        <label for="packetCorruptionPercentage">Tasso di corruzione (%):</label>
        <input 
          type="number" 
          id="packetCorruptionPercentage" 
          v-model.number="settings.packetCorruptionPercentage"
          min="0"
          max="100"
          step="1"
          class="form-control"
        />
        <small class="form-text text-muted">
          Percentuale di risposte che verranno corrotte simulando problemi di trasmissione dati.
        </small>
      </div>

      <div class="form-group">
        <label class="checkbox-label">
          <input type="checkbox" v-model="settings.simulationEnabled" />
          <span>Attiva simulazione di rete</span>
        </label>
        <small class="form-text text-muted">
          Attiva o disattiva la simulazione di rete. Quando disattivata, le richieste non subiranno ritardi o problemi simulati.
        </small>
      </div>

      <div class="button-group">
        <button @click="saveSettings" class="btn btn-primary">
          Salva Impostazioni
        </button>
        <button @click="resetSettings" class="btn btn-outline-secondary">
          Ripristina Predefiniti
        </button>
      </div>
    </div>

    <div v-if="showMessage" class="settings-message" :class="messageType">
      {{ message }}
    </div>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'NetworkSimulationSettings',
  data() {
    return {
      settings: {
        latencyMs: 0,
        latencyVariationMs: 0,
        packetLossPercentage: 0,
        packetCorruptionPercentage: 0,
        simulationEnabled: false
      },
      message: '',
      messageType: 'success',
      showMessage: false
    };
  },
  mounted() {
    this.loadSettings();
  },
  methods: {
    async loadSettings() {
      try {
        const response = await axios.get('/api/networksimulation');
        this.settings = response.data;
      } catch (error) {
        console.error('Errore durante il caricamento delle impostazioni di simulazione:', error);
        this.showErrorMessage('Impossibile caricare le impostazioni di simulazione');
      }
    },
    
    async saveSettings() {
      try {
        // Validazione dei dati
        if (this.settings.latencyMs < 0) this.settings.latencyMs = 0;
        if (this.settings.latencyVariationMs < 0) this.settings.latencyVariationMs = 0;
        if (this.settings.packetLossPercentage < 0) this.settings.packetLossPercentage = 0;
        if (this.settings.packetLossPercentage > 100) this.settings.packetLossPercentage = 100;
        if (this.settings.packetCorruptionPercentage < 0) this.settings.packetCorruptionPercentage = 0;
        if (this.settings.packetCorruptionPercentage > 100) this.settings.packetCorruptionPercentage = 100;
        
        // Invia i dati al server
        await axios.post('/api/networksimulation', this.settings);
        
        // Feedback positivo
        this.showSuccessMessage('Impostazioni di simulazione salvate con successo');
      } catch (error) {
        console.error('Errore durante il salvataggio delle impostazioni di simulazione:', error);
        this.showErrorMessage('Impossibile salvare le impostazioni di simulazione');
      }
    },
    
    async resetSettings() {
      try {
        await axios.post('/api/networksimulation/reset');
        await this.loadSettings();
        this.showSuccessMessage('Impostazioni di simulazione ripristinate');
      } catch (error) {
        console.error('Errore durante il ripristino delle impostazioni di simulazione:', error);
        this.showErrorMessage('Impossibile ripristinare le impostazioni predefinite');
      }
    },
    
    showSuccessMessage(message) {
      this.message = message;
      this.messageType = 'success';
      this.showMessage = true;
      setTimeout(() => {
        this.showMessage = false;
      }, 3000);
    },
    
    showErrorMessage(message) {
      this.message = message;
      this.messageType = 'error';
      this.showMessage = true;
      setTimeout(() => {
        this.showMessage = false;
      }, 5000);
    }
  }
};
</script>

<style scoped>
.network-simulation-settings {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.settings-description {
  margin-bottom: 10px;
}

.settings-description p {
  margin: 0;
  color: #6c757d;
  font-size: 14px;
  line-height: 1.5;
}

.settings-form {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.form-group {
  margin-bottom: 10px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

.checkbox-label {
  display: flex;
  align-items: center;
  cursor: pointer;
}

.checkbox-label input {
  margin-right: 8px;
}

.checkbox-label span {
  font-weight: 500;
}

.form-control {
  width: 100%;
  max-width: 200px;
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

.button-group {
  display: flex;
  gap: 10px;
  margin-top: 10px;
}

.btn {
  padding: 8px 16px;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-primary:hover {
  background-color: #0069d9;
}

.btn-outline-secondary {
  background-color: transparent;
  color: #6c757d;
  border: 1px solid #6c757d;
}

.btn-outline-secondary:hover {
  background-color: #6c757d;
  color: white;
}

.settings-message {
  padding: 10px 15px;
  border-radius: 4px;
  margin-top: 15px;
  font-size: 14px;
}

.settings-message.success {
  background-color: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.settings-message.error {
  background-color: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}
</style>