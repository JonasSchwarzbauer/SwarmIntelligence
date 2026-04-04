<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from "vue";
import { subscribeToTopic, unsubscribeFromTopic, connection } from "@/plugins/signalr";
import type {
  AgentDataDto,
  BufferInformationDto,
  ManagerStateDto,
  WorkerStateDto,
  UsbStartedDto,
  ManagerStateApi,
  MapWorkerStateApi,
} from "@/models/models";
import { apiUrl } from "@/config";

// ── Topics ──────────────────────────────────────────────────────────────────
const TOPICS = ["BufferInformation", "ManagerState", "WorkerState", "UsbStarted", "AgentData", "DriveControlError", "MapControlError"];

// ── Agents ───────────────────────────────────────────────────────────────────
const agents = ref<AgentDataDto[]>([]);

const activeAgents  = computed(() => agents.value.filter(a => isAgentActive(a)));
const stoppedAgents = computed(() => agents.value.filter(a => !isAgentActive(a)));
const avgVelocity   = computed(() => {
  if (!agents.value.length) return 0;
  return agents.value.reduce((s, a) => s + a.velocity, 0) / agents.value.length;
});
const avgDwm = computed(() => {
  if (!agents.value.length) return 0;
  return agents.value.reduce((s, a) => s + a.dwmSuccessRate, 0) / agents.value.length;
});

/** Agent is considered active if it received data in the last 10 seconds */
function isAgentActive(a: AgentDataDto) {
  return (Date.now() - new Date(a.timestamp).getTime()) < 10_000;
}

// ── Buffer ────────────────────────────────────────────────────────────────────
const buffer = ref<BufferInformationDto | null>(null);
const bufferHistory = ref<number[]>([]);

const bufferColor = computed(() => {
  const p = buffer.value?.bufferUsagePercentage ?? 0;
  if (p >= 10) return "error";
  if (p >= 5) return "warning";
  return "success";
});

const bufferCssColor = computed(() => {
  const p = buffer.value?.bufferUsagePercentage ?? 0;
  if (p >= 10) return "var(--red)";
  if (p >= 5)  return "var(--amber)";
  return "var(--green)";
});

// ── Manager state ─────────────────────────────────────────────────────────────
const managerRunning  = ref(false);
const managerUpdated  = ref<string | null>(null);

// ── Worker state ──────────────────────────────────────────────────────────────
const workerState   = ref("Stopped");
const workerUpdated = ref<string | null>(null);

// ── USB status ────────────────────────────────────────────────────────────────
const usbSuccess  = ref(false);
const usbPort     = ref<string | null>(null);
const usbBaud     = ref<number | null>(null);
const usbUpdated  = ref<string | null>(null);

// ── Swarm mode ────────────────────────────────────────────────────────────────
const swarmMode = ref<string | null>(null);

const swarmModeColor = computed(() => {
  switch (swarmMode.value) {
    case "Formation": return "primary";
    case "Manual":    return "secondary";
    default:          return "grey";
  }
});

const swarmModeIcon = computed(() => {
  switch (swarmMode.value) {
    case "Formation": return "mdi-hexagon-multiple";
    case "Manual":    return "mdi-steering";
    default:          return "mdi-help-circle-outline";
  }
});

// ── Recent errors ─────────────────────────────────────────────────────────────
const recentErrors = ref<{ message: string; source: string; timestamp: string }[]>([]);
const MAX_ERRORS = 5;

// ── Loading ───────────────────────────────────────────────────────────────────
const isLoading = ref(true);

// ── Fetch initial data ────────────────────────────────────────────────────────
async function safeJson<T>(res: Response): Promise<T | null> {
  const text = await res.text();
  if (!text) return null;
  try { return JSON.parse(text) as T; } catch { return null; }
}

