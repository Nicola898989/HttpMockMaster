import { defineComponent, h } from 'vue';
import { Chart, registerables } from 'chart.js';
import { generateUniqueID } from '@/utils/helpers';

// Registra tutti i componenti necessari di Chart.js
Chart.register(...registerables);

/**
 * Componente base per tutti i grafici
 * Estende il componente Chart di vue-chartjs e fornisce funzionalità comuni
 */
export default defineComponent({
  name: 'BaseChart',
  props: {
    // Tipo di grafico (line, bar, pie, doughnut, radar, polarArea, bubble, scatter)
    type: {
      type: String,
      required: true,
      validator: (value) => {
        return [
          'line',
          'bar',
          'pie',
          'doughnut',
          'radar',
          'polarArea',
          'bubble',
          'scatter'
        ].includes(value);
      }
    },
    
    // Dati del grafico
    chartData: {
      type: Object,
      required: true
    },
    
    // Opzioni del grafico
    options: {
      type: Object,
      default: () => ({})
    },
    
    // Classi CSS aggiuntive per il canvas
    cssClasses: {
      type: [String, Array, Object],
      default: ''
    },
    
    // Larghezza del grafico
    width: {
      type: Number,
      default: 400
    },
    
    // Altezza del grafico
    height: {
      type: Number,
      default: 400
    },
    
    // Plugins aggiuntivi
    plugins: {
      type: Array,
      default: () => []
    },
    
    // ID del canvas
    chartId: {
      type: String,
      default: () => `chart-${generateUniqueID()}`
    }
  },
  
  setup(props, { expose }) {
    // Riferimento all'istanza del grafico
    let chartInstance = null;
    
    // Metodo per aggiornare i dati del grafico
    const updateChart = () => {
      if (chartInstance) {
        chartInstance.data = props.chartData;
        chartInstance.options = props.options;
        chartInstance.update();
      }
    };
    
    // Metodo per renderizzare il grafico
    const renderChart = () => {
      if (chartInstance) {
        chartInstance.destroy();
      }
      
      const canvas = document.getElementById(props.chartId);
      if (canvas) {
        const ctx = canvas.getContext('2d');
        
        // Crea una nuova istanza del grafico
        chartInstance = new Chart(ctx, {
          type: props.type,
          data: props.chartData,
          options: props.options,
          plugins: props.plugins
        });
      }
    };
    
    // Metodo per salvare il grafico come immagine
    const saveAsImage = (fileName = 'chart.png', quality = 1) => {
      if (chartInstance) {
        const canvas = document.getElementById(props.chartId);
        if (canvas) {
          const link = document.createElement('a');
          link.href = canvas.toDataURL('image/png', quality);
          link.download = fileName;
          link.click();
        }
      }
    };
    
    // Metodo per ottenere i dati del grafico come CSV
    const getDataAsCSV = () => {
      if (!chartInstance || !props.chartData) return '';
      
      const labels = props.chartData.labels || [];
      const datasets = props.chartData.datasets || [];
      
      let csv = 'data:text/csv;charset=utf-8,';
      
      // Aggiungi intestazioni
      let headers = ['Label'];
      datasets.forEach(dataset => {
        headers.push(dataset.label || 'Dataset');
      });
      csv += headers.join(',') + '\n';
      
      // Aggiungi righe
      labels.forEach((label, i) => {
        let row = [label];
        datasets.forEach(dataset => {
          row.push(dataset.data[i]);
        });
        csv += row.join(',') + '\n';
      });
      
      return encodeURI(csv);
    };
    
    // Metodo per scaricare i dati come CSV
    const downloadCSV = (fileName = 'chart-data.csv') => {
      const csvContent = getDataAsCSV();
      if (csvContent) {
        const link = document.createElement('a');
        link.href = csvContent;
        link.download = fileName;
        link.click();
      }
    };
    
    // Esponi metodi pubblici
    expose({
      renderChart,
      updateChart,
      saveAsImage,
      downloadCSV,
      getDataAsCSV
    });
    
    return () => h('canvas', {
      id: props.chartId,
      width: props.width,
      height: props.height,
      class: props.cssClasses
    });
  },
  
  watch: {
    'chartData': {
      handler() {
        this.updateChart();
      },
      deep: true
    },
    'options': {
      handler() {
        this.updateChart();
      },
      deep: true
    }
  },
  
  mounted() {
    this.renderChart();
  },
  
  beforeUnmount() {
    if (this.chartInstance) {
      this.chartInstance.destroy();
    }
  }
});