<template>
  <div class="comparison-container">
    <h2>Confronto Richieste</h2>
    
    <div class="selection-form">
      <div class="form-group">
        <label for="requestIds">Seleziona le richieste da confrontare:</label>
        <select v-model="selectedRequests" multiple class="form-control" id="requestIds">
          <option v-for="request in availableRequests" :key="request.id" :value="request.id">
            {{ request.method }} {{ request.url }} (ID: {{ request.id }})
          </option>
        </select>
        <small class="text-muted">Tieni premuto Ctrl o ⌘ per selezionare più richieste</small>
      </div>
      
      <button class="btn btn-primary" @click="compareRequests" :disabled="selectedRequests.length < 2">
        Confronta
      </button>
    </div>
    
    <div v-if="isLoading" class="text-center mt-4">
      <div class="spinner-border" role="status">
        <span class="sr-only">Caricamento...</span>
      </div>
    </div>

    <div v-if="error" class="alert alert-danger mt-4">
      {{ error }}
    </div>
    
    <div v-if="comparisonResult && !isLoading" class="comparison-result mt-4">
      <h3>Risultato del confronto</h3>
      
      <div class="similarity-info alert" :class="similarityClass">
        <strong>Similarità: {{ comparisonResult.similarityPercentage }}%</strong>
        <span v-if="comparisonResult.hasSignificantDifferences">
          (Sono state rilevate differenze significative)
        </span>
        <span v-else>
          (Le richieste sono praticamente identiche)
        </span>
      </div>
      
      <div class="differences-panel mt-3">
        <h4>Differenze rilevate:</h4>
        <div v-if="Object.keys(comparisonResult.differences).length === 0" class="alert alert-success">
          Nessuna differenza significativa rilevata.
        </div>
        <div v-else>
          <table class="table table-striped">
            <thead>
              <tr>
                <th style="width: 15%">Campo</th>
                <th>Differenze</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(diff, field) in comparisonResult.differences" :key="field">
                <td><strong>{{ formatFieldName(field) }}</strong></td>
                <td>
                  <!-- Per array di valori semplici -->
                  <div v-if="Array.isArray(diff) && !isObject(diff[0])">
                    <div v-for="(value, index) in diff" :key="index" class="diff-value">
                      <span class="badge bg-secondary">Richiesta {{ index + 1 }}</span>
                      <pre>{{ value === null ? 'Nessun valore' : value }}</pre>
                    </div>
                  </div>
                  
                  <!-- Per oggetti JSON -->
                  <div v-else-if="isObject(diff)">
                    <pre>{{ JSON.stringify(diff, null, 2) }}</pre>
                  </div>
                  
                  <!-- Fallback -->
                  <div v-else>
                    {{ diff }}
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
      
      <div class="requests-panel mt-4">
        <h4>Richieste confrontate:</h4>
        <div class="row">
          <div 
            v-for="(request, index) in comparisonResult.requests" 
            :key="request.id" 
            class="col-md-6 mb-3"
          >
            <div class="card">
              <div class="card-header">
                <strong>Richiesta {{ index + 1 }} (ID: {{ request.id }})</strong>
              </div>
              <div class="card-body">
                <p><strong>Metodo:</strong> {{ request.method }}</p>
                <p><strong>URL:</strong> {{ request.url }}</p>
                <p><strong>Timestamp:</strong> {{ formatDate(request.timestamp) }}</p>
                
                <h5 class="mt-3">Intestazioni:</h5>
                <pre>{{ request.headers || 'Nessuna intestazione' }}</pre>
                
                <h5 class="mt-3">Corpo:</h5>
                <pre>{{ request.body || 'Nessun corpo' }}</pre>
                
                <h5 class="mt-3">Risposta:</h5>
                <div v-if="request.response">
                  <p><strong>Stato:</strong> {{ request.response.statusCode }}</p>
                  <h6>Intestazioni:</h6>
                  <pre>{{ request.response.headers || 'Nessuna intestazione' }}</pre>
                  <h6>Corpo:</h6>
                  <pre>{{ formatResponseBody(request.response.body) }}</pre>
                </div>
                <p v-else class="text-muted">Nessuna risposta registrata</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Confronto di JSON personalizzato -->
    <div class="json-comparison mt-5">
      <h3>Confronto JSON personalizzato</h3>
      <div class="row">
        <div class="col-md-6">
          <div class="form-group">
            <label for="json1">Primo JSON:</label>
            <textarea 
              v-model="jsonInput.json1" 
              class="form-control" 
              id="json1" 
              rows="10"
              placeholder="{}"
            ></textarea>
          </div>
        </div>
        <div class="col-md-6">
          <div class="form-group">
            <label for="json2">Secondo JSON:</label>
            <textarea 
              v-model="jsonInput.json2" 
              class="form-control" 
              id="json2" 
              rows="10"
              placeholder="{}"
            ></textarea>
          </div>
        </div>
      </div>
      
      <div class="mt-3">
        <button class="btn btn-primary" @click="compareJson" :disabled="!jsonInput.json1 || !jsonInput.json2">
          Confronta JSON
        </button>
      </div>
      
      <div v-if="jsonDiffResult && !isJsonComparing" class="mt-4">
        <h4>Differenze JSON:</h4>
        <div v-if="Object.keys(jsonDiffResult).length === 0" class="alert alert-success">
          Nessuna differenza rilevata tra i due JSON.
        </div>
        <div v-else>
          <table class="table table-striped">
            <thead>
              <tr>
                <th style="width: 25%">Proprietà</th>
                <th>JSON 1</th>
                <th>JSON 2</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(diff, prop) in jsonDiffResult" :key="prop">
                <td><strong>{{ prop }}</strong></td>
                <td>
                  <pre v-if="Array.isArray(diff)">{{ JSON.stringify(diff[0], null, 2) }}</pre>
                  <pre v-else>{{ JSON.stringify(diff, null, 2) }}</pre>
                </td>
                <td>
                  <pre v-if="Array.isArray(diff)">{{ JSON.stringify(diff[1], null, 2) }}</pre>
                  <pre v-else>-</pre>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script>

