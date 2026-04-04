/**
 * Pinia Event Store — pub/sub model with per-type storage and throttling
 */
import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import {
  EVENT_TYPES,
  type EventLogEntry,
  type EventPayload,
  type EventPayloadByType,
  type EventType,
} from '@/models/models'

export interface TypeMeta {
  label: string
  icon: string
  accentColor: string
  capacity: number
  throttleMs: number
  category: 'drivecontrol' | 'mapcontrol' | 'system'
}

export const TYPE_META: Record<EventType, TypeMeta> = {
  [EVENT_TYPES.COMMAND_GENERATED]:   { label: 'Cmd Generated',  icon: 'mdi-robot-outline',         accentColor: '#60a5fa', capacity: 50, throttleMs: 0,    category: 'drivecontrol' },
  [EVENT_TYPES.COMMAND_ASSIGNED]:    { label: 'Cmd Assigned',   icon: 'mdi-account-arrow-right',    accentColor: '#60a5fa', capacity: 50, throttleMs: 0,    category: 'drivecontrol' },
  [EVENT_TYPES.COMMAND_DISPATCHED]:  { label: 'Cmd Dispatched', icon: 'mdi-send-check-outline',     accentColor: '#34d399', capacity: 50, throttleMs: 0,    category: 'drivecontrol' },
  [EVENT_TYPES.COMMAND_CLEARED]:     { label: 'Cmd Cleared',    icon: 'mdi-broom',                  accentColor: '#60a5fa', capacity: 50, throttleMs: 0,    category: 'drivecontrol' },
  [EVENT_TYPES.AGENT_REGISTRATION]:  { label: 'Registration',   icon: 'mdi-badge-account',          accentColor: '#a78bfa', capacity: 50, throttleMs: 0,    category: 'drivecontrol' },
  [EVENT_TYPES.DRIVE_CONTROL_ERROR]: { label: 'DC Error',       icon: 'mdi-alert-octagon',          accentColor: '#f87171', capacity: 50, throttleMs: 0,    category: 'drivecontrol' },
  [EVENT_TYPES.AGENT_DATA]:          { label: 'Agent Data',     icon: 'mdi-robot',                  accentColor: '#34d399', capacity: 30, throttleMs: 500,  category: 'mapcontrol'   },
  [EVENT_TYPES.MAP_UPDATED]:         { label: 'Map Updated',    icon: 'mdi-map',                    accentColor: '#34d399', capacity: 30, throttleMs: 200,  category: 'mapcontrol'   },
  [EVENT_TYPES.BUFFER_INFORMATION]:  { label: 'Buffer Info',    icon: 'mdi-database',               accentColor: '#22d3ee', capacity: 30, throttleMs: 1000, category: 'mapcontrol'   },
  [EVENT_TYPES.USB_STARTED]:         { label: 'USB Started',    icon: 'mdi-usb',                    accentColor: '#22d3ee', capacity: 30, throttleMs: 0,    category: 'system'       },
  [EVENT_TYPES.MAP_CONTROL_ERROR]:   { label: 'MC Error',       icon: 'mdi-alert-circle-outline',   accentColor: '#f87171', capacity: 50, throttleMs: 0,    category: 'mapcontrol'   },
  [EVENT_TYPES.MANAGER_STATE]:       { label: 'Manager State',  icon: 'mdi-cog-outline',            accentColor: '#a78bfa', capacity: 30, throttleMs: 200,  category: 'system'       },
  [EVENT_TYPES.WORKER_STATE]:        { label: 'Worker State',   icon: 'mdi-account-hard-hat',       accentColor: '#a78bfa', capacity: 30, throttleMs: 200,  category: 'system'       },
}

const MAX_DISPLAY = 200

const makeId = () => globalThis.crypto?.randomUUID?.() ?? Math.random().toString(36).slice(2)

const resolveTimestamp = (payload: EventPayload): string => {
  const keys = ['timestamp', 'generatedAt', 'assignedAt', 'dispatchedAt', 'dataReceived']
  const rec = payload as unknown as Record<string, unknown>
  for (const k of keys) {
    const v = rec[k]
    if (typeof v === 'string' && v) return v
  }
  return new Date().toISOString()
}

const initBuckets = (): Record<EventType, EventLogEntry[]> => {
  const r = {} as Record<EventType, EventLogEntry[]>
  for (const t of Object.values(EVENT_TYPES)) r[t] = []
  return r
}

export const useEventStore = defineStore('events', () => {
  const eventsByType        = ref<Record<EventType, EventLogEntry[]>>(initBuckets())
  const activeSubscriptions = ref<Set<EventType>>(new Set())
  const displayTypes        = ref<Set<EventType>>(new Set(Object.values(EVENT_TYPES) as EventType[]))  // all types shown by default
  const isPaused            = ref(false)
  const connectionState     = ref<'connected' | 'disconnected'>('disconnected')

  const lastStoredAt = new Map<EventType, number>()

  const addEvent = <TType extends EventType>(
    type: TType,
    payload: EventPayloadByType[TType],
    timestamp?: string,
  ) => {
    if (isPaused.value) return
    if (!activeSubscriptions.value.has(type)) return
    const cfg = TYPE_META[type]
    if (cfg.throttleMs > 0) {
      const now = Date.now()
      if (now - (lastStoredAt.get(type) ?? 0) < cfg.throttleMs) return
      lastStoredAt.set(type, now)
    }
    const entry: EventLogEntry<TType> = {
      id: makeId(),
      type,
      timestamp: timestamp ?? resolveTimestamp(payload),
      payload,
    }
    const bucket = eventsByType.value[type]
    bucket.unshift(entry)
    if (bucket.length > cfg.capacity) bucket.length = cfg.capacity
  }

  const subscribe = (type: EventType) => {
    const next = new Set(activeSubscriptions.value)
    next.add(type)
    activeSubscriptions.value = next
  }

  const unsubscribe = (type: EventType) => {
    const next = new Set(activeSubscriptions.value)
    next.delete(type)
    activeSubscriptions.value = next
  }

  const toggleSubscription = (type: EventType) =>
    activeSubscriptions.value.has(type) ? unsubscribe(type) : subscribe(type)

  const subscribeAll = () => {
    activeSubscriptions.value = new Set(Object.values(EVENT_TYPES) as EventType[])
  }

  const unsubscribeAll = () => {
    activeSubscriptions.value = new Set()
  }

  const clearType  = (type: EventType) => { eventsByType.value[type] = [] }
  const clearAll   = () => { for (const t of Object.values(EVENT_TYPES)) eventsByType.value[t as EventType] = [] }
  const clearEvents = clearAll // legacy alias

  const eventCounts = computed(() => {
    const c = {} as Record<EventType, number>
    for (const t of Object.values(EVENT_TYPES)) c[t as EventType] = eventsByType.value[t as EventType].length
    return c
  })

  const allEvents = computed((): EventLogEntry[] => {
    const all: EventLogEntry[] = []
    for (const entries of Object.values(eventsByType.value)) all.push(...entries)
    return all.sort((a, b) => b.timestamp.localeCompare(a.timestamp)).slice(0, MAX_DISPLAY)
  })

  // legacy alias
  const events = allEvents

  const setConnectionState = (state: 'connected' | 'disconnected') => {
    connectionState.value = state
  }

  return {
    eventsByType, activeSubscriptions, displayTypes, isPaused, connectionState,
    addEvent, subscribe, unsubscribe, toggleSubscription, subscribeAll, unsubscribeAll,
    clearType, clearAll, clearEvents, eventCounts, allEvents, events, setConnectionState,
  }
})
