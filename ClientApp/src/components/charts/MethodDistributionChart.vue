<template>
  <div class="chart-wrapper">
    <BaseChart
      type="pie"
      :chartData="chartData"
      :options="options"
      :height="300"
      cssClasses="method-distribution-chart"
    />
  </div>
</template>

<script>
import BaseChart from './BaseChart.js';

export default {
  name: 'MethodDistributionChart',
  components: {
    BaseChart
  },
  props: {
    // Dati dei metodi HTTP
    methodData: {
      type: Array,
      required: true
    },
    
    // Titolo del grafico
    title: {
      type: String,
      default: 'Distribuzione Metodi HTTP'
    }
  },
  computed: {
    chartData() {
      // Colori predefiniti per i vari metodi HTTP
      const colorMap = {
        'GET': '#4BC0C0',
        'POST': '#FF6384',
        'PUT': '#FFCD56',
        'DELETE': '#FF9F40',
        'PATCH': '#36A2EB',
        'HEAD': '#9966FF',
        'OPTIONS': '#C9CBCF'
      };
      
      return {
        labels: this.methodData.map(item => item.method),
        datasets: [
          {
            data: this.methodData.map(item => item.count),
            backgroundColor: this.methodData.map(item => colorMap[item.method] || '#C9CBCF'),
            borderWidth: 1,
            borderColor: '#ffffff'
          }
        ]
      };
    },
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
              bottom: 10
            }
          },
          tooltip: {
            callbacks: {
              label: function(context) {
                const label = context.label || '';
                const value = context.parsed || 0;
                const total = context.dataset.data.reduce((acc, current) => acc + current, 0);
                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                return `${label}: ${value} (${percentage}%)`;
              }
            }
          },
          legend: {
            position: 'right',
            labels: {
              padding: 15,
              usePointStyle: true,
              pointStyle: 'circle'
            }
          }
        }
      };
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