import axios from 'axios';

/**
 * URL di base per le API
 */
const BASE_URL = process.env.NODE_ENV === 'production' 
  ? 'http://localhost:5000/api' 
  : 'http://localhost:5000/api';

/**
 * Servizio per il monitoraggio delle performance
 */
export const performanceService = {
  /**
   * Ottiene le metriche di performance aggregate
   * @param {Object} options - Opzioni per filtrare i dati
   * @param {string} [options.timeFrame='day'] - Intervallo di tempo ('hour', 'day', 'week', 'month')
   * @param {Date} [options.startDate] - Data di inizio opzionale
   * @param {Date} [options.endDate] - Data di fine opzionale
   * @returns {Promise<Object>} - Dati delle metriche di performance
   */
  async getPerformanceMetrics(options = {}) {
    try {
      const params = this._buildQueryParams(options);
      
      const response = await axios.get(`${BASE_URL}/performance/metrics`, { params });
      return response.data;
    } catch (error) {
      console.error('Errore nel recupero delle metriche di performance:', error);
      throw new Error(this._getErrorMessage(error));
    }
  },

  /**
   * Ottiene i dati delle serie temporali per le performance
   * @param {Object} options - Opzioni per filtrare i dati
   * @param {string} [options.timeFrame='day'] - Intervallo di tempo ('hour', 'day', 'week', 'month')
   * @param {Date} [options.startDate] - Data di inizio opzionale
   * @param {Date} [options.endDate] - Data di fine opzionale
   * @param {string} [options.groupBy='hour'] - Come raggruppare i dati ('minute', 'hour', 'day')
   * @returns {Promise<Object>} - Dati delle serie temporali
   */
  async getTimeSeriesData(options = {}) {
    try {
      const params = this._buildQueryParams(options);

      if (options.groupBy) {
        params.groupBy = options.groupBy;
      }
      
      const response = await axios.get(`${BASE_URL}/performance/timeseries`, { params });
      return response.data;
    } catch (error) {
      console.error('Errore nel recupero dei dati delle serie temporali:', error);
      throw new Error(this._getErrorMessage(error));
    }
  },

  /**
   * Costruisce i parametri di query per le richieste API
   * @param {Object} options - Opzioni per filtrare i dati
   * @returns {Object} - Parametri di query
   * @private
   */
  _buildQueryParams(options) {
    const params = {};
    
    if (options.timeFrame) {
      params.timeFrame = options.timeFrame;
    }
    
    if (options.startDate instanceof Date) {
      params.startDate = options.startDate.toISOString();
    }
    
    if (options.endDate instanceof Date) {
      params.endDate = options.endDate.toISOString();
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
    if (error.response && error.response.data) {
      return typeof error.response.data === 'string' 
        ? error.response.data 
        : error.response.data.message || 'Errore nella richiesta al server';
    }
    return error.message || 'Errore di connessione al server';
  }
};