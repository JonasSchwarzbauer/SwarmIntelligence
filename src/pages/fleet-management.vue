<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import { subscribeToTopic, unsubscribeFromTopic, connection } from "@/plugins/signalr";
import type { AgentDataDto } from "@/models/models";
import { apiUrl } from "@/config";

const TOPIC_NAME = "FleetManagement";

const agents = ref<AgentDataDto[]>([]);
const isLoading = ref(true);
const error = ref<string | null>(null);

const fetchAgents = async () => {
  isLoading.value = true;
  error.value = null;
  try {
    const response = await fetch(apiUrl('/api/cache/agents'));
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    agents.value = await response.json();
  } catch (err: any) {
    console.error("Failed to fetch agents:", err);
    error.value = "Failed to load agents";
  } finally {
    isLoading.value = false;
  }
};

const handleAgentUpdate = (dto: AgentDataDto) => {
  const index = agents.value.findIndex(a => a.agentId === dto.agentId);
  if (index !== -1) {
    agents.value[index] = dto;
  } else {
    agents.value.push(dto);
    agents.value.sort((a, b) => a.agentId - b.agentId);
  }
};

const orientationDeg = (rad: number) => ((rad + Math.PI) * (180 / Math.PI) + 180) % 360;

onMounted(() => {
  subscribeToTopic(TOPIC_NAME);
  fetchAgents();
  connection.on("AgentData", handleAgentUpdate);
});

onUnmounted(() => {
  unsubscribeFromTopic(TOPIC_NAME);
  connection.off("AgentData", handleAgentUpdate);
});

function getFlagCssColor(flag: string): string {
  if (flag === "WaypointActive") return "var(--accent)";
  if (flag === "StoppedSensorTimeout") return "var(--red)";
  if (flag.startsWith("Stopped")) return "var(--amber)";
  return "var(--text-3)";
}

function dwmCssColor(rate: number) {
  if (rate > 75) return 'var(--green)';
  if (rate > 40) return 'var(--amber)';
  return 'var(--red)';
}

const headers = [
  { title: "Agent",         key: "agentId",         sortable: true,  minWidth: "80px"  },
  { title: "Position",      key: "position",        sortable: false, minWidth: "165px" },
  { title: "Velocity",      key: "velocity",        sortable: true,  minWidth: "115px" },
  { title: "Orientation",   key: "orientation",     sortable: false, minWidth: "115px" },
  { title: "Frontal Dist.", key: "frontalDistance", sortable: true,  minWidth: "125px" },
  { title: "Target",        key: "target",          sortable: false, minWidth: "185px" },
  { title: "DWM Rate",      key: "dwmSuccessRate",  sortable: true,  minWidth: "130px" },
  { title: "Flags",         key: "flags",           sortable: false, minWidth: "150px" },
  { title: "Updated",       key: "timestamp",       sortable: true,  minWidth: "110px" },
];
</script>

