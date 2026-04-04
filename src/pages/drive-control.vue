<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { CommandGeneratedDto, CommandAssignedDto, CommandDispatchedDto, CommandClearedDto, DriveControlErrorDto, DriveCommandDto } from '@/models/models'
import { connection, subscribeToTopic, unsubscribeFromTopic } from '@/plugins/signalr'
import { apiUrl } from '@/config'

interface CommandState {
  agentId:           number
  currentCommand:    DriveCommandDto | null
  phase:             number
  generatedAt:       string
  assignedAt:        string | null
  dispatchedAt:      string | null
  dispatchLatencyMs: number | null
  lastUpdated:       string
}

interface AgentError {
  agentId:          number
  message:          string
  source:           string
  sourceContext:    string
  exceptionMessage: string
  timestamp:        string
}

const PHASES = [
  { key: 'Generated',  label: 'Generated',  value: 0 },
  { key: 'Assigned',   label: 'Assigned',   value: 1 },
  { key: 'Dispatched', label: 'Dispatched', value: 2 },
  { key: 'Cleared',    label: 'Cleared',    value: 4 },
] as const

const PHASE_LABELS = ['Generated', 'Assigned', 'Dispatched', 'Completed', 'Cleared']
const TOPICS = ['CommandGenerated', 'CommandAssigned', 'CommandDispatched', 'CommandCleared', 'DriveControlError']

const commandMap = ref<Map<number, CommandState>>(new Map())
const errorMap   = ref<Map<number, AgentError>>(new Map())
const isLoading  = ref(true)

const fetchInitialData = async () => {
  isLoading.value = true
  try {
    const [cmdRes, errRes] = await Promise.all([
      fetch(apiUrl('/api/cache/commands')),
      fetch(apiUrl('/api/cache/errors')),
    ])
    if (cmdRes.ok) {
      const data: CommandState[] = await cmdRes.json()
      commandMap.value = new Map(data.map(s => [s.agentId, s]))
    }
    if (errRes.ok) {
      const data: AgentError[] = await errRes.json()
      errorMap.value = new Map(data.map(e => [e.agentId, e]))
    }
  } catch (e) {
    console.error('Failed to fetch drive control data', e)
  } finally {
    isLoading.value = false
  }
}

const handleGenerated = (dto: CommandGeneratedDto) => {
  commandMap.value = new Map(commandMap.value).set(dto.agentId, {
    agentId: dto.agentId, currentCommand: dto.command, phase: 0,
    generatedAt: dto.generatedAt as unknown as string,
    assignedAt: null, dispatchedAt: null, dispatchLatencyMs: null,
    lastUpdated: new Date().toISOString(),
  })
}

const handleAssigned = (dto: CommandAssignedDto) => {
  const existing = commandMap.value.get(dto.agentId)
  if (!existing) return
  commandMap.value = new Map(commandMap.value).set(dto.agentId, {
    ...existing, phase: 1, assignedAt: dto.assignedAt as unknown as string, lastUpdated: new Date().toISOString(),
  })
}

const handleDispatched = (dto: CommandDispatchedDto) => {
  const existing = commandMap.value.get(dto.agentId)
  if (!existing) return
  commandMap.value = new Map(commandMap.value).set(dto.agentId, {
    ...existing, phase: 2, dispatchedAt: dto.dispatchedAt as unknown as string,
    dispatchLatencyMs: dto.dispatchLatencyMs, lastUpdated: new Date().toISOString(),
  })
}

const handleCleared = (dto: CommandClearedDto) => {
  const existing = commandMap.value.get(dto.agentId)
  if (!existing) return
  commandMap.value = new Map(commandMap.value).set(dto.agentId, {
    ...existing, phase: 4, currentCommand: null, lastUpdated: dto.timestamp as unknown as string,
  })
}

const handleError = (dto: DriveControlErrorDto) => {
  errorMap.value = new Map(errorMap.value).set(dto.agentId, {
    agentId: dto.agentId, message: dto.message, source: dto.source,
    sourceContext: dto.sourceContext, exceptionMessage: dto.exceptionMessage,
    timestamp: dto.timestamp as unknown as string,
  })
}

onMounted(async () => {
  await fetchInitialData()
  await Promise.all(TOPICS.map(t => subscribeToTopic(t)))
  connection.on('CommandGenerated',  handleGenerated)
  connection.on('CommandAssigned',   handleAssigned)
  connection.on('CommandDispatched', handleDispatched)
  connection.on('CommandCleared',    handleCleared)
  connection.on('DriveControlError', handleError)
})

