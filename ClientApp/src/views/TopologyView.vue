<template>
  <div class="topology-view">
    <NetworkTopology />
  </div>
</template>

<script>
import NetworkTopology from '@/components/NetworkTopology.vue';

export default {
  name: 'TopologyView',
  components: {
    NetworkTopology
  }
};
</script>

<style scoped>
.topology-view {
  height: 100%;
  width: 100%;
  display: flex;
  flex-direction: column;
}
</style>