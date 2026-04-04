<script setup lang="ts">
import { computed } from 'vue'
import type { AgentDataDto } from '@/models/models'
import EventCard from './EventCard.vue'

const props = defineProps<{ event: AgentDataDto }>()

const flagColor = (flag: string) =>
  flag.includes('Error') || flag.includes('Timeout') ? 'error' :
  flag.includes('Stopped') ? 'warn' :
  flag.includes('Active')  ? 'green' : 'dim'

const dwmColor = computed(() => {
  const r = props.event.dwmSuccessRate * 100
  return r >= 80 ? '#34d399' : r >= 50 ? '#fbbf24' : '#f87171'
})
</script>

<template>
  <EventCard title="Agent Data" icon="mdi-robot" accent-color="#34d399" :timestamp="event.timestamp" :expandable="true">
    <template #header>
      <span class="ec-badge blue">#{{ event.agentId }}</span>
      <span class="ec-badge dim">({{ event.x.toFixed(2) }}, {{ event.y.toFixed(2) }})</span>
      <span class="ec-badge dim">{{ event.velocity.toFixed(2) }} m/s</span>
      <span v-if="event.flags.length" class="ec-badge amber">{{ event.flags.length }} flag{{ event.flags.length > 1 ? 's' : '' }}</span>
    </template>
    <template #detail>
      <div class="ad-grid">
        <div class="ad-section">
          <div class="ad-label">Position</div>
          <div class="ad-val">x={{ event.x.toFixed(3) }} y={{ event.y.toFixed(3) }}</div>
          <div class="ad-label mt4">Orientation</div>
          <div class="ad-val">{{ (event.orientation * 180 / Math.PI).toFixed(1) }}°</div>
          <div class="ad-label mt4">Frontal dist</div>
          <div class="ad-val">{{ event.frontalDistance.toFixed(3) }} m</div>
        </div>
        <div class="ad-section">
          <div class="ad-label">Target</div>
          <div class="ad-val">x={{ event.target.x.toFixed(3) }} y={{ event.target.y.toFixed(3) }}</div>
          <div class="ad-val dim">hdg={{ event.target.heading.toFixed(1) }}° spd={{ event.target.maxSpeed }}</div>
          <div class="ad-label mt4">DWM success</div>
          <div class="dwm-bar-wrap">
            <div class="dwm-bar-fill" :style="{ width: `${event.dwmSuccessRate * 100}%`, background: dwmColor }" />
            <span class="dwm-pct" :style="{ color: dwmColor }">{{ (event.dwmSuccessRate * 100).toFixed(0) }}%</span>
          </div>
        </div>
      </div>
      <div v-if="event.flags.length" class="ad-flags">
        <span v-for="flag in event.flags" :key="flag" class="ec-badge-detail" :class="flagColor(flag)">{{ flag }}</span>
      </div>
    </template>
  </EventCard>
</template>

<style scoped>
.ad-grid    { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 8px; }
.ad-section { font-family: 'Roboto Mono', monospace; font-size: 11px; }
.ad-label   { font-size: 10px; color: rgba(255,255,255,0.3); text-transform: uppercase; letter-spacing: 0.04em; }
.ad-val     { color: rgba(255,255,255,0.75); }
.ad-val.dim { color: rgba(255,255,255,0.38); font-size: 10px; }
.mt4 { margin-top: 6px; }
.dwm-bar-wrap { position: relative; height: 14px; background: rgba(255,255,255,0.06); border-radius: 3px; overflow: hidden; margin-top: 3px; }
.dwm-bar-fill { height: 100%; border-radius: 3px; transition: width 0.3s; }
.dwm-pct { position: absolute; right: 4px; top: 0; bottom: 0; display: flex; align-items: center; font-size: 10px; font-family: monospace; }
.ad-flags  { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 6px; border-top: 1px solid rgba(48,54,61,0.35); padding-top: 6px; }
.ec-badge-detail { display: inline-flex; align-items: center; padding: 1px 6px; border-radius: 3px; font-size: 10px; font-family: 'Roboto Mono', monospace; border: 1px solid transparent; }
.ec-badge-detail.error { background: rgba(248,113,113,0.12); color: #f87171; border-color: rgba(248,113,113,0.22); }
.ec-badge-detail.warn  { background: rgba(251,191,36,0.12);  color: #fbbf24; border-color: rgba(251,191,36,0.22); }
.ec-badge-detail.green { background: rgba(52,211,153,0.12);  color: #34d399; border-color: rgba(52,211,153,0.22); }
.ec-badge-detail.dim   { background: rgba(255,255,255,0.05); color: rgba(255,255,255,0.42); border-color: rgba(255,255,255,0.08); }
</style>
