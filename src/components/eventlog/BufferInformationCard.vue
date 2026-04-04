<script setup lang="ts">
import { computed } from 'vue'
import type { BufferInformationDto } from '@/models/models'
import EventCard from './EventCard.vue'

const props = defineProps<{ event: BufferInformationDto }>()

const usageColor = computed(() => {
  const p = props.event.bufferUsagePercentage
  return p >= 85 ? '#f87171' : p >= 60 ? '#fbbf24' : '#34d399'
})
</script>

<template>
  <EventCard title="Buffer Info" icon="mdi-database" accent-color="#22d3ee" :timestamp="event.timestamp" :expandable="true">
    <template #header>
      <span class="ec-badge" :class="event.success ? 'success' : 'error'">
        {{ event.success ? 'Healthy' : 'Warning' }}
      </span>
      <span class="ec-badge cyan">{{ event.bufferCount }}/{{ event.bufferCapacity }}</span>
      <div class="buf-wrap">
        <div class="buf-fill" :style="{ width: `${event.bufferUsagePercentage}%`, background: usageColor }" />
        <span class="buf-pct" :style="{ color: usageColor }">{{ event.bufferUsagePercentage.toFixed(1) }}%</span>
      </div>
    </template>
    <template #detail>
      <div class="bi-row">
        <span class="bi-key">Count</span><span class="bi-val">{{ event.bufferCount }}</span>
        <span class="bi-key ml">Capacity</span><span class="bi-val">{{ event.bufferCapacity }}</span>
        <span class="bi-key ml">Usage</span><span class="bi-val" :style="{ color: usageColor }">{{ event.bufferUsagePercentage.toFixed(2) }}%</span>
      </div>
    </template>
  </EventCard>
</template>

<style scoped>
.buf-wrap { position: relative; width: 80px; height: 14px; background: rgba(255,255,255,0.06); border-radius: 3px; overflow: hidden; flex-shrink: 0; }
.buf-fill { height: 100%; border-radius: 3px; transition: width 0.3s; }
.buf-pct  { position: absolute; right: 3px; top: 0; bottom: 0; display: flex; align-items: center; font-size: 9px; font-family: monospace; }
.bi-row   { display: flex; align-items: center; gap: 4px; font-family: 'Roboto Mono', monospace; font-size: 11px; }
.bi-key   { color: rgba(255,255,255,0.3); font-size: 10px; text-transform: uppercase; letter-spacing: 0.04em; }
.bi-val   { color: rgba(255,255,255,0.75); min-width: 28px; }
.ml       { margin-left: 16px; }
</style>