export default {
  name: 'RequestComparison',
  data() {
    return {
      availableRequests: [],
      selectedRequests: [],
      comparisonResult: null,
      isLoading: false,
      error: null,
      jsonInput: {
        json1: '',
        json2: ''
      },
      jsonDiffResult: null,
      isJsonComparing: false
    };
  },
  computed: {
    similarityClass() {
      const percentage = this.comparisonResult ? this.comparisonResult.similarityPercentage : 0;
      if (percentage >= 90) return 'alert-success';
      if (percentage >= 70) return 'alert-warning';
      return 'alert-danger';
    }
  },
  async mounted() {
    try {
      // Ottenere l'elenco delle richieste recenti
      const response = await axios.get('/api/requests', {
        params: {
          limit: 50,
          sortBy: 'timestamp',
          sortDirection: 'desc'
        }
      });
      this.availableRequests = response.data;
    } catch (error) {
      console.error('Errore nel caricamento delle richieste:', error);
      this.error = 'Impossibile caricare le richieste recenti. Riprova più tardi.';
    }
  },
  methods: {
    async compareRequests() {
      if (this.selectedRequests.length < 2) {
        this.error = 'Seleziona almeno due richieste da confrontare';
        return;
      }
      
      this.isLoading = true;
      this.error = null;
      this.comparisonResult = null;
      
      try {
        const result = await this.$store.dispatch('comparison/compareRequests', this.selectedRequests);
        if (result) {
          this.comparisonResult = result;
        } else {
          this.error = this.$store.state.comparison.error;
        }
      } catch (error) {
        console.error('Errore durante il confronto delle richieste:', error);
        this.error = 'Si è verificato un errore durante il confronto';
      } finally {
        this.isLoading = false;
      }
    },
    
    async compareJson() {
      if (!this.jsonInput.json1 || !this.jsonInput.json2) {
        this.error = 'Inserisci entrambi i JSON da confrontare';
        return;
      }
      
      this.isJsonComparing = true;
      this.error = null;
      this.jsonDiffResult = null;
      
      try {
        const result = await this.$store.dispatch('comparison/compareJson', {
          json1: this.jsonInput.json1,
          json2: this.jsonInput.json2
        });
        
        if (result) {
          this.jsonDiffResult = result;
        } else {
          this.error = this.$store.state.comparison.error;
        }
      } catch (error) {
        console.error('Errore durante il confronto JSON:', error);
        this.error = 'Si è verificato un errore durante il confronto JSON';
      } finally {
        this.isJsonComparing = false;
      }
    },
    
    formatFieldName(field) {
      // Trasforma nomi di campo in formato leggibile
      const mapping = {
        'Url': 'URL',
        'Method': 'Metodo HTTP',
        'RequestHeaders': 'Intestazioni richiesta',
        'RequestBody': 'Corpo richiesta',
        'RequestBodyDetails': 'Dettagli corpo richiesta',
        'StatusCode': 'Codice di stato',
        'ResponseHeaders': 'Intestazioni risposta',
        'ResponseBody': 'Corpo risposta',
        'ResponseBodyDetails': 'Dettagli corpo risposta',
        'ResponseBodyJson': 'JSON risposta',
        'ResponseMissing': 'Risposta mancante',
        'ResponseTimestamp': 'Timestamp risposta'
      };
      
      return mapping[field] || field;
    },
    
    formatDate(timestamp) {
      if (!timestamp) return 'N/D';
      
      try {
        const date = new Date(timestamp);
        return date.toLocaleString('it-IT');
      } catch (e) {
        return timestamp;
      }
    },
    
    formatResponseBody(body) {
      if (!body) return 'Nessun corpo';
      
      // Prova a formattare il JSON
      try {
        const parsedBody = JSON.parse(body);
        return JSON.stringify(parsedBody, null, 2);
      } catch {
        // Se non è JSON, restituisci il corpo così com'è
        return body;
      }
    },
    
    isObject(value) {
      return value !== null && typeof value === 'object';
    }
  }
};
</script>

<style scoped>
.comparison-container {
  padding: 20px;
}

.diff-value {
  margin-bottom: 10px;
}

pre {
  background-color: #f8f9fa;
  padding: 10px;
  border-radius: 4px;
  max-height: 300px;
  overflow: auto;
  margin-top: 5px;
  white-space: pre-wrap;
}

.selection-form {
  max-width: 600px;
  margin: 0 auto;
}

.similarity-info {
  font-size: 1.1rem;
}
</style>
