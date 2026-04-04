<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import InteractiveMapCanvas from '@/components/InteractiveMapCanvas.vue';
</script>

<template>
  <div class="map-page">
    <InteractiveMapCanvas />
  </div>
</template>

<style scoped>
.map-page {
  height: 100%;
  width: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.map-page :deep(.interactive-map-root) {
  flex: 1 1 0;
  min-height: 0;
}
</style>
