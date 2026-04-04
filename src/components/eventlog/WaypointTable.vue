<script setup lang="ts">
import type { WaypointDto } from '@/models/models'
defineProps<{ waypoints: WaypointDto[] }>()
</script>

<template>
  <div class="wt-wrap">
    <div class="wt-head">
      <span class="wt-idx">#</span>
      <span>X</span><span>Y</span><span>Heading</span><span>Max Spd</span>
    </div>
    <div v-if="!waypoints.length" class="wt-empty">No waypoints</div>
    <div v-for="(wp, i) in waypoints" :key="i" class="wt-row">
      <span class="wt-idx">{{ i + 1 }}</span>
      <span>{{ wp.x.toFixed(3) }}</span>
      <span>{{ wp.y.toFixed(3) }}</span>
      <span>{{ wp.heading.toFixed(1) }}°</span>
      <span>{{ wp.maxSpeed }}</span>
    </div>
  </div>
</template>

<style scoped>
.wt-wrap { font-family: 'Roboto Mono', monospace; font-size: 11px; }
.wt-head, .wt-row {
  display: grid;
  grid-template-columns: 22px 1fr 1fr 1.3fr 1.1fr;
  gap: 6px;
  padding: 3px 0;
  border-bottom: 1px solid rgba(48,54,61,0.3);
}
.wt-head { color: rgba(255,255,255,0.3); font-size: 10px; text-transform: uppercase; letter-spacing: 0.05em; }
.wt-row  { color: rgba(255,255,255,0.72); }
.wt-row:last-child { border-bottom: none; }
.wt-idx  { color: rgba(255,255,255,0.28); }
.wt-empty { color: rgba(255,255,255,0.28); font-style: italic; padding: 4px 0; }
</style>
