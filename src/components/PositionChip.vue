<script setup lang="ts">
defineProps<{
  x: number;
  y: number;
}>();

const fmt = (v: number) => (v != null ? v.toFixed(2) : '\u2014');
</script>

<template>
  <div class="d-flex align-center" style="gap: 4px;">
    <v-chip size="x-small" color="blue-grey" variant="tonal" label class="pos-chip">
      <span class="coord-label">X</span>
      <span class="coord-value">{{ fmt(x) }}</span>
    </v-chip>
    <v-chip size="x-small" color="blue-grey" variant="tonal" label class="pos-chip">
      <span class="coord-label">Y</span>
      <span class="coord-value">{{ fmt(y) }}</span>
    </v-chip>
  </div>
</template>

<style scoped>
.pos-chip {
  font-family: "Roboto Mono", monospace;
  min-width: 64px;
}
.coord-label {
  font-size: 0.6rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  opacity: 0.6;
  margin-right: 4px;
  text-transform: uppercase;
}
.coord-value {
  font-weight: 600;
  font-size: 0.72rem;
}
</style>
