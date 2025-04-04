import axios from 'axios';

/**
 * URL di base per le API
 */
const BASE_URL = 'http://localhost:5000/api';

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
      
      const response = await axios({
        url: `${BASE_URL}/export/json`,
        method: 'GET',
        responseType: 'blob',
        params: params
      });
      
      this.downloadFile(response.data, `requests_export_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.json`);
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
      
      const response = await axios({
        url: `${BASE_URL}/export/csv`,
        method: 'GET',
        responseType: 'blob',
        params: params
      });
      
      this.downloadFile(response.data, `requests_export_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.csv`);
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
      const response = await axios({
        url: `${BASE_URL}/export/request/${requestId}`,
        method: 'GET',
        responseType: 'blob'
      });
      
      this.downloadFile(response.data, `request_details_${requestId}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.json`);
      return response.data;
    } catch (error) {
      console.error(`Errore durante l'esportazione dei dettagli della richiesta ${requestId}:`, error);
      throw new Error(this._getErrorMessage(error));
    }
  },
  
  /**
   * Scarica il blob come file
   * @param {Blob} blob - Il blob di dati
   * @param {string} filename - Il nome del file da utilizzare
   */
  downloadFile(blob, filename) {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    
    // Pulizia
    link.parentNode.removeChild(link);
    window.URL.revokeObjectURL(url);
  },
  
  /**
   * Costruisce i parametri di filtro per le richieste di esportazione
   * @param {Object} filters - Parametri di filtro
   * @returns {Object} - Parametri di filtro elaborati
   * @private
   */
  _buildFilterParams(filters) {
    const params = {};
    
    if (filters.fromDate) {
      params.fromDate = filters.fromDate instanceof Date 
        ? filters.fromDate.toISOString() 
        : filters.fromDate;
    }
    
    if (filters.toDate) {
      params.toDate = filters.toDate instanceof Date 
        ? filters.toDate.toISOString() 
        : filters.toDate;
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
      return `Errore ${error.response.status}: ${error.response.data || 'Si è verificato un errore durante l\'esportazione'}`;
    } else if (error.request) {
      return 'Impossibile raggiungere il server. Verifica la connessione.';
    } else {
      return error.message || 'Errore sconosciuto durante l\'esportazione';
    }
  }
};