<template>
  <div class="rules-view">
    <RulesList />
  </div>
</template>

<script>
import RulesList from '@/components/RulesList.vue';

export default {
  name: 'Rules',
  components: {
    RulesList
  }
}
</script>

<style scoped>
.rules-view {
  height: calc(100vh - 120px);
}
</style>
