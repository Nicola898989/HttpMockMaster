import axios from 'axios';

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
      // Costruisci i parametri di query
      const params = this._buildFilterParams(filters);
      
      // Effettua la richiesta di esportazione
      const response = await axios.get('/api/export/json', { 
        params,
        responseType: 'blob'
      });
      
      return response.data;
    } catch (error) {
      console.error('Errore durante l\'esportazione come JSON:', error);
      throw error;
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
      // Costruisci i parametri di query
      const params = this._buildFilterParams(filters);
      
      // Effettua la richiesta di esportazione
      const response = await axios.get('/api/export/csv', { 
        params,
        responseType: 'blob'
      });
      
      return response.data;
    } catch (error) {
      console.error('Errore durante l\'esportazione come CSV:', error);
      throw error;
    }
  },
  
  /**
   * Esporta i dettagli di una specifica richiesta
   * @param {number} requestId - ID della richiesta da esportare
   * @returns {Promise<Blob>} - Dettagli della richiesta come blob JSON
   */
  async exportRequestDetails(requestId) {
    if (!requestId) {
      throw new Error('ID richiesta non valido');
    }
    
    try {
      const response = await axios.get(`/api/export/request/${requestId}`, {
        responseType: 'blob'
      });
      
      return response.data;
    } catch (error) {
      console.error(`Errore durante l'esportazione dei dettagli della richiesta ${requestId}:`, error);
      throw error;
    }
  },
  
  /**
   * Scarica il blob come file
   * @param {Blob} blob - Il blob di dati
   * @param {string} filename - Il nome del file da utilizzare
   */
  downloadFile(blob, filename) {
    if (!blob) {
      console.error('Blob non valido per il download');
      return;
    }
    
    // Crea un URL oggetto temporaneo per il blob
    const url = window.URL.createObjectURL(blob);
    
    // Crea un elemento link per il download
    const downloadLink = document.createElement('a');
    downloadLink.href = url;
    downloadLink.download = filename;
    
    // Aggiunge temporaneamente il link al document e simula il click
    document.body.appendChild(downloadLink);
    downloadLink.click();
    
    // Pulisce
    window.URL.revokeObjectURL(url);
    document.body.removeChild(downloadLink);
  },
  
  /**
   * Costruisce i parametri di filtro per le richieste di esportazione
   * @param {Object} filters - Parametri di filtro
   * @returns {Object} - Parametri di filtro elaborati
   * @private
   */
  _buildFilterParams(filters) {
    const params = {};
    
    if (filters.fromDate instanceof Date) {
      params.startDate = filters.fromDate.toISOString();
    }
    
    if (filters.toDate instanceof Date) {
      params.endDate = filters.toDate.toISOString();
    }
    
    if (filters.method) {
      params.method = filters.method;
    }
    
    if (filters.host) {
      params.host = filters.host;
    }
    
    return params;
  }
};