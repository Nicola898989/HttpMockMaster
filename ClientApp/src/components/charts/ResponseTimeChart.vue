<template>
  <div class="chart-wrapper">
    <BaseChart
      type="line"
      :chartData="chartData"
      :options="options"
      :height="350"
      cssClasses="response-time-chart"
    />
  </div>
</template>

<script>
import BaseChart from './BaseChart.js';

export default {
  name: 'ResponseTimeChart',
  components: {
    BaseChart
  },
  props: {
    // Dati delle serie temporali
    timeSeriesData: {
      type: Array,
      required: true
    },
    
    // Titolo del grafico
    title: {
      type: String,
      default: 'Tempi di Risposta HTTP'
    },
    
    // Mostra il tempo medio
    showAverage: {
      type: Boolean,
      default: true
    },
    
    // Mostra i percentili (95°, 99°)
    showPercentiles: {
      type: Boolean,
      default: true
    },
    
    // Formato ora per le etichette dell'asse X
    timeFormat: {
      type: String,
      default: 'HH:mm',
      validator: (value) => ['HH:mm', 'DD/MM HH:mm', 'DD/MM/YYYY', 'MM/YYYY'].includes(value)
    }
  },
  computed: {
    // Prepara i dati per il grafico a linee
    chartData() {
      if (!this.timeSeriesData || this.timeSeriesData.length === 0) {
        return {
          labels: [],
          datasets: []
        };
      }
      
      // Estrai le etichette temporali come timestamps
      const labels = this.timeSeriesData.map(point => {
        const date = new Date(point.timestamp);
        return this.formatDateTime(date);
      });
      
      // Dati per tempo di risposta medio
      const avgResponseTimes = this.timeSeriesData.map(point => point.avgResponseTime || 0);
      
      // Dati per i percentili dei tempi di risposta
      const p95ResponseTimes = this.showPercentiles 
        ? this.timeSeriesData.map(point => point.p95ResponseTime || 0)
        : [];
        
      const p99ResponseTimes = this.showPercentiles 
        ? this.timeSeriesData.map(point => point.p99ResponseTime || 0)
        : [];
      
      // Prepara i dataset
      const datasets = [];
      
      // Tempo di risposta medio
      if (this.showAverage) {
        datasets.push({
          label: 'Tempo medio (ms)',
          data: avgResponseTimes,
          borderColor: '#2196F3',
          backgroundColor: 'rgba(33, 150, 243, 0.1)',
          borderWidth: 2,
          tension: 0.4,
          fill: true,
          pointRadius: 3,
          pointHoverRadius: 5
        });
      }
      
      // Percentile 95°
      if (this.showPercentiles) {
        datasets.push({
          label: 'Percentile 95° (ms)',
          data: p95ResponseTimes,
          borderColor: '#FF9800',
          backgroundColor: 'rgba(255, 152, 0, 0.1)',
          borderWidth: 2,
          tension: 0.4,
          fill: false,
          pointRadius: 2,
          pointHoverRadius: 4
        });
        
        // Percentile 99°
        datasets.push({
          label: 'Percentile 99° (ms)',
          data: p99ResponseTimes,
          borderColor: '#F44336',
          backgroundColor: 'rgba(244, 67, 54, 0.1)',
          borderWidth: 2,
          tension: 0.4,
          fill: false,
          pointRadius: 2,
          pointHoverRadius: 4,
          borderDash: [5, 5]
        });
      }
      
      return {
        labels,
        datasets
      };
    },
    
    // Configurazione del grafico
    options() {
      return {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          title: {
            display: true,
            text: this.title,
            font: {
              size: 16,
              weight: 'bold'
            },
            padding: {
              top: 10,
              bottom: 20
            }
          },
          legend: {
            position: 'top',
            align: 'end',
            labels: {
              usePointStyle: true,
              padding: 15
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            callbacks: {
              label: (context) => {
                const label = context.dataset.label || '';
                const value = context.raw !== null && context.raw !== undefined 
                  ? context.raw.toFixed(2) 
                  : 'N/A';
                return `${label}: ${value} ms`;
              }
            }
          }
        },
        hover: {
          mode: 'nearest',
          intersect: true
        },
        scales: {
          x: {
            title: {
              display: true,
              text: 'Orario'
            },
            grid: {
              display: false
            }
          },
          y: {
            title: {
              display: true,
              text: 'Tempo di risposta (ms)'
            },
            beginAtZero: true,
            ticks: {
              callback: (value) => `${value} ms`
            },
            grid: {
              color: 'rgba(0, 0, 0, 0.05)'
            }
          }
        },
        interaction: {
          mode: 'index',
          intersect: false
        },
        elements: {
          line: {
            tension: 0.4
          }
        }
      };
    }
  },
  methods: {
    // Formatta una data in base al formato specificato
    formatDateTime(date) {
      if (!date) return '';
      
      const day = date.getDate().toString().padStart(2, '0');
      const month = (date.getMonth() + 1).toString().padStart(2, '0');
      const year = date.getFullYear();
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      
      switch (this.timeFormat) {
        case 'HH:mm':
          return `${hours}:${minutes}`;
        case 'DD/MM HH:mm':
          return `${day}/${month} ${hours}:${minutes}`;
        case 'DD/MM/YYYY':
          return `${day}/${month}/${year}`;
        case 'MM/YYYY':
          return `${month}/${year}`;
        default:
          return `${day}/${month}/${year} ${hours}:${minutes}`;
      }
    }
  }
};
</script>

<style scoped>
.chart-wrapper {
  background-color: white;
  border-radius: 10px;
  padding: 15px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  margin-bottom: 20px;
}
</style>