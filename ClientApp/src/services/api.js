import axios from 'axios'

/**
 * Configurazione del client API per le richieste HTTP
 * Centralizza la configurazione delle richieste e la gestione degli errori
 */
const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
})

/**
 * Intercettore di richieste per la gestione degli errori
 */
api.interceptors.request.use(
  config => {
    return config
  },
  error => {
    console.error('API Request Error:', error)
    return Promise.reject(error)
  }
)

/**
 * Intercettore di risposte per la gestione degli errori
 * Registra dettagli specifici sui diversi tipi di errore API
 */
api.interceptors.response.use(
  response => {
    return response
  },
  error => {
    if (error.response) {
      // La richiesta è stata effettuata e il server ha risposto con un codice di stato
      // che non rientra nell'intervallo 2xx
      console.error('API Error:', error.response.status, error.response.data)
    } else if (error.request) {
      // La richiesta è stata effettuata ma non è stata ricevuta alcuna risposta
      console.error('API No Response:', error.request)
    } else {
      // Si è verificato un errore durante l'impostazione della richiesta
      console.error('API Request Setup Error:', error.message)
    }
    
    return Promise.reject(error)
  }
)

export default api
