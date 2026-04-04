<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import AwarenessMapCanvas from '@/components/AwarenessMapCanvas.vue';
</script>

<template>
  <div class="map-page">
    <AwarenessMapCanvas />
  </div>
</template>

<style scoped>
/* Fill all space that v-main provides without allowing page scroll */
.map-page {
  height: 100%;
  width: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
/* Make the root element of AwarenessMapCanvas participate in the flex layout */
.map-page :deep(.map-container) {
  flex: 1 1 0;
  min-height: 0;
}
</style>
