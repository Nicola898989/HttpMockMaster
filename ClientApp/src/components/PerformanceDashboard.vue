<template>
  <div class="performance-dashboard">
    <div class="dashboard-header">
      <h2>Dashboard Performance</h2>
      
      <div class="control-panel">
        <div class="time-controls">
          <span class="control-label">Intervallo di tempo:</span>
          <div class="time-buttons">
            <button 
              v-for="option in timeOptions" 
              :key="option.value" 
              :class="['time-button', { active: timeFrame === option.value }]"
              @click="setTimeFrame(option.value)"
            >
              {{ option.label }}
            </button>
          </div>
        </div>
        
        <div class="refresh-controls">
          <button class="refresh-button" @click="refreshData">
            <i class="refresh-icon"></i>
            Aggiorna
          </button>
          
          <div class="auto-refresh">
            <input 
              type="checkbox" 
              id="auto-refresh" 
              v-model="autoRefresh"
              @change="toggleAutoRefresh"
            >
            <label for="auto-refresh">Auto-refresh (30s)</label>
          </div>
        </div>
      </div>
    </div>
    
    <div v-if="loading" class="loading-overlay">
      <div class="spinner"></div>
      <div class="loading-text">Caricamento dati in corso...</div>
    </div>
    
    <div v-else-if="error" class="error-message">
      <div class="error-icon">⚠️</div>
      <div class="error-text">
        <h4>Errore durante il caricamento dei dati</h4>
        <p>{{ error }}</p>
        <button class="retry-button" @click="refreshData">Riprova</button>
      </div>
    </div>
    
    <div v-else class="dashboard-content">
      <div class="metrics-summary">
        <div class="metric-card">
          <div class="metric-title">Totale Richieste</div>
          <div class="metric-value">{{ performanceMetrics.totalRequests }}</div>
        </div>
        
        <div class="metric-card">
          <div class="metric-title">Tempo Medio Risposta</div>
          <div class="metric-value">{{ formatResponseTime(performanceMetrics.responseTimeMetrics?.avg) }}</div>
        </div>
        
        <div class="metric-card">
          <div class="metric-title">Dim. Media Risposta</div>
          <div class="metric-value">{{ formatBytes(performanceMetrics.requestSizeMetrics?.response?.avg) }}</div>
        </div>
        
        <div class="metric-card">
          <div class="metric-title">Tasso di Successo</div>
          <div class="metric-value">{{ calculateSuccessRate() }}%</div>
        </div>
      </div>
      
      <div class="charts-grid">
        <div class="chart-container chart-lg">
          <ResponseTimeChart 
            :timeSeriesData="timeSeriesData.data" 
            title="Tempo di Risposta nel Tempo"
          />
        </div>
        
        <div class="chart-container chart-lg">
          <RequestVolumeChart 
            :timeSeriesData="timeSeriesData.data" 
            title="Volume di Richieste nel Tempo"
          />
        </div>
        
        <div class="chart-container">
          <StatusCodeChart 
            :statusCodeData="performanceMetrics.statusCodeMetrics" 
            title="Distribuzione Codici HTTP"
          />
        </div>
        
        <div class="chart-container">
          <MethodDistributionChart 
            :methodData="performanceMetrics.methodMetrics" 
            title="Distribuzione Metodi HTTP"
          />
        </div>
        
        <div class="chart-container">
          <ResponseSizeChart 
            :sizeData="performanceMetrics.requestSizeMetrics" 
            title="Dimensioni Medie Dati"
            :showAverages="true"
          />
        </div>
        
        <div class="chart-container">
          <SuccessRateChart 
            :timeSeriesData="timeSeriesData.data" 
            title="Tasso di Successo nel Tempo"
          />
        </div>
      </div>
      
      <div class="response-time-details">
        <h3>Dettagli Tempi di Risposta</h3>
        
        <div class="details-grid">
          <div class="detail-item">
            <div class="detail-label">Medio</div>
            <div class="detail-value">{{ formatResponseTime(performanceMetrics.responseTimeMetrics?.avg) }}</div>
          </div>
          
          <div class="detail-item">
            <div class="detail-label">Minimo</div>
            <div class="detail-value">{{ formatResponseTime(performanceMetrics.responseTimeMetrics?.min) }}</div>
          </div>
          
          <div class="detail-item">
            <div class="detail-label">Massimo</div>
            <div class="detail-value">{{ formatResponseTime(performanceMetrics.responseTimeMetrics?.max) }}</div>
          </div>
          
          <div class="detail-item">
            <div class="detail-label">Mediana</div>
            <div class="detail-value">{{ formatResponseTime(performanceMetrics.responseTimeMetrics?.median) }}</div>
          </div>
          
          <div class="detail-item">
            <div class="detail-label">Percentile 95</div>
            <div class="detail-value">{{ formatResponseTime(performanceMetrics.responseTimeMetrics?.p95) }}</div>
          </div>
          
          <div class="detail-item">
            <div class="detail-label">Percentile 99</div>
            <div class="detail-value">{{ formatResponseTime(performanceMetrics.responseTimeMetrics?.p99) }}</div>
          </div>
        </div>
      </div>
      
      <div class="method-performance">
        <h3>Performance per Metodo HTTP</h3>
        
        <table class="method-table">
          <thead>
            <tr>
              <th>Metodo</th>
              <th>Conteggio</th>
              <th>Tempo Medio (ms)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="method in performanceMetrics.methodMetrics" :key="method.method">
              <td>
                <span :class="['method-badge', `method-${method.method.toLowerCase()}`]">
                  {{ method.method }}
                </span>
              </td>
              <td>{{ method.count }}</td>
              <td>{{ formatResponseTime(method.avgResponseTime) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script>
import { performanceService } from '../services/performanceService';
import ResponseTimeChart from './charts/ResponseTimeChart.vue';
import RequestVolumeChart from './charts/RequestVolumeChart.vue';
import StatusCodeChart from './charts/StatusCodeChart.vue';
import MethodDistributionChart from './charts/MethodDistributionChart.vue';
import ResponseSizeChart from './charts/ResponseSizeChart.vue';
import SuccessRateChart from './charts/SuccessRateChart.vue';

export default {
  name: 'PerformanceDashboard',
  components: {
    ResponseTimeChart,
    RequestVolumeChart,
    StatusCodeChart,
    MethodDistributionChart,
    ResponseSizeChart,
    SuccessRateChart
  },
  data() {
    return {
      timeFrame: 'day',
      timeOptions: [
        { value: 'hour', label: '1h' },
        { value: 'day', label: '24h' },
        { value: 'week', label: '7g' },
        { value: 'month', label: '30g' }
      ],
      performanceMetrics: {
        totalRequests: 0,
        responseTimeMetrics: {},
        requestSizeMetrics: { request: {}, response: {} },
        methodMetrics: [],
        statusCodeMetrics: []
      },
      timeSeriesData: {
        data: []
      },
      loading: true,
      error: null,
      autoRefresh: false,
      refreshInterval: null
    };
  },
  mounted() {
    this.fetchData();
  },
  beforeUnmount() {
    this.clearRefreshInterval();
  },
  methods: {
    async fetchData() {
      this.loading = true;
      this.error = null;
      
      try {
        // Carica i dati delle metriche aggregate
        const metricsPromise = performanceService.getPerformanceMetrics({
          timeFrame: this.timeFrame
        });
        
        // Carica i dati delle serie temporali
        const timeSeriesPromise = performanceService.getTimeSeriesData({
          timeFrame: this.timeFrame,
          groupBy: this.getGroupByBasedOnTimeFrame()
        });
        
        // Attendi il completamento di entrambe le chiamate
        const [metricsData, timeSeriesData] = await Promise.all([
          metricsPromise,
          timeSeriesPromise
        ]);
        
        this.performanceMetrics = metricsData;
        this.timeSeriesData = timeSeriesData;
      } catch (error) {
        console.error('Errore durante il recupero dei dati delle performance:', error);
        this.error = error.message || 'Errore durante il caricamento dei dati';
      } finally {
        this.loading = false;
      }
    },
    
    refreshData() {
      this.fetchData();
    },
    
    setTimeFrame(timeFrame) {
      this.timeFrame = timeFrame;
      this.fetchData();
    },
    
    toggleAutoRefresh() {
      if (this.autoRefresh) {
        this.refreshInterval = setInterval(() => {
          this.refreshData();
        }, 30000); // Aggiorna ogni 30 secondi
      } else {
        this.clearRefreshInterval();
      }
    },
    
    clearRefreshInterval() {
      if (this.refreshInterval) {
        clearInterval(this.refreshInterval);
        this.refreshInterval = null;
      }
    },
    
    getGroupByBasedOnTimeFrame() {
      // Determina il raggruppamento appropriato in base all'intervallo di tempo
      switch (this.timeFrame) {
        case 'hour':
          return 'minute';
        case 'day':
          return 'hour';
        case 'week':
        case 'month':
          return 'day';
        default:
          return 'hour';
      }
    },
    
    formatResponseTime(ms) {
      if (ms === undefined || ms === null) return '-';
      
      if (ms < 1) {
        return '< 1 ms';
      } else if (ms < 1000) {
        return `${Math.round(ms)} ms`;
      } else {
        return `${(ms / 1000).toFixed(2)} s`;
      }
    },
    
    formatBytes(bytes, decimals = 2) {
      if (bytes === undefined || bytes === null) return '-';
      if (bytes === 0) return '0 Bytes';
      
      const k = 1024;
      const dm = decimals < 0 ? 0 : decimals;
      const sizes = ['Bytes', 'KB', 'MB', 'GB'];
      
      const i = Math.floor(Math.log(bytes) / Math.log(k));
      
      return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
    },
    
    calculateSuccessRate() {
      // Calcola il tasso di successo dai dati dei codici di stato
      if (!this.performanceMetrics.statusCodeMetrics) return '-';
      
      const successData = this.performanceMetrics.statusCodeMetrics.find(item => 
        item.statusClass === '2xx' || item.statusClass === '3xx'
      );
      
      if (successData) {
        return Math.round(successData.percentage);
      }
      
      // Calcola il tasso dalle serie temporali se disponibili
      if (this.timeSeriesData.data && this.timeSeriesData.data.length > 0) {
        const avgSuccessRate = this.timeSeriesData.data.reduce((acc, point) => 
          acc + point.successRate, 0
        ) / this.timeSeriesData.data.length;
        
        return Math.round(avgSuccessRate);
      }
      
      return '-';
    }
  }
};
</script>

<style scoped>
.performance-dashboard {
  padding: 20px;
  background-color: #f8f9fa;
  min-height: calc(100vh - 60px);
}

.dashboard-header {
  margin-bottom: 20px;
}

.dashboard-header h2 {
  margin-top: 0;
  margin-bottom: 15px;
  font-size: 1.8rem;
  color: #343a40;
}

.control-panel {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding: 15px;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.05);
}

.time-controls {
  display: flex;
  align-items: center;
}

.control-label {
  margin-right: 10px;
  font-weight: 500;
  color: #495057;
}

.time-buttons {
  display: flex;
  gap: 5px;
}

.time-button {
  padding: 6px 12px;
  border: 1px solid #ced4da;
  background-color: white;
  color: #495057;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all 0.2s;
}

.time-button:hover {
  background-color: #f8f9fa;
}

.time-button.active {
  background-color: #e9ecef;
  border-color: #adb5bd;
  font-weight: 500;
}

.refresh-controls {
  display: flex;
  align-items: center;
  gap: 15px;
}

.refresh-button {
  display: flex;
  align-items: center;
  padding: 6px 12px;
  background-color: #f8f9fa;
  border: 1px solid #ced4da;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.refresh-button:hover {
  background-color: #e9ecef;
}

.refresh-icon {
  display: inline-block;
  width: 16px;
  height: 16px;
  margin-right: 5px;
  border: 2px solid #6c757d;
  border-radius: 50%;
  border-top-color: transparent;
  vertical-align: -3px;
}

.refresh-button:hover .refresh-icon {
  animation: spin 1s linear infinite;
}

.auto-refresh {
  display: flex;
  align-items: center;
}

.auto-refresh input {
  margin-right: 5px;
}

.loading-overlay {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 40px;
  background-color: rgba(255, 255, 255, 0.8);
  border-radius: 8px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.05);
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid rgba(0, 0, 0, 0.1);
  border-radius: 50%;
  border-top-color: #007bff;
  animation: spin 1s linear infinite;
  margin-bottom: 15px;
}

