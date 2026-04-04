<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { Component } from 'vue'
import { storeToRefs } from 'pinia'
import { EVENT_TYPES, type EventType } from '@/models/models'
import { useEventStore, TYPE_META } from '@/stores/eventStore'
import { subscribeToTopic, unsubscribeFromTopic } from '@/plugins/signalr'
import {
  EventCard,
  AgentDataCard, AgentRegistrationCard, BufferInformationCard,
  CommandAssignedCard, CommandClearedCard, CommandDispatchedCard,
  CommandGeneratedCard, DriveControlErrorCard,
  MapControlErrorCard, MapUpdatedCard, UsbStartedCard,
} from '@/components/eventlog'

const store = useEventStore()
const { activeSubscriptions, displayTypes, isPaused, connectionState, eventCounts, allEvents } = storeToRefs(store)

const CARD: Partial<Record<EventType, Component>> = {
  [EVENT_TYPES.COMMAND_GENERATED]:   CommandGeneratedCard,
  [EVENT_TYPES.COMMAND_ASSIGNED]:    CommandAssignedCard,
  [EVENT_TYPES.COMMAND_DISPATCHED]:  CommandDispatchedCard,
  [EVENT_TYPES.COMMAND_CLEARED]:     CommandClearedCard,
  [EVENT_TYPES.AGENT_REGISTRATION]:  AgentRegistrationCard,
  [EVENT_TYPES.DRIVE_CONTROL_ERROR]: DriveControlErrorCard,
  [EVENT_TYPES.AGENT_DATA]:          AgentDataCard,
  [EVENT_TYPES.MAP_UPDATED]:         MapUpdatedCard,
  [EVENT_TYPES.BUFFER_INFORMATION]:  BufferInformationCard,
  [EVENT_TYPES.USB_STARTED]:         UsbStartedCard,
  [EVENT_TYPES.MAP_CONTROL_ERROR]:   MapControlErrorCard,
}

const CATEGORIES = [
  { key: 'drivecontrol', label: 'Drive Control' },
  { key: 'mapcontrol',   label: 'Map Control'   },
  { key: 'system',       label: 'System'        },
] as const

const typesByCategory = (cat: string) =>
  (Object.entries(TYPE_META) as [EventType, typeof TYPE_META[EventType]][])
    .filter(([, m]) => m.category === cat)

const toggleSubscription = async (type: EventType) => {
  if (activeSubscriptions.value.has(type)) {
    await unsubscribeFromTopic(type)
    store.unsubscribe(type)
  } else {
    await subscribeToTopic(type)
    store.subscribe(type)
  }
}

const subscribeAll = async () => {
  const types = Object.values(EVENT_TYPES) as EventType[]
  await Promise.all(
    types
      .filter(t => !activeSubscriptions.value.has(t))
      .map(t => subscribeToTopic(t).then(() => store.subscribe(t)))
  )
}

const unsubscribeAll = async () => {
  await Promise.all(
    [...activeSubscriptions.value].map(t => unsubscribeFromTopic(t))
  )
  store.unsubscribeAll()
}

const toggleDisplayType = (type: EventType) => {
  const next = new Set(displayTypes.value)
  if (next.has(type)) next.delete(type)
  else next.add(type)
  displayTypes.value = next
}

const selectAllDisplay  = () => { displayTypes.value = new Set(Object.values(EVENT_TYPES) as EventType[]) }
const selectNoneDisplay = () => { displayTypes.value = new Set() }

const panelOpen = ref(false)
const searchText = ref('')

const filteredEvents = computed(() => {
  let events = allEvents.value.filter(e => displayTypes.value.has(e.type as EventType))
  const q = searchText.value.trim().toLowerCase()
  if (q) events = events.filter(e => JSON.stringify(e.payload).toLowerCase().includes(q))
  return events
})

const connChip = computed(() => ({
  color: connectionState.value === 'connected' ? 'success' : 'error',
  icon:  connectionState.value === 'connected' ? 'mdi-lan-connect' : 'mdi-lan-disconnect',
  text:  connectionState.value === 'connected' ? 'Connected' : 'Disconnected',
}))
</script>

