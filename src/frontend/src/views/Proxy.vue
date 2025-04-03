<template>
  <div class="proxy-view">
    <ProxySettings />
  </div>
</template>

<script>
import ProxySettings from '@/components/ProxySettings.vue';

export default {
  name: 'Proxy',
  components: {
    ProxySettings
  }
}
</script>

<style scoped>
.proxy-view {
  height: calc(100vh - 120px);
  overflow-y: auto;
}
</style>