onUnmounted(() => {
  TOPICS.forEach(t => unsubscribeFromTopic(t))
  connection.off('CommandGenerated',  handleGenerated)
  connection.off('CommandAssigned',   handleAssigned)
  connection.off('CommandDispatched', handleDispatched)
  connection.off('CommandCleared',    handleCleared)
  connection.off('DriveControlError', handleError)
})

const sortedAgents = computed(() => [...commandMap.value.values()].sort((a, b) => a.agentId - b.agentId))

const phaseCounts = computed(() => {
  const counts = { 0: 0, 1: 0, 2: 0, 4: 0, errors: 0 }
  for (const s of commandMap.value.values()) {
    const key = s.phase as 0 | 1 | 2 | 4
    if (key in counts) counts[key]++
  }
  counts.errors = errorMap.value.size
  return counts
})

const fmt = (ts: string | null | undefined): string => {
  if (!ts) return '—'
  try { return new Date(ts).toLocaleTimeString('en', { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' }) } catch { return '—' }
}

const getPhaseTime = (s: CommandState, key: string): string => {
  if (key === 'Generated')  return fmt(s.generatedAt)
  if (key === 'Assigned')   return fmt(s.assignedAt)
  if (key === 'Dispatched') return fmt(s.dispatchedAt)
  if (key === 'Cleared')    return s.phase === 4 ? fmt(s.lastUpdated) : '—'
  return '—'
}

const stepState = (agentPhase: number, stepValue: number): 'done' | 'active' | 'pending' => {
  if (agentPhase > stepValue) return 'done'
  if (agentPhase === stepValue) return 'active'
  return 'pending'
}

const latencyColor = (ms: number | null): string => {
  if (ms === null) return 'var(--text-3)'
  if (ms < 50) return 'var(--green)'
  if (ms < 200) return 'var(--amber)'
  return 'var(--red)'
}

const phaseColor = (phase: number): string => {
  if (phase === 4) return 'var(--text-3)'
  if (phase === 2) return 'var(--green)'
  if (phase === 1) return 'var(--accent)'
  return 'var(--amber)'
}
</script>

<template>
  <div class="dc">

    <div class="dc__top">

      <div class="dc__chips">
        <div class="dc__chip" title="Generated">
          <span class="dc__dot" style="background:var(--amber)" /> Gen <span class="dc__chip-n">{{ phaseCounts[0] }}</span>
        </div>
        <div class="dc__chip" title="Assigned">
          <span class="dc__dot" style="background:var(--accent)" /> Asgn <span class="dc__chip-n">{{ phaseCounts[1] }}</span>
        </div>
        <div class="dc__chip" title="Dispatched">
          <span class="dc__dot" style="background:var(--green)" /> Disp <span class="dc__chip-n">{{ phaseCounts[2] }}</span>
        </div>
        <div class="dc__chip" title="Cleared">
          <span class="dc__dot" style="background:var(--text-3)" /> Clrd <span class="dc__chip-n">{{ phaseCounts[4] }}</span>
        </div>
        <div v-if="phaseCounts.errors > 0" class="dc__chip dc__chip--err" title="Errors">
          <v-icon icon="mdi-alert-circle" size="11" /> <span class="dc__chip-n">{{ phaseCounts.errors }}</span>
        </div>
      </div>

      <span class="mono dc__count">{{ sortedAgents.length }} agent{{ sortedAgents.length !== 1 ? 's' : '' }}</span>
    </div>

    <div v-if="isLoading" class="dc__center">
      <v-progress-circular indeterminate size="28" width="3" color="primary" />
    </div>

    <div v-else-if="sortedAgents.length === 0" class="dc__center">
      <v-icon icon="mdi-robot-off" size="36" style="color:var(--text-3)" class="mb-3" />
      <div style="color:var(--text-2); font-size:0.82rem">No command data available.</div>
    </div>

    <div v-else class="dc__cards">
      <div
        v-for="state in sortedAgents" :key="state.agentId"
        class="card dc-card"
        :class="{ 'dc-card--active': state.phase < 4, 'dc-card--err': errorMap.has(state.agentId) }"
      >
        <!-- Header -->
        <div class="dc-card__head">
          <span class="mono dc-card__id">#{{ state.agentId }}</span>
          <span class="badge dc-card__phase" :style="{ background: phaseColor(state.phase) + '18', color: phaseColor(state.phase), border: '1px solid ' + phaseColor(state.phase) + '30' }">
            {{ PHASE_LABELS[state.phase] }}
          </span>
          <span style="flex:1" />
          <span class="mono dc-card__ts">{{ fmt(state.lastUpdated) }}</span>
          <span v-if="errorMap.has(state.agentId)" class="dc-card__err-badge">Error</span>
        </div>

        <!-- Timeline -->
        <div class="tl">
          <div v-for="(phase, i) in PHASES" :key="phase.key" class="tl__step" :class="`tl--${stepState(state.phase, phase.value)}`">
            <div v-if="i > 0" class="tl__line" :class="{ 'tl__line--done': state.phase >= phase.value }" />
            <div class="tl__dot-wrap">
              <div class="tl__dot" :style="{
                background: stepState(state.phase, phase.value) === 'pending' ? 'var(--surface-raised)' :
                            stepState(state.phase, phase.value) === 'active' ? phaseColor(state.phase) : 'var(--green)'
              }" />
              <div v-if="stepState(state.phase, phase.value) === 'active' && state.phase !== 4" class="tl__pulse" :style="{ background: phaseColor(state.phase) }" />
            </div>
            <div class="tl__content">
              <div class="tl__label">{{ phase.label }}</div>
              <div class="tl__time mono">{{ getPhaseTime(state, phase.key) }}</div>
              <div v-if="phase.key === 'Dispatched' && state.dispatchLatencyMs !== null && state.phase >= 2"
                class="tl__latency mono" :style="{ color: latencyColor(state.dispatchLatencyMs) }">
                {{ state.dispatchLatencyMs!.toFixed(1) }}ms
              </div>
            </div>
          </div>
        </div>

        <!-- Command details -->
        <div v-if="state.currentCommand" class="dc-cmd">
          <div class="dc-cmd__row">
            <span class="dc-cmd__key">Waypoints</span>
            <div class="dc-cmd__wps">
              <span v-for="(wp, i) in state.currentCommand.waypoints.slice(0, 6)" :key="i" class="dc-cmd__wp mono">({{ wp.x }}, {{ wp.y }})</span>
              <span v-if="state.currentCommand.waypoints.length > 6" class="dc-cmd__wp dc-cmd__wp--more mono">+{{ state.currentCommand.waypoints.length - 6 }}</span>
            </div>
          </div>
          <div v-if="state.currentCommand.driveFlags.length" class="dc-cmd__row">
            <span class="dc-cmd__key">Flags</span>
            <div class="dc-cmd__wps">
              <span v-for="f in state.currentCommand.driveFlags" :key="f" class="flag flag--info mono">{{ f }}</span>
            </div>
          </div>
        </div>

        <!-- Error -->
        <div v-if="errorMap.has(state.agentId)" class="dc-err">
          <div class="dc-err__row">
            <v-icon icon="mdi-alert-octagon" size="13" color="error" style="flex-shrink:0" />
            <span class="mono dc-err__msg">{{ errorMap.get(state.agentId)!.message }}</span>
          </div>
          <div class="dc-err__meta mono">
            {{ errorMap.get(state.agentId)!.source }} · {{ errorMap.get(state.agentId)!.sourceContext }} · {{ fmt(errorMap.get(state.agentId)!.timestamp) }}
          </div>
          <div v-if="errorMap.get(state.agentId)!.exceptionMessage" class="dc-err__exc mono">
            {{ errorMap.get(state.agentId)!.exceptionMessage }}
          </div>
        </div>

      </div>
    </div>

  </div>
</template>

<style scoped>
.dc {
  background: var(--bg);
  min-height: 100%;
  padding: 24px;
  font-family: var(--font);
  color: var(--text);
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.dc__top { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; }
.dc__count { font-size: 0.75rem; color: var(--text-3); margin-left: auto; }

.dc__chips { display: flex; gap: 6px; flex-wrap: wrap; }
.dc__chip {
  display: flex; align-items: center; gap: 5px; padding: 4px 10px;
  border-radius: var(--radius-sm); background: var(--surface); border: 1px solid var(--border);
  font-family: var(--font-mono); font-size: 0.72rem; color: var(--text-2);
}
.dc__chip--err { color: var(--red); border-color: var(--red-border); background: var(--red-dim); }
.dc__dot { width: 7px; height: 7px; border-radius: 50%; flex-shrink: 0; }
.dc__chip-n { font-weight: 700; color: var(--text); min-width: 12px; text-align: center; }

.dc__center { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 64px 0; }

.dc__cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(480px, 1fr)); gap: 12px; }

/* ── Card ──────────────────────────────────────────────────────── */
.dc-card { overflow: hidden; display: flex; flex-direction: column; }
.dc-card--active { border-left: 2px solid var(--green); }
.dc-card--err { border-top: 2px solid var(--red-border); }

.dc-card__head {
  display: flex; align-items: center; gap: 10px; padding: 10px 16px;
  border-bottom: 1px solid var(--border); background: var(--surface-raised);
}
.dc-card__id { font-size: 0.92rem; font-weight: 600; color: var(--accent); min-width: 36px; }
.dc-card__phase { font-size: 0.68rem; }
.dc-card__ts { font-size: 0.68rem; color: var(--text-3); }
.dc-card__err-badge {
  font-family: var(--font-mono); font-size: 0.65rem; color: var(--red);
  padding: 2px 8px; background: var(--red-dim); border: 1px solid var(--red-border); border-radius: var(--radius-sm);
}

/* ── Timeline ──────────────────────────────────────────────────── */
.tl { display: flex; padding: 18px 20px 14px; background: rgba(12, 14, 20, 0.5); }
.tl__step { flex: 1; display: flex; flex-direction: column; align-items: center; position: relative; }

.tl__line { position: absolute; top: 6px; right: 50%; width: 100%; height: 2px; background: var(--border); transition: background 0.3s; }
.tl__line--done { background: rgba(52, 211, 153, 0.35); }

.tl__dot-wrap { position: relative; z-index: 1; }
.tl__dot { width: 13px; height: 13px; border-radius: 50%; transition: background 0.3s; position: relative; z-index: 2; }

.tl__pulse {
  position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);
  width: 24px; height: 24px; border-radius: 50%; opacity: 0.3;
  animation: pulse 1.8s ease-out infinite; z-index: 1;
}
@keyframes pulse {
  0%   { transform: translate(-50%,-50%) scale(0.5); opacity: 0.4; }
  100% { transform: translate(-50%,-50%) scale(1.8); opacity: 0; }
}

