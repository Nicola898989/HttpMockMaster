<template>
  <div class="chart-wrapper">
    <BaseChart
      type="bar"
      :chartData="chartData"
      :options="options"
      :height="350"
      cssClasses="response-size-chart"
    />
  </div>
</template>

<script>
import BaseChart from './BaseChart.js';

export default {
  name: 'ResponseSizeChart',
  components: {
    BaseChart
  },
  props: {
    // Dati delle dimensioni di richiesta/risposta
    sizeData: {
      type: Object,
      required: true
    },
    
    // Dati aggiuntivi di metodi HTTP per raggruppare le dimensioni
    methodData: {
      type: Array,
      default: () => []
    },
    
    // Titolo del grafico
    title: {
      type: String,
      default: 'Dimensione Media dati'
    },
    
    // Mostrare valori assoluti o medi
    showAverages: {
      type: Boolean,
      default: true
    }
  },
  computed: {
    chartData() {
      if (this.methodData.length > 0) {
        // Visualizza le dimensioni medie per metodo HTTP
        return {
          labels: this.methodData.map(item => item.method),
          datasets: [
            {
              label: 'Dimensione Media Richiesta (bytes)',
              data: this.methodData.map(() => Math.floor(this.sizeData.request.avg)),
              backgroundColor: '#4BC0C0',
              borderWidth: 0,
              borderRadius: 4,
              barPercentage: 0.6
            },
            {
              label: 'Dimensione Media Risposta (bytes)',
              data: this.methodData.map(() => Math.floor(this.sizeData.response.avg)),
              backgroundColor: '#FF6384',
              borderWidth: 0,
              borderRadius: 4,
              barPercentage: 0.6
            }
          ]
        };
      } else {
        // Visualizza le dimensioni totali o medie
        const valueProperty = this.showAverages ? 'avg' : 'total';
        
        return {
          labels: ['Richiesta', 'Risposta'],
          datasets: [
            {
              label: this.showAverages ? 'Dimensione Media (bytes)' : 'Dimensione Totale (bytes)',
              data: [
                Math.floor(this.sizeData.request[valueProperty]),
                Math.floor(this.sizeData.response[valueProperty])
              ],
              backgroundColor: ['#4BC0C0', '#FF6384'],
              borderWidth: 0,
              borderRadius: 4,
              barPercentage: 0.5
            }
          ]
        };
      }
    },
    options() {
      return {
        responsive: true,
        maintainAspectRatio: false,
        indexAxis: this.methodData.length > 0 ? 'x' : 'y',
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
          tooltip: {
            callbacks: {
              label: function(context) {
                const value = context.parsed.y || context.parsed.x || 0;
                return `${context.dataset.label}: ${this.formatBytes(value)}`;
              }.bind(this)
            }
          },
          legend: {
            position: 'top',
            labels: {
              boxWidth: 12,
              usePointStyle: true
            }
          }
        },
        scales: {
          x: {
            grid: {
              display: false
            },
            stacked: false
          },
          y: {
            title: {
              display: true,
              text: 'Bytes'
            },
            beginAtZero: true,
            grid: {
              color: '#f0f0f0'
            },
            ticks: {
              callback: function(value) {
                return this.formatBytes(value, 0);
              }.bind(this)
            }
          }
        }
      };
    }
  },
  methods: {
    formatBytes(bytes, decimals = 2) {
      if (bytes === 0) return '0 Bytes';
      
      const k = 1024;
      const dm = decimals < 0 ? 0 : decimals;
      const sizes = ['Bytes', 'KB', 'MB', 'GB'];
      
      const i = Math.floor(Math.log(bytes) / Math.log(k));
      
      return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
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