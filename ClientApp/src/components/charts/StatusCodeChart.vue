<template>
  <div class="chart-wrapper">
    <BaseChart
      type="doughnut"
      :chartData="chartData"
      :options="options"
      :height="350"
      cssClasses="status-code-chart"
    />
  </div>
</template>

<script>
import BaseChart from './BaseChart.js';

export default {
  name: 'StatusCodeChart',
  components: {
    BaseChart
  },
  props: {
    // Dati di distribuzione dei codici di stato
    statusCodeData: {
      type: Object,
      required: true
    },
    
    // Titolo del grafico
    title: {
      type: String,
      default: 'Distribuzione Codici di Stato HTTP'
    },
    
    // Mostra le etichette sulle fette del grafico
    showLabels: {
      type: Boolean,
      default: true
    },
    
    // Visualizza percentuali invece dei valori assoluti
    showPercentages: {
      type: Boolean,
      default: true
    },
    
    // Raggruppa per categoria (2xx, 3xx, ecc.) invece di singoli codici
    groupByCategory: {
      type: Boolean,
      default: false
    },
    
    // Raggio del cerchio interno del grafico a ciambella
    cutoutPercentage: {
      type: Number,
      default: 65
    }
  },
  computed: {
    // Prepara i dati per il grafico a ciambella
    chartData() {
      if (!this.statusCodeData || Object.keys(this.statusCodeData).length === 0) {
        return {
          labels: ['Nessun dato'],
          datasets: [{
            data: [1],
            backgroundColor: ['#F5F5F5'],
            borderWidth: 0
          }]
        };
      }
      
      // Dati di base
      const statusData = { ...this.statusCodeData };
      
      // Se richiesto, raggruppa per categoria
      let processedData = {};
      if (this.groupByCategory) {
        // Raggruppa i codici per categoria (1xx, 2xx, 3xx, 4xx, 5xx)
        for (const code in statusData) {
          const category = this.getStatusCategory(parseInt(code, 10));
          if (!processedData[category]) {
            processedData[category] = 0;
          }
          processedData[category] += statusData[code];
        }
      } else {
        // Usa i singoli codici di stato
        processedData = statusData;
      }
      
      // Estrai etichette e valori
      const labels = Object.keys(processedData).map(code => 
        this.groupByCategory 
          ? `${code} - ${this.getCategoryName(code)}` 
          : `${code} ${this.getStatusName(parseInt(code, 10))}`
      );
      
      const data = Object.values(processedData);
      
      // Genera colori in base alle categorie
      const backgroundColors = Object.keys(processedData).map(code => 
        this.getStatusColor(this.groupByCategory ? code : parseInt(code, 10))
      );
      
      // Bordi più scuri per le fette
      const borderColors = backgroundColors.map(color => this.adjustBrightness(color, -30));
      
      return {
        labels,
        datasets: [{
          data,
          backgroundColor: backgroundColors,
          borderColor: borderColors,
          borderWidth: 1,
          hoverOffset: 10
        }]
      };
    },
    
    // Configurazione del grafico
    options() {
      const total = this.calculateTotal();
      
      return {
        responsive: true,
        maintainAspectRatio: false,
        cutout: `${this.cutoutPercentage}%`,
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
            position: 'right',
            labels: {
              padding: 15,
              usePointStyle: true,
              generateLabels: (chart) => {
                const datasets = chart.data.datasets;
                return chart.data.labels.map((label, i) => {
                  const meta = chart.getDatasetMeta(0);
                  const style = meta.controller.getStyle(i);
                  
                  return {
                    text: label,
                    fillStyle: style.backgroundColor,
                    strokeStyle: style.borderColor,
                    lineWidth: style.borderWidth,
                    pointStyle: 'circle',
                    hidden: !meta.data[i].active,
                    index: i
                  };
                });
              },
              formatter: (item, index) => {
                const value = this.chartData.datasets[0].data[index.dataIndex];
                const percentage = ((value / total) * 100).toFixed(1);
                
                // Se richiesto, mostra percentuali
                return this.showPercentages && total > 0
                  ? `${item.text} (${percentage}%)`
                  : `${item.text} (${value})`;
              }
            }
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const label = context.label || '';
                const value = context.raw;
                const percentage = total > 0
                  ? ((value / total) * 100).toFixed(1)
                  : 0;
                
                return this.showPercentages
                  ? `${label}: ${value} (${percentage}%)`
                  : `${label}: ${value}`;
              }
            }
          },
          datalabels: this.showLabels ? {
            formatter: (value, context) => {
              const percentage = total > 0
                ? ((value / total) * 100).toFixed(1)
                : 0;
              
              // Mostra solo percentuali significative
              if (percentage < 3) return '';
              
              return this.showPercentages
                ? `${percentage}%`
                : value;
            },
            color: '#fff',
            font: {
              weight: 'bold',
              size: 11
            },
            textAlign: 'center'
          } : false
        },
        layout: {
          padding: 10
        },
        animation: {
          animateRotate: true,
          animateScale: true
        }
      };
    }
  },
  methods: {
    // Calcola il totale delle richieste
    calculateTotal() {
      const statusData = this.statusCodeData || {};
      return Object.values(statusData).reduce((sum, count) => sum + count, 0);
    },
    
    // Ottiene la categoria del codice di stato (1xx, 2xx, ecc.)
    getStatusCategory(code) {
      if (code >= 100 && code < 200) return '1xx';
      if (code >= 200 && code < 300) return '2xx';
      if (code >= 300 && code < 400) return '3xx';
      if (code >= 400 && code < 500) return '4xx';
      if (code >= 500 && code < 600) return '5xx';
      return 'Altro';
    },
    
    // Ottiene il nome della categoria
    getCategoryName(category) {
      const categoryNames = {
        '1xx': 'Informational',
        '2xx': 'Success',
        '3xx': 'Redirection',
        '4xx': 'Client Error',
        '5xx': 'Server Error'
      };
      
      return categoryNames[category] || category;
    },
    
    // Ottiene il nome del codice di stato
    getStatusName(code) {
      const commonCodes = {
        200: 'OK',
        201: 'Created',
        204: 'No Content',
        301: 'Moved Permanently',
        302: 'Found',
        304: 'Not Modified',
        400: 'Bad Request',
        401: 'Unauthorized',
        403: 'Forbidden',
        404: 'Not Found',
        405: 'Method Not Allowed',
        408: 'Request Timeout',
        422: 'Unprocessable Entity',
        429: 'Too Many Requests',
        500: 'Internal Server Error',
        502: 'Bad Gateway',
        503: 'Service Unavailable',
        504: 'Gateway Timeout'
      };
      
      return commonCodes[code] || '';
    },
    
    // Ottiene il colore per il codice di stato
    getStatusColor(code) {
      // Se è già una categoria (1xx, 2xx, ecc.)
      if (typeof code === 'string') {
        const categoryColors = {
          '1xx': 'rgba(33, 150, 243, 0.7)',  // Blu
          '2xx': 'rgba(76, 175, 80, 0.7)',   // Verde
          '3xx': 'rgba(255, 152, 0, 0.7)',   // Arancione
          '4xx': 'rgba(244, 67, 54, 0.7)',   // Rosso
          '5xx': 'rgba(156, 39, 176, 0.7)'   // Viola
        };
        
        return categoryColors[code] || 'rgba(158, 158, 158, 0.7)';
      }
      
      // Altrimenti, assegna colori in base alla categoria del codice
      const category = this.getStatusCategory(code);
      
      if (category === '1xx') return 'rgba(33, 150, 243, 0.7)';  // Blu
      if (category === '2xx') return 'rgba(76, 175, 80, 0.7)';   // Verde
      if (category === '3xx') return 'rgba(255, 152, 0, 0.7)';   // Arancione
      if (category === '4xx') return 'rgba(244, 67, 54, 0.7)';   // Rosso
      if (category === '5xx') return 'rgba(156, 39, 176, 0.7)';  // Viola
      
      return 'rgba(158, 158, 158, 0.7)';  // Grigio per default
    },
    
    // Regola la luminosità di un colore
    adjustBrightness(color, percent) {
      if (!color) return '#9E9E9E';
      
      const num = parseInt(color.replace(/[^0-9a-f]/gi, ''), 16);
      const r = (num >> 16) + percent;
      const g = ((num >> 8) & 0x00FF) + percent;
      const b = (num & 0x0000FF) + percent;
      
      return 'rgba(' + 
        (r < 255 ? (r < 0 ? 0 : r) : 255) + ',' + 
        (g < 255 ? (g < 0 ? 0 : g) : 255) + ',' + 
        (b < 255 ? (b < 0 ? 0 : b) : 255) + ',' +
        (color.indexOf('rgba') >= 0 ? color.match(/,[0-9.]+\)$/)[0].slice(1) : '1)');
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