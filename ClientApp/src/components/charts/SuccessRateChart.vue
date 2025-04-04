<template>
  <div class="chart-wrapper">
    <BaseChart
      type="line"
      :chartData="chartData"
      :options="options"
      :height="350"
      cssClasses="success-rate-chart"
    />
  </div>
</template>

<script>
import BaseChart from './BaseChart.js';

export default {
  name: 'SuccessRateChart',
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
      default: 'Tasso di Successo'
    },
    
    // Colore principale del grafico
    primaryColor: {
      type: String,
      default: '#28a745'
    }
  },
  computed: {
    chartData() {
      const labels = this.timeSeriesData.map(point => {
        const date = new Date(point.timestamp);
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      });
      
      return {
        labels,
        datasets: [
          {
            label: 'Tasso di successo (%)',
            data: this.timeSeriesData.map(point => point.successRate),
            borderColor: this.primaryColor,
            backgroundColor: this.primaryColor + '33', // Aggiunge trasparenza
            tension: 0.3,
            fill: true,
            pointRadius: 3,
            pointHoverRadius: 6
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
              bottom: 20
            }
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            callbacks: {
              label: function(context) {
                return `${context.dataset.label}: ${context.parsed.y.toFixed(1)}%`;
              }
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
            ticks: {
              maxRotation: 45,
              minRotation: 45
            }
          },
          y: {
            title: {
              display: true,
              text: 'Percentuale'
            },
            min: 0,
            max: 100,
            ticks: {
              callback: value => `${value}%`
            },
            grid: {
              color: '#f0f0f0'
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