<template>
  <div class="fleet">

    <div class="fleet__top">
      <div class="fleet__controls">
        <span v-if="error" class="mono fleet__error">
          <v-icon icon="mdi-alert-circle-outline" size="13" style="color:var(--red)" />
          {{ error }}
        </span>
        <span class="fleet__count mono">
          <span class="dot dot--online" />
          {{ agents.length }} agents
        </span>
        <v-btn icon="mdi-refresh" variant="text" size="small" :loading="isLoading" style="color:var(--text-3)" @click="fetchAgents" />
      </div>
    </div>

    <div class="card fleet__table-wrap">
      <v-data-table
        :headers="headers"
        :items="agents"
        item-value="agentId"
        :items-per-page="-1"
        density="comfortable"
        hover
        class="fleet-table"
      >
        <template #item.agentId="{ item }">
          <span class="mono" style="font-size:0.88rem; font-weight:600; color:var(--accent)">#{{ item.agentId }}</span>
        </template>

        <template #item.position="{ item }">
          <span class="mono" style="font-size:0.82rem; color:var(--text-2)">
            ({{ item.x.toFixed(2) }}, {{ item.y.toFixed(2) }})
          </span>
        </template>

        <template #item.velocity="{ item }">
          <div class="table-cell">
            <v-icon :icon="item.velocity > 0.01 ? 'mdi-speedometer' : 'mdi-speedometer-slow'" size="13"
              :style="{ color: item.velocity > 0.01 ? 'var(--green)' : 'var(--text-3)' }" />
            <span class="mono" :style="{ color: item.velocity > 0.01 ? 'var(--green)' : 'var(--text-2)', fontWeight: 500 }">
              {{ item.velocity.toFixed(2) }}<span style="color:var(--text-3); font-size:0.68rem"> m/s</span>
            </span>
          </div>
        </template>

        <template #item.orientation="{ item }">
          <div class="table-cell">
            <v-icon icon="mdi-navigation" size="13" style="color:var(--accent)"
              :style="{ transform: `rotate(${orientationDeg(item.orientation)}deg)` }" />
            <span class="mono" style="font-weight:500; color:var(--text)">
              {{ orientationDeg(item.orientation).toFixed(0) }}<span style="color:var(--text-3);font-size:0.68rem">&deg;</span>
            </span>
          </div>
        </template>

        <template #item.frontalDistance="{ item }">
          <div class="table-cell">
            <v-icon :icon="item.frontalDistance < 0.5 ? 'mdi-alert' : 'mdi-ruler'" size="13"
              :style="{ color: item.frontalDistance < 0.5 ? 'var(--red)' : item.frontalDistance < 1.0 ? 'var(--amber)' : 'var(--text-3)' }" />
            <span class="mono" :style="{ color: item.frontalDistance < 0.5 ? 'var(--red)' : item.frontalDistance < 1.0 ? 'var(--amber)' : 'var(--text)', fontWeight: 500 }">
              {{ item.frontalDistance.toFixed(2) }}<span style="color:var(--text-3);font-size:0.68rem"> m</span>
            </span>
          </div>
        </template>

        <template #item.target="{ item }">
          <div v-if="item.target" class="table-cell">
            <v-icon icon="mdi-flag-checkered" size="12" style="color:var(--purple)" />
            <span class="mono" style="font-size:0.82rem; color:var(--purple)">({{ item.target.x.toFixed(2) }}, {{ item.target.y.toFixed(2) }})</span>
          </div>
          <span v-else class="mono" style="color:var(--text-3); font-size:0.72rem">&mdash;</span>
        </template>

        <template #item.dwmSuccessRate="{ item }">
          <div class="table-cell">
            <v-progress-linear :model-value="item.dwmSuccessRate"
              :color="item.dwmSuccessRate > 75 ? 'success' : item.dwmSuccessRate > 40 ? 'warning' : 'error'"
              height="4" rounded bg-color="surface-variant" style="width:52px; flex-shrink:0" />
            <span class="mono" :style="{ fontSize:'0.75rem', fontWeight:600, color: dwmCssColor(item.dwmSuccessRate) }">
              {{ item.dwmSuccessRate.toFixed(0) }}%
            </span>
          </div>
        </template>

        <template #item.flags="{ item }">
          <div v-if="item.flags?.length" class="flags-cell">
            <span v-for="f in item.flags" :key="f" class="flag mono" :style="{ color: getFlagCssColor(f) }">{{ f }}</span>
          </div>
          <span v-else class="mono" style="color:var(--text-3); font-size:0.72rem">&mdash;</span>
        </template>

        <template #item.timestamp="{ item }">
          <TimestampChip :timestamp="item.timestamp" />
        </template>

        <template #no-data>
          <div class="empty-state" style="padding:48px 0">
            <v-icon icon="mdi-radar" size="36" style="color:var(--text-3)" class="mb-2" />
            <div style="font-size:0.78rem; color:var(--text-2)">Waiting for telemetry&hellip;</div>
          </div>
        </template>

        <template #bottom />
      </v-data-table>
    </div>

  </div>
</template>

<style scoped>
.fleet {
  padding: 24px;
  min-height: 100%;
  background: var(--bg);
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.fleet__top { display: flex; align-items: center; justify-content: flex-end; }
.fleet__controls { display: flex; align-items: center; gap: 12px; }
.fleet__count { display: flex; align-items: center; gap: 7px; font-size: 0.75rem; color: var(--text-2); }
.fleet__error { display: flex; align-items: center; gap: 5px; font-size: 0.72rem; color: var(--red); }
.fleet__table-wrap { overflow: hidden; }

.fleet-table {
  background: transparent !important;
  --v-theme-surface: 20, 23, 31 !important;
}
.fleet-table :deep(.v-table__wrapper) { background: transparent !important; }

.fleet-table :deep(.v-data-table__th) {
  font-size: 0.75rem !important;
  font-weight: 600 !important;
  letter-spacing: 0.04em !important;
  text-transform: uppercase !important;
  color: var(--text-3) !important;
  background: var(--surface-raised) !important;
  border-bottom: 1px solid var(--border) !important;
  padding: 12px 16px !important;
  white-space: nowrap;
}

.fleet-table :deep(.v-data-table__td) {
  font-family: var(--font-mono) !important;
  font-size: 0.9rem !important;
  border-bottom: 1px solid var(--border) !important;
  padding: 10px 16px !important;
}

.fleet-table :deep(tbody tr:hover td) {
  background: var(--surface-hover) !important;
}

.table-cell { display: flex; align-items: center; gap: 6px; }
.flags-cell { display: flex; flex-wrap: wrap; gap: 4px; }
</style>
