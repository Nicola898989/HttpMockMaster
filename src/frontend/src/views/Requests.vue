<template>
  <div class="requests-view">
    <div class="row">
      <div class="col-md-4">
        <RequestsList />
      </div>
      <div class="col-md-8">
        <RequestDetail :request="selectedRequest" />
      </div>
    </div>
  </div>
</template>

<script>
import { mapState } from 'vuex';
import RequestsList from '@/components/RequestsList.vue';
import RequestDetail from '@/components/RequestDetail.vue';

export default {
  name: 'Requests',
  components: {
    RequestsList,
    RequestDetail
  },
  computed: {
    ...mapState({
      selectedRequest: state => state.selectedRequest
    })
  }
}
</script>

<style scoped>
.requests-view {
  height: calc(100vh - 120px);
}

.row {
  height: 100%;
}

.col-md-4, .col-md-8 {
  height: 100%;
  overflow-y: auto;
}
</style>