<template>
  <div class="el">

    <!-- ── Header ──────────────────────────────────────────────── -->
    <div class="el__top">
      <div class="el__conn" :class="connectionState === 'connected' ? 'el__conn--on' : 'el__conn--off'">
        <div :class="['dot', connectionState === 'connected' ? 'dot--online' : 'dot--error']" />
        <span>{{ connChip.text }}</span>
      </div>
      <span class="mono el__total">{{ filteredEvents.length }} events</span>
      <div style="flex:1" />
      <button class="el__settings-btn" :class="{ active: panelOpen }" @click="panelOpen = !panelOpen">
        <v-icon icon="mdi-tune" size="14" />
        {{ panelOpen ? 'Hide' : 'Settings' }}
      </button>
    </div>

    <!-- ── Settings Panel ──────────────────────────────────────── -->
    <div v-if="panelOpen" class="card el__panel">
      <div class="panel-section">
        <div class="panel-head">
          <span class="label">Backend Subscriptions</span>
          <span class="panel-hint">controls which events the server sends</span>
          <div style="margin-left:auto; display:flex; gap:6px">
            <button class="mini-btn" @click="subscribeAll()">All</button>
            <button class="mini-btn" @click="unsubscribeAll()">None</button>
          </div>
        </div>
        <div v-for="cat in CATEGORIES" :key="cat.key" class="sub-cat">
          <div class="sub-cat__label">{{ cat.label }}</div>
          <div class="sub-cat__tiles">
            <button
              v-for="[type, meta] in typesByCategory(cat.key)" :key="type"
              class="sub-tile"
              :class="{ active: activeSubscriptions.has(type) }"
              :style="activeSubscriptions.has(type) ? { borderColor: meta.accentColor + '40', background: meta.accentColor + '12' } : {}"
              @click="toggleSubscription(type)"
            >
              <v-icon :icon="meta.icon" size="12" :color="activeSubscriptions.has(type) ? meta.accentColor : undefined" />
              <span>{{ meta.label }}</span>
              <span class="sub-tile__count" :style="{ color: activeSubscriptions.has(type) ? meta.accentColor : undefined }">{{ eventCounts[type] }}</span>
            </button>
          </div>
        </div>
      </div>

      <div style="height:1px; background:var(--border)" />

      <div class="panel-section">
        <div class="panel-head">
          <span class="label">Show in Stream</span>
          <span class="panel-hint">client-side display filter</span>
          <div style="margin-left:auto; display:flex; gap:6px">
            <button class="mini-btn" @click="selectAllDisplay()">All</button>
            <button class="mini-btn" @click="selectNoneDisplay()">None</button>
          </div>
        </div>
        <div class="display-checks">
          <label
            v-for="[type, meta] in (Object.entries(TYPE_META) as [EventType, typeof TYPE_META[EventType]][])"
            :key="type"
            class="check-label"
            :style="displayTypes.has(type) ? { color: meta.accentColor } : {}"
          >
            <input type="checkbox" :checked="displayTypes.has(type)" @change="toggleDisplayType(type)" class="check-input" :style="{ accentColor: meta.accentColor }" />
            <v-icon :icon="meta.icon" size="12" />
            {{ meta.label }}
          </label>
        </div>
      </div>
    </div>

    <!-- ── Toolbar ─────────────────────────────────────────────── -->
    <div class="el__toolbar">
      <button class="tool-btn" :class="{ 'tool-btn--active': isPaused }" @click="store.isPaused = !store.isPaused">
        <v-icon :icon="isPaused ? 'mdi-play' : 'mdi-pause'" size="14" />
        {{ isPaused ? 'Resume' : 'Pause' }}
      </button>
      <button class="tool-btn" @click="store.clearAll()">
        <v-icon icon="mdi-delete-sweep" size="14" />
        Clear
      </button>
      <div class="tool-search">
        <v-icon icon="mdi-magnify" size="14" class="tool-search__icon" />
        <input v-model="searchText" type="text" placeholder="Search events…" class="tool-search__input" />
        <button v-if="searchText" class="tool-search__clear" @click="searchText = ''">×</button>
      </div>
    </div>

    <!-- ── Stream ──────────────────────────────────────────────── -->
    <div class="el__stream">
      <div v-if="activeSubscriptions.size === 0" class="empty-state">
        <v-icon icon="mdi-broadcast-off" size="32" class="mb-2" />
        <div style="font-size:0.82rem">No active subscriptions.</div>
        <div style="font-size:0.72rem; color:var(--text-3)">Open Settings above and subscribe to event types.</div>
      </div>
      <div v-else-if="filteredEvents.length === 0" class="empty-state">
        <v-icon icon="mdi-inbox" size="32" class="mb-2" />
        <div style="font-size:0.82rem">No events yet{{ searchText ? ' matching filter' : '' }}.</div>
      </div>
      <template v-else>
        <template v-for="entry in filteredEvents" :key="entry.id">
          <component
            v-if="CARD[entry.type as EventType]"
            :is="CARD[entry.type as EventType]"
            :event="entry.payload"
          />
          <EventCard
            v-else
            :title="TYPE_META[entry.type as EventType]?.label ?? entry.type"
            :icon="TYPE_META[entry.type as EventType]?.icon ?? 'mdi-information-outline'"
            :accent-color="TYPE_META[entry.type as EventType]?.accentColor ?? '#888'"
            :timestamp="(entry.payload as unknown as Record<string, string>).timestamp ?? entry.timestamp"
          >
            <template #header>
              <span
                v-for="[k, v] in Object.entries(entry.payload as object).filter(([k]) => k !== 'timestamp')"
                :key="k"
                class="ec-badge dim"
              >{{ k }}: {{ v }}</span>
            </template>
          </EventCard>
        </template>
      </template>
    </div>

  </div>