.loading-text {
  font-size: 1.1rem;
  color: #495057;
}

.error-message {
  display: flex;
  padding: 30px;
  background-color: #fff5f5;
  border-radius: 8px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.05);
  margin-bottom: 20px;
}

.error-icon {
  margin-right: 20px;
  font-size: 2rem;
}

.error-text h4 {
  margin-top: 0;
  margin-bottom: 10px;
  color: #e03131;
}

.retry-button {
  margin-top: 15px;
  padding: 8px 16px;
  background-color: #f8f9fa;
  border: 1px solid #ced4da;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.retry-button:hover {
  background-color: #e9ecef;
}

.metrics-summary {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 15px;
  margin-bottom: 20px;
}

.metric-card {
  background-color: white;
  border-radius: 8px;
  padding: 15px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.05);
  text-align: center;
}

.metric-title {
  font-size: 0.9rem;
  color: #6c757d;
  margin-bottom: 10px;
}

.metric-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: #343a40;
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px;
  margin-bottom: 20px;
}

.chart-container {
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.05);
}

.chart-lg {
  grid-column: span 2;
}

.response-time-details,
.method-performance {
  background-color: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.05);
  margin-bottom: 20px;
}

.response-time-details h3,
.method-performance h3 {
  margin-top: 0;
  margin-bottom: 15px;
  font-size: 1.2rem;
  color: #343a40;
}