.tl__content { margin-top: 8px; display: flex; flex-direction: column; align-items: center; gap: 2px; }
.tl__label { font-size: 0.62rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em; }
.tl__time { font-size: 0.68rem; color: var(--text-3); }

.tl--active .tl__label  { color: var(--text); }
.tl--done .tl__label    { color: var(--text-2); }
.tl--pending .tl__label { color: var(--text-3); }

.tl__latency { font-size: 0.65rem; font-weight: 700; margin-top: 2px; }

/* ── Command details ───────────────────────────────────────────── */
.dc-cmd { padding: 10px 16px; display: flex; flex-direction: column; gap: 6px; border-top: 1px solid var(--border); }
.dc-cmd__row { display: flex; align-items: flex-start; gap: 10px; }
.dc-cmd__key { font-size: 0.62rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em; color: var(--text-3); min-width: 68px; padding-top: 3px; }
.dc-cmd__wps { display: flex; flex-wrap: wrap; gap: 4px; }
.dc-cmd__wp {
  padding: 2px 7px; font-size: 0.72rem;
  background: var(--green-dim); border: 1px solid var(--green-border); border-radius: var(--radius-sm); color: var(--green);
}
.dc-cmd__wp--more { background: rgba(255,255,255,0.03); border-color: var(--border); color: var(--text-2); }

/* ── Error panel ───────────────────────────────────────────────── */
.dc-err { padding: 10px 16px; background: var(--red-dim); border-top: 1px solid var(--red-border); display: flex; flex-direction: column; gap: 4px; }
.dc-err__row { display: flex; align-items: flex-start; gap: 7px; }
.dc-err__msg { font-size: 0.78rem; color: var(--red); word-break: break-word; line-height: 1.4; }
.dc-err__meta { font-size: 0.68rem; color: var(--text-2); }
.dc-err__exc { font-size: 0.68rem; color: rgba(248,113,113,0.6); font-style: italic; word-break: break-word; }
</style>