const fetchAll = async () => {
  isLoading.value = true;
  try {
    const [agentsRes, bufferRes, managerRes, workerRes, usbRes, modeRes] = await Promise.all([
      fetch(apiUrl("/api/cache/agents")),
      fetch(apiUrl("/api/cache/buffer-information")),
      fetch(apiUrl("/api/cache/manager-state")),
      fetch(apiUrl("/api/cache/map-worker-state")),
      fetch(apiUrl("/api/cache/usb-status")),
      fetch(apiUrl("/api/cache/swarm-mode")),
    ]);

    if (agentsRes.ok)  { const d = await safeJson<AgentDataDto[]>(agentsRes);   if (d) agents.value = d; }
    if (bufferRes.ok)  { const d = await safeJson<BufferInformationDto>(bufferRes); if (d) applyBuffer(d); }
    if (managerRes.ok) {
      const d = await safeJson<ManagerStateApi>(managerRes);
      if (d) { managerRunning.value = d.isRunning; managerUpdated.value = d.lastUpdated; }
    }
    if (workerRes.ok) {
      const d = await safeJson<MapWorkerStateApi>(workerRes);
      if (d) { workerState.value = d.currentState; workerUpdated.value = d.lastUpdated; }
    }
    if (usbRes.ok)  { const d = await safeJson<any>(usbRes);  if (d) applyUsb(d); }
    if (modeRes.ok) { const d = await safeJson<any>(modeRes); if (d) swarmMode.value = d?.mode ?? null; }
  } finally {
    isLoading.value = false;
  }
};

function applyBuffer(dto: BufferInformationDto) {
  buffer.value = dto;
  bufferHistory.value = [...bufferHistory.value.slice(-29), dto.bufferUsagePercentage];
}

function applyUsb(dto: { success?: boolean; portName?: string; baudRate?: number; message?: string; timestamp?: string }) {
  usbSuccess.value  = dto.success  ?? false;
  usbPort.value     = dto.portName ?? null;
  usbBaud.value     = dto.baudRate ?? null;
  usbUpdated.value  = dto.timestamp ?? null;
}

// ── SignalR handlers ──────────────────────────────────────────────────────────
const handleAgentData = (dto: AgentDataDto) => {
  const idx = agents.value.findIndex(a => a.agentId === dto.agentId);
  if (idx !== -1) agents.value[idx] = dto;
  else {
    agents.value.push(dto);
    agents.value.sort((a, b) => a.agentId - b.agentId);
  }
};

const handleBuffer       = (dto: BufferInformationDto) => applyBuffer(dto);
const handleManagerState = (dto: ManagerStateDto)      => { managerRunning.value = dto.isRunning; managerUpdated.value = dto.timestamp; };
const handleWorkerState  = (dto: WorkerStateDto)       => { workerState.value = dto.state; workerUpdated.value = dto.timestamp; };
const handleUsbStarted   = (dto: UsbStartedDto)        => applyUsb(dto);
const handleDriveError   = (dto: any) => pushError(dto.message ?? "DriveControl error", dto.source ?? "DriveControl", dto.timestamp);
const handleMapError     = (dto: any) => pushError(dto.message ?? "MapControl error",   dto.source ?? "MapControl",   dto.timestamp);

function pushError(message: string, source: string, timestamp: string) {
  recentErrors.value = [{ message, source, timestamp }, ...recentErrors.value].slice(0, MAX_ERRORS);
}

const fmt = (ts: string | null) => ts ? new Date(ts).toLocaleTimeString() : "—";

function agentStatusDot(a: AgentDataDto) {
  if (!isAgentActive(a)) return 'dot--offline';
  if (a.flags?.some(f => f.startsWith("Stopped"))) return 'dot--warn';
  return 'dot--online';
}

function dwmCssColor(rate: number) {
  if (rate > 75) return 'var(--green)';
  if (rate > 40) return 'var(--amber)';
  return 'var(--red)';
}

onMounted(() => {
  TOPICS.forEach(t => subscribeToTopic(t));
  connection.on("AgentData",        handleAgentData);
  connection.on("BufferInformation",handleBuffer);
  connection.on("ManagerState",     handleManagerState);
  connection.on("WorkerState",      handleWorkerState);
  connection.on("UsbStarted",       handleUsbStarted);
  connection.on("DriveControlError",handleDriveError);
  connection.on("MapControlError",  handleMapError);
  fetchAll();
});

