<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import { computed, ref } from 'vue'
import Go2RtcWebRtc from '@/components/Go2RtcWebRtc.vue'

const lead = ref({
  id: 'Lead-01',
  headingDeg: 0,
  speedMps: 0,
  pos: { x: 0, y: 0 },
  lastUpdate: new Date().toISOString(),
})

const headingText = computed(() => `${Math.round(lead.value.headingDeg)}°`)
const speedText = computed(() => `${lead.value.speedMps.toFixed(2)} m/s`)
const posText = computed(() => `${lead.value.pos.x.toFixed(2)}, ${lead.value.pos.y.toFixed(2)}`)
</script>

<template>
  <div class="lv">


    <div class="lv__content">

      <!-- Telemetry -->
      <div class="card lv__telemetry">
        <div class="card-title">
          <v-icon icon="mdi-car-connected" size="14" style="color:var(--accent)" />
          Telemetry
          <span class="badge" style="background:var(--accent-dim); color:var(--accent); border:1px solid var(--accent-border); margin-left:auto">{{ lead.id }}</span>
        </div>
        <div style="height:1px; background:var(--border)" />

        <div class="lv__stats">
          <div class="lv__stat">
            <div class="label">Heading</div>
            <div class="mono lv__stat-val">{{ headingText }}</div>
          </div>
          <div class="lv__stat">
            <div class="label">Speed</div>
            <div class="mono lv__stat-val">{{ speedText }}</div>
          </div>
          <div class="lv__stat">
            <div class="label">Position</div>
            <div class="mono lv__stat-val">{{ posText }}</div>
          </div>
        </div>

        <div class="lv__update mono">Last update: {{ lead.lastUpdate }}</div>
      </div>

      <!-- Camera -->
      <div class="card lv__camera">
        <div class="card-title">
          <v-icon icon="mdi-cctv" size="14" style="color:var(--text-2)" />
          Camera Stream
        </div>
        <div style="height:1px; background:var(--border)" />
        <div class="lv__cam-wrap">
          <Go2RtcWebRtc src="cam1" />
        </div>
      </div>

    </div>

  </div>
</template>

<style scoped>
.lv {
  padding: 24px;
  min-height: 100%;
  background: var(--bg);
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.lv__content {
  display: grid;
  grid-template-columns: 280px 1fr;
  gap: 16px;
  flex: 1;
}

@media (max-width: 900px) {
  .lv__content { grid-template-columns: 1fr; }
}

.lv__telemetry { display: flex; flex-direction: column; }

.lv__stats {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.lv__stat-val {
  font-size: 1.2rem;
  font-weight: 600;
  color: var(--text);
  margin-top: 4px;
}

.lv__update {
  font-size: 0.68rem;
  color: var(--text-3);
  padding: 0 16px 14px;
  margin-top: auto;
}

.lv__camera {
  display: flex;
  flex-direction: column;
}

.lv__cam-wrap {
  flex: 1;
  min-height: 400px;
  padding: 4px;
}
</style>