</template>

<style scoped>
.el {
  background: var(--bg);
  min-height: 100%;
  padding: 24px;
  font-family: var(--font);
  color: var(--text);
  display: flex;
  flex-direction: column;
  gap: 14px;
}

/* ── Header ────────────────────────────────────────────────────── */
.el__top { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.el__conn {
  display: flex; align-items: center; gap: 7px;
  font-family: var(--font-mono); font-size: 0.78rem; font-weight: 500;
  padding: 4px 10px; border-radius: var(--radius-sm);
  border: 1px solid var(--border);
}
.el__conn--on  { color: var(--green); border-color: var(--green-border); background: var(--green-dim); }
.el__conn--off { color: var(--red); border-color: var(--red-border); background: var(--red-dim); }
.el__total { font-size: 0.75rem; color: var(--text-3); }

.el__settings-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 6px 14px; font-size: 0.75rem; font-family: var(--font);
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius);
  color: var(--text-2); cursor: pointer; transition: all 0.15s;
}
.el__settings-btn:hover, .el__settings-btn.active { color: var(--text); border-color: var(--border-hi); }

/* ── Panel ─────────────────────────────────────────────────────── */
.el__panel { padding: 16px 18px; display: flex; flex-direction: column; gap: 16px; }
.panel-section { display: flex; flex-direction: column; gap: 10px; }
.panel-head { display: flex; align-items: center; gap: 8px; }
.panel-hint { font-size: 0.68rem; color: var(--text-3); }

.mini-btn {
  padding: 3px 10px; font-size: 0.72rem; font-family: var(--font-mono);
  background: var(--surface-raised); border: 1px solid var(--border); border-radius: var(--radius-sm);
  color: var(--text-2); cursor: pointer; transition: all 0.15s;
}
.mini-btn:hover { color: var(--accent); border-color: var(--accent-border); }

/* ── Subscription tiles ────────────────────────────────────────── */
.sub-cat { display: flex; flex-direction: column; gap: 6px; }
.sub-cat__label { font-size: 0.62rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--text-3); }
.sub-cat__tiles { display: flex; flex-wrap: wrap; gap: 6px; }

.sub-tile {
  display: flex; align-items: center; gap: 6px; padding: 4px 10px;
  border-radius: var(--radius-sm); border: 1px solid var(--border);
  background: var(--surface-raised); color: var(--text-2); cursor: pointer;
  font-family: var(--font-mono); font-size: 0.72rem;
  transition: all 0.15s; user-select: none;
}
.sub-tile:hover { color: var(--text); border-color: var(--border-hi); }
.sub-tile.active { color: var(--text); }
.sub-tile__count { font-weight: 700; min-width: 14px; text-align: right; }

/* ── Display checkboxes ────────────────────────────────────────── */
.display-checks { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 6px 14px; }
.check-label {
  display: flex; align-items: center; gap: 7px; font-size: 0.78rem;
  color: var(--text-2); cursor: pointer; user-select: none; transition: color 0.15s; white-space: nowrap;
}
.check-label:hover { color: var(--text) !important; }
.check-input { width: 13px; height: 13px; cursor: pointer; flex-shrink: 0; }

/* ── Toolbar ───────────────────────────────────────────────────── */
.el__toolbar {
  display: flex; align-items: center; gap: 8px; flex-wrap: wrap;
  padding: 8px 12px;
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius);
}

.tool-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 5px 12px; font-size: 0.75rem; font-family: var(--font-mono);
  background: transparent; border: 1px solid var(--border); border-radius: var(--radius-sm);
  color: var(--text-2); cursor: pointer; transition: all 0.15s;
}
.tool-btn:hover { color: var(--text); border-color: var(--border-hi); background: var(--surface-hover); }
.tool-btn--active { background: var(--red-dim); border-color: var(--red-border); color: var(--red); }

.tool-search { position: relative; display: flex; align-items: center; flex: 1; min-width: 180px; max-width: 360px; }
.tool-search__icon { position: absolute; left: 10px; color: var(--text-3); pointer-events: none; }
.tool-search__input {
  width: 100%; padding: 5px 30px 5px 28px; font-size: 0.78rem; font-family: var(--font-mono);
  background: transparent; border: 1px solid var(--border); border-radius: var(--radius-sm);
  color: var(--text); outline: none; transition: border-color 0.15s;
}
.tool-search__input:focus { border-color: var(--border-hi); }
.tool-search__input::placeholder { color: var(--text-3); }
.tool-search__clear { position: absolute; right: 8px; background: none; border: none; color: var(--text-3); cursor: pointer; font-size: 15px; }

/* ── Stream ────────────────────────────────────────────────────── */
.el__stream { flex: 1; display: flex; flex-direction: column; }
</style>