onUnmounted(() => {
  TOPICS.forEach(t => unsubscribeFromTopic(t));
  connection.off("AgentData",        handleAgentData);
  connection.off("BufferInformation",handleBuffer);
  connection.off("ManagerState",     handleManagerState);
  connection.off("WorkerState",      handleWorkerState);
  connection.off("UsbStarted",       handleUsbStarted);
  connection.off("DriveControlError",handleDriveError);
  connection.off("MapControlError",  handleMapError);
});
</script>

<template>
  <div class="dash">

    <!-- ── KPI Row ─────────────────────────────────────────────── -->
    <div class="kpi-row">

      <div class="card kpi">
        <div class="kpi__head"><span class="label">Agents</span></div>
        <div class="value mono">{{ agents.length }}</div>
        <v-progress-linear :model-value="agents.length ? (activeAgents.length / agents.length) * 100 : 0" color="success" bg-color="surface-variant" height="3" class="mt-2 mb-2" />
        <div class="kpi__sub">
          <span class="dot dot--online" style="width:6px;height:6px" />
          <span class="mono" style="font-size:0.75rem; color:var(--green)">{{ activeAgents.length }} active</span>
          <template v-if="stoppedAgents.length">
            <span class="dot dot--error" style="width:6px;height:6px;margin-left:6px" />
            <span class="mono" style="font-size:0.75rem; color:var(--red)">{{ stoppedAgents.length }} idle</span>
          </template>
        </div>
      </div>

      <div class="card kpi">
        <div class="kpi__head"><span class="label">Buffer Fill</span></div>
        <div class="value mono" :style="{ color: bufferCssColor }">
          {{ buffer ? buffer.bufferUsagePercentage.toFixed(1) : '—' }}<span class="unit">%</span>
        </div>
        <v-progress-linear v-if="buffer" :model-value="buffer.bufferUsagePercentage" :color="bufferColor" bg-color="surface-variant" height="3" class="mt-2 mb-2" />
        <div v-if="buffer" class="kpi__sub">
          <span class="mono" style="font-size:0.75rem; color:var(--text-2)">{{ buffer.bufferCount }} / {{ buffer.bufferCapacity }} slots</span>
        </div>
      </div>

      <div class="card kpi">
        <div class="kpi__head"><span class="label">Avg Velocity</span></div>
        <div class="value mono">{{ avgVelocity.toFixed(2) }}<span class="unit">m/s</span></div>
        <v-progress-linear :model-value="Math.min((avgVelocity / 1.5) * 100, 100)" color="primary" bg-color="surface-variant" height="3" class="mt-2 mb-2" />
        <div class="kpi__sub">
          <span class="mono" style="font-size:0.75rem; color:var(--text-2)">across {{ agents.length }} agent(s)</span>
        </div>
      </div>

      <div class="card kpi">
        <div class="kpi__head"><span class="label">Avg DWM Rate</span></div>
        <div class="value mono" :style="{ color: dwmCssColor(avgDwm) }">
          {{ avgDwm.toFixed(0) }}<span class="unit">%</span>
        </div>
        <v-progress-linear :model-value="avgDwm" :color="avgDwm > 75 ? 'success' : avgDwm > 40 ? 'warning' : 'error'" bg-color="surface-variant" height="3" class="mt-2 mb-2" />
        <div class="kpi__sub">
          <span class="mono" style="font-size:0.75rem; color:var(--text-2)">UWB positioning quality</span>
        </div>
      </div>
    </div>

    <!-- ── Status Row ──────────────────────────────────────────── -->
    <div class="status-row">
      <div class="status-card card">
        <div class="label" style="margin-bottom:8px">Manager</div>
        <div class="status-card__state">
          <div :class="['dot', managerRunning ? 'dot--online' : 'dot--error']" />
          <span class="mono" :style="{ fontSize:'0.85rem', fontWeight:600, color: managerRunning ? 'var(--green)' : 'var(--red)' }">
            {{ managerRunning ? 'Running' : 'Stopped' }}
          </span>
        </div>
        <div class="mono" style="font-size:0.7rem; color:var(--text-3); margin-top:4px">{{ fmt(managerUpdated) }}</div>
      </div>

      <div class="status-card card">
        <div class="label" style="margin-bottom:8px">Map Worker</div>
        <div class="status-card__state">
          <div :class="['dot', workerState === 'Started' ? 'dot--online' : 'dot--error']" />
          <span class="mono" :style="{ fontSize:'0.85rem', fontWeight:600, color: workerState === 'Started' ? 'var(--green)' : 'var(--red)' }">
            {{ workerState }}
          </span>
        </div>
        <div class="mono" style="font-size:0.7rem; color:var(--text-3); margin-top:4px">{{ fmt(workerUpdated) }}</div>
      </div>

      <div class="status-card card">
        <div class="label" style="margin-bottom:8px">Swarm Mode</div>
        <div class="status-card__state">
          <v-icon :icon="swarmModeIcon" size="14" :color="swarmModeColor" class="mr-1" />
          <span class="mono" style="font-size:0.85rem; font-weight:600; color:var(--text)">
            {{ swarmMode ?? 'Unknown' }}
          </span>
        </div>
        <div class="mono" style="font-size:0.7rem; color:var(--text-3); margin-top:4px">Formation / Manual</div>
      </div>

      <div class="status-card card">
        <div class="label" style="margin-bottom:8px">USB Link</div>
        <div class="status-card__state">
          <div :class="['dot', usbSuccess ? 'dot--online' : 'dot--error']" />
          <span class="mono" :style="{ fontSize:'0.85rem', fontWeight:600, color: usbSuccess ? 'var(--green)' : 'var(--red)' }">
            {{ usbSuccess ? 'Connected' : 'Offline' }}
          </span>
        </div>
        <div class="mono" style="font-size:0.7rem; color:var(--text-3); margin-top:4px">
          {{ usbPort ? `${usbPort} · ${usbBaud} baud` : 'No connection data' }}
        </div>
      </div>
    </div>

    <!-- ── Lower: Agents + Buffer/Errors ───────────────────────── -->
    <div class="lower-row">

      <!-- Agent Fleet -->
      <div class="card agent-panel">
        <div class="card-title">
          Agent Fleet
          <span class="badge" style="background:var(--accent-dim); color:var(--accent); border:1px solid var(--accent-border); margin-left:auto">{{ agents.length }}</span>
        </div>
        <div style="height:1px; background:var(--border); margin:0 0 4px" />

        <div v-if="!isLoading && !agents.length" class="empty-state" style="padding:24px 0">
          <v-icon icon="mdi-radar" size="36" style="color:var(--text-3)" class="mb-2" />
          <div style="font-size:0.78rem; color:var(--text-2)">No agents detected</div>
        </div>

        <div v-else-if="isLoading" class="pa-3">
          <v-skeleton-loader v-for="i in 3" :key="i" type="list-item-two-line" class="mb-1" />
        </div>

        <div v-else class="agent-list">
          <div v-for="agent in agents" :key="agent.agentId" class="agent-row">
            <div class="agent-row__left">
              <div :class="['dot', agentStatusDot(agent)]" />
              <span class="mono agent-id">#{{ agent.agentId }}</span>
              <span class="mono agent-pos">({{ agent.x.toFixed(2) }}, {{ agent.y.toFixed(2) }})</span>
              <div class="agent-flags">
                <span
                  v-for="f in agent.flags"
                  :key="f"
                  class="flag mono"
                  :class="f.startsWith('Stopped') ? 'flag--warn' : f === 'WaypointActive' ? 'flag--info' : ''"
                >{{ f }}</span>
              </div>
            </div>
            <div class="agent-row__right">
              <span class="mono agent-stat">
                <v-icon icon="mdi-speedometer" size="11" class="mr-1" />{{ agent.velocity.toFixed(2) }} m/s
              </span>
              <span class="mono agent-stat">
                <v-icon icon="mdi-ruler" size="11" class="mr-1" />{{ agent.frontalDistance.toFixed(2) }} m
              </span>
              <div class="agent-dwm">
                <v-progress-linear
                  :model-value="agent.dwmSuccessRate"
                  :color="agent.dwmSuccessRate > 75 ? 'success' : agent.dwmSuccessRate > 40 ? 'warning' : 'error'"
                  height="3" rounded bg-color="surface-variant" style="width:56px;flex-shrink:0"
                />
                <span class="mono" style="font-size:0.68rem; color:var(--text-2)">{{ agent.dwmSuccessRate.toFixed(0) }}%</span>
              </div>
              <TimestampChip :timestamp="agent.timestamp" />
            </div>
          </div>
        </div>
      </div>

      <!-- Right column -->
      <div class="right-col">

        <!-- Buffer -->
        <div class="card buffer-panel">
          <div class="card-title">
            Buffer
            <span v-if="buffer" class="badge" :style="{ background: bufferCssColor + '18', color: bufferCssColor, border: '1px solid ' + bufferCssColor + '30', marginLeft: 'auto' }">
              {{ buffer.success ? 'OK' : 'ERR' }}
            </span>
          </div>
          <div style="height:1px; background:var(--border); margin:0 0 12px" />

          <div class="buffer-body">
            <div class="buffer-gauge">
              <v-progress-circular :model-value="buffer?.bufferUsagePercentage ?? 0" :color="bufferColor" size="96" width="7" bg-color="surface-variant">
                <span class="mono" :style="{ fontSize: '1.05rem', fontWeight: 600, color: bufferCssColor }">
                  {{ buffer ? buffer.bufferUsagePercentage.toFixed(0) : 0 }}%
                </span>
              </v-progress-circular>

              <div class="sparkline">
                <div class="label mb-1">History ({{ bufferHistory.length }})</div>
                <div class="sparkline__bars">
                  <div
                    v-for="(val, i) in bufferHistory" :key="i"
                    class="sparkline__bar"
                    :style="{
                      height: `${Math.max(2, val * 0.32)}px`,
                      background: val >= 10 ? 'var(--red)' : val >= 5 ? 'var(--amber)' : 'var(--green)',
                      opacity: 0.3 + (i / bufferHistory.length) * 0.7,
                    }"
                  />
                </div>
              </div>
            </div>

            <div class="buffer-stats">
              <div class="buffer-stat">
                <div class="label">Used</div>
                <div class="mono buffer-stat__val">{{ buffer?.bufferCount ?? 0 }}</div>
                <v-progress-linear :model-value="buffer ? (buffer.bufferCount / buffer.bufferCapacity) * 100 : 0" :color="bufferColor" height="3" bg-color="surface-variant" class="mt-1" />
              </div>
              <div class="buffer-stat">
                <div class="label">Capacity</div>
                <div class="mono buffer-stat__val">{{ buffer?.bufferCapacity ?? 0 }}</div>
              </div>
              <div class="buffer-stat">
                <div class="label">Free</div>
                <div class="mono buffer-stat__val" style="color:var(--green)">
                  {{ buffer ? buffer.bufferCapacity - buffer.bufferCount : 0 }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Recent Errors -->
        <div class="card errors-panel">
          <div class="card-title">
            Recent Errors
            <span v-if="recentErrors.length" class="badge" style="background:var(--red-dim); color:var(--red); border:1px solid var(--red-border); margin-left:auto">{{ recentErrors.length }}</span>
          </div>
          <div style="height:1px; background:var(--border); margin:0 0 8px" />

          <div v-if="!recentErrors.length" class="empty-state" style="padding:16px 0">
            <v-icon icon="mdi-check-circle-outline" size="26" style="color:var(--green)" class="mb-1" />
            <div style="font-size:0.75rem; color:var(--text-2)">No errors reported</div>
          </div>

          <div v-else class="error-list">
            <div v-for="(err, i) in recentErrors" :key="i" class="error-row">
              <v-icon icon="mdi-alert-circle-outline" size="13" style="color:var(--red);flex-shrink:0" />
              <div style="flex:1;min-width:0">
                <div class="mono error-msg">{{ err.message }}</div>
                <div class="mono error-meta">{{ err.source }} · {{ fmt(err.timestamp) }}</div>
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>

  </div>
</template>

<style scoped>
.dash {
  padding: 24px;
  min-height: 100%;
  background: var(--bg);
  display: flex;
  flex-direction: column;
  gap: 16px;
}


/* ── KPI ───────────────────────────────────────────────────────── */
.kpi-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
}
@media (max-width: 1100px) { .kpi-row { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 600px)  { .kpi-row { grid-template-columns: 1fr; } }

.kpi { padding: 16px; }
.kpi__head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; }
.kpi__sub { display: flex; align-items: center; gap: 6px; margin-top: 4px; }