.details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 15px;
}

.detail-item {
  padding: 10px;
  background-color: #f8f9fa;
  border-radius: 6px;
  text-align: center;
}

.detail-label {
  font-size: 0.9rem;
  color: #6c757d;
  margin-bottom: 5px;
}

.detail-value {
  font-size: 1.1rem;
  font-weight: 500;
  color: #343a40;
}

.method-table {
  width: 100%;
  border-collapse: collapse;
}

.method-table th {
  text-align: left;
  padding: 10px;
  border-bottom: 2px solid #e9ecef;
  font-weight: 500;
  color: #495057;
}

.method-table td {
  padding: 10px;
  border-bottom: 1px solid #e9ecef;
}

.method-badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 4px;
  font-weight: 600;
  font-size: 0.85rem;
}

.method-get {
  background-color: #4BC0C0;
  color: white;
}

.method-post {
  background-color: #FF6384;
  color: white;
}

.method-put {
  background-color: #FFCD56;
  color: #343a40;
}

.method-delete {
  background-color: #FF9F40;
  color: white;
}

.method-patch {
  background-color: #36A2EB;
  color: white;
}

.method-head {
  background-color: #9966FF;
  color: white;
}

.method-options {
  background-color: #C9CBCF;
  color: #343a40;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@media (max-width: 768px) {
  .control-panel {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .time-controls {
    margin-bottom: 15px;
  }
  
  .charts-grid {
    grid-template-columns: 1fr;
  }
  
  .chart-lg {
    grid-column: 1;
  }
}
</style>