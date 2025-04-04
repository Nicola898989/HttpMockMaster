<template>
  <div class="chart-wrapper">
    <BaseChart
      type="bar"
      :chartData="chartData"
      :options="options"
      :height="350"
      cssClasses="request-volume-chart"
    />
  </div>
</template>

<script>
import BaseChart from './BaseChart.js';

export default {
  name: 'RequestVolumeChart',
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
      default: 'Volume Richieste HTTP'
    },
    
    // Tipo di raggruppamento
    groupBy: {
      type: String,
      default: 'total',
      validator: (value) => ['total', 'method', 'status'].includes(value)
    },
    
    // Mostra etichette sopra le barre
    showDataLabels: {
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
    // Prepara i dati per il grafico a barre
    chartData() {
      if (!this.timeSeriesData || this.timeSeriesData.length === 0) {
        return {
          labels: [],
          datasets: []
        };
      }
      
      // Estrai le etichette temporali
      const labels = this.timeSeriesData.map(point => {
        const date = new Date(point.timestamp);
        return this.formatDateTime(date);
      });
      
      // Raggruppa i dati in base al tipo di raggruppamento selezionato
      if (this.groupBy === 'method') {
        return this.prepareMethodBasedData(labels);
      } else if (this.groupBy === 'status') {
        return this.prepareStatusBasedData(labels);
      } else {
        return this.prepareTotalVolumeData(labels);
      }
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
            position: this.groupBy === 'total' ? 'top' : 'right',
            align: 'center',
            labels: {
              usePointStyle: true,
              padding: 15
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false
          },
          datalabels: this.showDataLabels ? {
            display: (context) => context.dataset.data[context.dataIndex] > 0,
            color: '#fff',
            font: {
              weight: 'bold'
            },
            backgroundColor: (context) => context.dataset.backgroundColor,
            borderRadius: 3,
            padding: 4
          } : false
        },
        scales: {
          x: {
            title: {
              display: true,
              text: 'Orario'
            },
            grid: {
              display: false
            },
            stacked: this.groupBy !== 'total'
          },
          y: {
            title: {
              display: true,
              text: 'Numero di richieste'
            },
            beginAtZero: true,
            ticks: {
              precision: 0,
              stepSize: this.calculateYAxisStepSize()
            },
            grid: {
              color: 'rgba(0, 0, 0, 0.05)'
            },
            stacked: this.groupBy !== 'total'
          }
        },
        interaction: {
          mode: 'index',
          intersect: false
        }
      };
    }
  },
  methods: {
    // Prepara i dati per la visualizzazione del volume totale
    prepareTotalVolumeData(labels) {
      const data = this.timeSeriesData.map(point => point.requestCount || 0);
      
      return {
        labels,
        datasets: [{
          label: 'Totale Richieste',
          data,
          backgroundColor: 'rgba(54, 162, 235, 0.7)',
          borderColor: 'rgba(54, 162, 235, 1)',
          borderWidth: 1,
          borderRadius: 5,
          maxBarThickness: 50
        }]
      };
    },
    
    // Prepara i dati raggruppati per metodo HTTP
    prepareMethodBasedData(labels) {
      // Definizione dei metodi HTTP comuni
      const methods = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'OPTIONS', 'HEAD'];
      const methodColors = {
        'GET': 'rgba(54, 162, 235, 0.7)',
        'POST': 'rgba(75, 192, 192, 0.7)',
        'PUT': 'rgba(255, 159, 64, 0.7)',
        'DELETE': 'rgba(255, 99, 132, 0.7)',
        'PATCH': 'rgba(153, 102, 255, 0.7)',
        'OPTIONS': 'rgba(201, 203, 207, 0.7)',
        'HEAD': 'rgba(255, 205, 86, 0.7)',
        'OTHER': 'rgba(100, 100, 100, 0.7)'
      };
      
      // Inizializza i dataset con valori zero
      const datasets = methods.map(method => ({
        label: method,
        data: new Array(labels.length).fill(0),
        backgroundColor: methodColors[method],
        borderColor: this.adjustBrightness(methodColors[method], -30),
        borderWidth: 1,
        borderRadius: 4,
        maxBarThickness: 30
      }));
      
      // Aggiungi un dataset per metodi "altro"
      datasets.push({
        label: 'OTHER',
        data: new Array(labels.length).fill(0),
        backgroundColor: methodColors['OTHER'],
        borderColor: this.adjustBrightness(methodColors['OTHER'], -30),
        borderWidth: 1,
        borderRadius: 4,
        maxBarThickness: 30
      });
      
      // Popola i dataset con i dati reali
      this.timeSeriesData.forEach((point, index) => {
        if (point.methodStats) {
          Object.entries(point.methodStats).forEach(([method, count]) => {
            // Trova il dataset appropriato
            const methodIndex = methods.indexOf(method);
            if (methodIndex >= 0) {
              datasets[methodIndex].data[index] = count;
            } else {
              // Aggiungi al dataset "altro"
              datasets[datasets.length - 1].data[index] += count;
            }
          });
        }
      });
      
      // Rimuovi i dataset vuoti (tutti zeri)
      const filteredDatasets = datasets.filter(ds => 
        ds.data.some(value => value > 0)
      );
      
      return {
        labels,
        datasets: filteredDatasets
      };
    },
    
    // Prepara i dati raggruppati per status code
    prepareStatusBasedData(labels) {
      // Definizione delle categorie di codici di stato
      const statusCategories = ['1xx', '2xx', '3xx', '4xx', '5xx'];
      const statusColors = {
        '1xx': 'rgba(33, 150, 243, 0.7)', // Blu
        '2xx': 'rgba(76, 175, 80, 0.7)',  // Verde
        '3xx': 'rgba(255, 152, 0, 0.7)',  // Arancione
        '4xx': 'rgba(244, 67, 54, 0.7)',  // Rosso
        '5xx': 'rgba(156, 39, 176, 0.7)'  // Viola
      };
      
      // Inizializza i dataset con valori zero
      const datasets = statusCategories.map(category => ({
        label: `${category} - ${this.getStatusCategoryName(category)}`,
        data: new Array(labels.length).fill(0),
        backgroundColor: statusColors[category],
        borderColor: this.adjustBrightness(statusColors[category], -30),
        borderWidth: 1,
        borderRadius: 4,
        maxBarThickness: 30
      }));
      
      // Popola i dataset con i dati reali
      this.timeSeriesData.forEach((point, index) => {
        if (point.statusCodeStats) {
          Object.entries(point.statusCodeStats).forEach(([statusCode, count]) => {
            const code = parseInt(statusCode, 10);
            
            // Assegna il conteggio alla categoria appropriata
            if (code >= 100 && code < 200) {
              datasets[0].data[index] += count;
            } else if (code >= 200 && code < 300) {
              datasets[1].data[index] += count;
            } else if (code >= 300 && code < 400) {
              datasets[2].data[index] += count;
            } else if (code >= 400 && code < 500) {
              datasets[3].data[index] += count;
            } else if (code >= 500 && code < 600) {
              datasets[4].data[index] += count;
            }
          });
        }
      });
      
      // Rimuovi i dataset vuoti (tutti zeri)
      const filteredDatasets = datasets.filter(ds => 
        ds.data.some(value => value > 0)
      );
      
      return {
        labels,
        datasets: filteredDatasets
      };
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
    },
    
    // Restituisce il nome della categoria di codici di stato
    getStatusCategoryName(category) {
      const names = {
        '1xx': 'Informational',
        '2xx': 'Success',
        '3xx': 'Redirection',
        '4xx': 'Client Error',
        '5xx': 'Server Error'
      };
      
      return names[category] || category;
    },
    
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
    },
    
    // Calcola il passo per l'asse Y
    calculateYAxisStepSize() {
      // Trova il valore massimo nei dati
      let maxValue = 0;
      
      if (this.timeSeriesData && this.timeSeriesData.length > 0) {
        if (this.groupBy === 'total') {
          // Per il volume totale, trova il massimo dei conteggi di richieste
          maxValue = Math.max(...this.timeSeriesData.map(p => p.requestCount || 0));
        } else {
          // Per altri raggruppamenti, calcola il totale per periodo
          this.timeSeriesData.forEach(point => {
            let periodTotal = 0;
            
            if (this.groupBy === 'method' && point.methodStats) {
              periodTotal = Object.values(point.methodStats).reduce((sum, count) => sum + count, 0);
            } else if (this.groupBy === 'status' && point.statusCodeStats) {
              periodTotal = Object.values(point.statusCodeStats).reduce((sum, count) => sum + count, 0);
            }
            
            maxValue = Math.max(maxValue, periodTotal);
          });
        }
      }
      
      // Calcola uno step appropriato in base al massimo
      if (maxValue <= 5) return 1;
      if (maxValue <= 20) return 2;
      if (maxValue <= 50) return 5;
      if (maxValue <= 100) return 10;
      
      // Per valori più grandi, arrotonda a una potenza di 10 appropriata
      const magnitude = Math.pow(10, Math.floor(Math.log10(maxValue)));
      return Math.ceil(maxValue / (magnitude * 5)) * magnitude;
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