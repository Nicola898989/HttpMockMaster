import axios from 'axios';

/**
 * URL di base per le API
 */
const API_BASE_URL = 'http://localhost:5000/api';

/**
 * Servizio per l'esportazione delle richieste HTTP in vari formati
 */
export const exportService = {
  /**
   * Esporta le richieste come JSON con filtri opzionali
   * @param {Object} filters - Parametri di filtro
   * @param {Date} [filters.fromDate] - Filtro per data iniziale
   * @param {Date} [filters.toDate] - Filtro per data finale
   * @param {string} [filters.method] - Filtro per metodo HTTP
   * @param {string} [filters.host] - Filtro per host
   * @returns {Promise<Blob>} - Dati JSON come blob
   */
  async exportAsJson(filters = {}) {
    try {
      const params = this._buildFilterParams(filters);
      const response = await axios.get(`${API_BASE_URL}/export/json`, {
        params,
        responseType: 'blob'
      });
      
      // Generiamo il nome del file
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      const filename = filters.host
        ? `export_${filters.host}_${timestamp}.json`
        : `export_${timestamp}.json`;
      
      // Scarichiamo il file
      this.downloadFile(response.data, filename);
      
      return response.data;
    } catch (error) {
      console.error('Errore durante l\'esportazione JSON:', error);
      throw new Error(this._getErrorMessage(error));
    }
  },
  
  /**
   * Esporta le richieste come CSV con filtri opzionali
   * @param {Object} filters - Parametri di filtro
   * @param {Date} [filters.fromDate] - Filtro per data iniziale
   * @param {Date} [filters.toDate] - Filtro per data finale
   * @param {string} [filters.method] - Filtro per metodo HTTP
   * @param {string} [filters.host] - Filtro per host
   * @returns {Promise<Blob>} - Dati CSV come blob
   */
  async exportAsCsv(filters = {}) {
    try {
      const params = this._buildFilterParams(filters);
      const response = await axios.get(`${API_BASE_URL}/export/csv`, {
        params,
        responseType: 'blob'
      });
      
      // Generiamo il nome del file
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      const filename = filters.host
        ? `export_${filters.host}_${timestamp}.csv`
        : `export_${timestamp}.csv`;
      
      // Scarichiamo il file
      this.downloadFile(response.data, filename);
      
      return response.data;
    } catch (error) {
      console.error('Errore durante l\'esportazione CSV:', error);
      throw new Error(this._getErrorMessage(error));
    }
  },
  
  /**
   * Esporta i dettagli di una specifica richiesta
   * @param {number} requestId - ID della richiesta da esportare
   * @returns {Promise<Blob>} - Dettagli della richiesta come blob JSON
   */
  async exportRequestDetails(requestId) {
    try {
      const response = await axios.get(`${API_BASE_URL}/export/request/${requestId}`, {
        responseType: 'blob'
      });
      
      // Generiamo il nome del file
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      const filename = `request_details_${requestId}_${timestamp}.json`;
      
      // Scarichiamo il file
      this.downloadFile(response.data, filename);
      
      return response.data;
    } catch (error) {
      console.error(`Errore durante l'esportazione della richiesta ${requestId}:`, error);
      throw new Error(this._getErrorMessage(error));
    }
  },
  
  /**
   * Scarica il blob come file
   * @param {Blob} blob - Il blob di dati
   * @param {string} filename - Il nome del file da utilizzare
   */
  downloadFile(blob, filename) {
    // Creiamo un URL oggetto per il blob
    const url = window.URL.createObjectURL(blob);
    
    // Creiamo un elemento anchor temporaneo per il download
    const a = document.createElement('a');
    a.style.display = 'none';
    a.href = url;
    a.download = filename;
    
    // Aggiungiamo l'elemento al DOM, facciamo click e poi lo rimuoviamo
    document.body.appendChild(a);
    a.click();
    
    // Cleanup
    window.setTimeout(() => {
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    }, 100);
  },
  
  /**
   * Costruisce i parametri di filtro per le richieste di esportazione
   * @param {Object} filters - Parametri di filtro
   * @returns {Object} - Parametri di filtro elaborati
   * @private
   */
  _buildFilterParams(filters) {
    const params = {};
    
    if (filters.fromDate instanceof Date && !isNaN(filters.fromDate)) {
      params.fromDate = filters.fromDate.toISOString();
    }
    
    if (filters.toDate instanceof Date && !isNaN(filters.toDate)) {
      params.toDate = filters.toDate.toISOString();
    }
    
    if (filters.method) {
      params.method = filters.method;
    }
    
    if (filters.host) {
      params.host = filters.host;
    }
    
    return params;
  },
  
  /**
   * Estrae il messaggio di errore dall'errore di Axios
   * @param {Error} error - L'errore da cui estrarre il messaggio
   * @returns {string} - Messaggio di errore
   * @private
   */
  _getErrorMessage(error) {
    if (error.response) {
      // La richiesta è stata fatta e il server ha risposto con uno stato di errore
      return `Errore dal server: ${error.response.status} - ${error.response.statusText}`;
    } else if (error.request) {
      // La richiesta è stata fatta ma non è stata ricevuta risposta
      return 'Errore di connessione: Nessuna risposta dal server';
    } else {
      // Qualcosa nel setup della richiesta ha triggerato un errore
      return `Errore: ${error.message}`;
    }
  }
};