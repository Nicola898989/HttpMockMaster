<template>
  <div class="comparison-view">
    <h1>Strumento di Comparazione</h1>
    <p class="description">
      Questo strumento ti permette di confrontare visivamente diverse richieste HTTP e le loro risposte,
      evidenziando le differenze. Utile per debugging e test A/B.
    </p>
    
    <RequestComparison />
  </div>
</template>

<script>
import RequestComparison from '@/components/RequestComparison.vue';

export default {
  name: 'ComparisonView',
  components: {
    RequestComparison
  }
};
</script>

<style scoped>
.comparison-view {
  padding: 20px;
}

.description {
  max-width: 800px;
  margin-bottom: 2rem;
  color: #6c757d;
}
</style>
