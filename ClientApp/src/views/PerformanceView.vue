<template>
  <div class="performance-view">
    <NavigationBar />
    <PerformanceDashboard />
  </div>
</template>

<script>
import NavigationBar from '../components/NavigationBar.vue';
import PerformanceDashboard from '../components/PerformanceDashboard.vue';

export default {
  name: 'PerformanceView',
  components: {
    NavigationBar,
    PerformanceDashboard
  }
};
</script>

<style scoped>
.performance-view {
  display: flex;
  flex-direction: column;
  height: 100vh;
}
</style>