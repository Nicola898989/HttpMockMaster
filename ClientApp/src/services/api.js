import axios from 'axios'

const api = axios.create({
  baseURL: 'http://localhost:8000/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
})

// Add a request interceptor to handle errors globally
api.interceptors.request.use(
  config => {
    return config
  },
  error => {
    console.error('API Request Error:', error)
    return Promise.reject(error)
  }
)

// Add a response interceptor to handle errors globally
api.interceptors.response.use(
  response => {
    return response
  },
  error => {
    if (error.response) {
      // The request was made and the server responded with a status code
      // that falls out of the range of 2xx
      console.error('API Error:', error.response.status, error.response.data)
    } else if (error.request) {
      // The request was made but no response was received
      console.error('API No Response:', error.request)
    } else {
      // Something happened in setting up the request that triggered an Error
      console.error('API Request Setup Error:', error.message)
    }
    
    return Promise.reject(error)
  }
)

export default api