/* ── Status row ────────────────────────────────────────────────── */
.status-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
}
@media (max-width: 1100px) { .status-row { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 600px)  { .status-row { grid-template-columns: 1fr; } }

.status-card { padding: 14px 16px; }
.status-card__state { display: flex; align-items: center; gap: 7px; }

/* ── Lower row ─────────────────────────────────────────────────── */
.lower-row {
  display: grid;
  grid-template-columns: 1fr 360px;
  gap: 12px;
  flex: 1;
}
@media (max-width: 1100px) { .lower-row { grid-template-columns: 1fr; } }

/* ── Agent panel ───────────────────────────────────────────────── */
.agent-panel { display: flex; flex-direction: column; overflow: hidden; }
.agent-list { overflow-y: auto; max-height: 400px; padding: 4px 0; }

.agent-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 16px;
  border-bottom: 1px solid var(--border);
  transition: background 0.15s;
  gap: 12px;
}
.agent-row:hover { background: var(--surface-hover); }

.agent-row__left { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.agent-row__right { display: flex; align-items: center; gap: 12px; flex-shrink: 0; }

.agent-id { font-size: 0.85rem; font-weight: 600; color: var(--accent); }
.agent-pos { font-size: 0.75rem; color: var(--text-2); }
.agent-flags { display: flex; gap: 4px; flex-wrap: wrap; }

.agent-stat {
  font-size: 0.75rem;
  color: var(--text-2);
  display: flex;
  align-items: center;
  white-space: nowrap;
}

.agent-dwm { display: flex; align-items: center; gap: 5px; }

/* ── Right col ─────────────────────────────────────────────────── */
.right-col { display: flex; flex-direction: column; gap: 12px; }

/* ── Buffer ────────────────────────────────────────────────────── */
.buffer-panel { flex-shrink: 0; }
.buffer-body { display: flex; gap: 16px; align-items: flex-start; padding: 0 16px 14px; }
.buffer-gauge { display: flex; flex-direction: column; align-items: center; gap: 10px; flex-shrink: 0; }

.sparkline { width: 100%; }
.sparkline__bars { display: flex; align-items: flex-end; height: 28px; gap: 2px; overflow: hidden; }
.sparkline__bar { flex: 1; border-radius: 1px; min-height: 2px; }

.buffer-stats { display: flex; flex-direction: column; gap: 10px; flex: 1; padding-top: 4px; }
.buffer-stat__val { font-size: 1.35rem; font-weight: 600; color: var(--text); line-height: 1.1; }

/* ── Errors ────────────────────────────────────────────────────── */
.errors-panel { flex: 1; }
.error-list { padding: 0 4px 8px; }
.error-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 7px 12px;
  border-bottom: 1px solid rgba(248, 113, 113, 0.06);
}
.error-msg { font-size: 0.78rem; color: var(--red); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.error-meta { font-size: 0.68rem; color: var(--text-3); margin-top: 2px; }
</style>
