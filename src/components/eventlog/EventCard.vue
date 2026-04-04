<script setup lang="ts">
import { ref, computed } from 'vue'

interface Props {
  title: string
  icon?: string
  accentColor?: string
  timestamp?: string
  expandable?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  icon: 'mdi-information-outline',
  accentColor: '#60a5fa',
  expandable: false,
})

const expanded = ref(false)

const formattedTime = computed(() => {
  if (!props.timestamp) return ''
  try {
    const d = new Date(props.timestamp)
    const hms = d.toLocaleTimeString('en', { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' })
    return hms + '.' + String(d.getMilliseconds()).padStart(3, '0')
  } catch { return props.timestamp }
})
</script>

<template>
  <div class="ec-root" :style="{ '--accent': accentColor }">
    <div
      class="ec-header"
      :class="{ 'ec-clickable': expandable }"
      @click="expandable && (expanded = !expanded)"
    >
      <div class="ec-bar" />
      <v-icon :icon="icon" size="15" class="ec-icon" />
      <span class="ec-title">{{ title }}</span>
      <div class="ec-slots">
        <slot name="header" />
      </div>
      <span class="ec-time">{{ formattedTime }}</span>
      <v-icon
        v-if="expandable"
        :icon="expanded ? 'mdi-chevron-up' : 'mdi-chevron-down'"
        size="15"
        class="ec-chevron"
      />
    </div>
    <div v-if="expanded && expandable" class="ec-body">
      <slot name="detail" />
    </div>
  </div>
</template>

<style scoped>
.ec-root {
  background: rgba(13, 17, 23, 0.65);
  border: 1px solid rgba(48, 54, 61, 0.6);
  border-radius: 6px;
  overflow: hidden;
  margin-bottom: 4px;
  font-family: 'Roboto Mono', monospace;
}
.ec-header {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 7px 10px 7px 0;
  min-height: 38px;
}
.ec-clickable { cursor: pointer; }
.ec-clickable:hover { background: rgba(255,255,255,0.03); }
.ec-bar {
  width: 3px;
  align-self: stretch;
  background: var(--accent);
  flex-shrink: 0;
  margin-right: 3px;
}
.ec-icon { color: var(--accent); flex-shrink: 0; opacity: 0.9; }
.ec-title {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: rgba(255,255,255,0.6);
  white-space: nowrap;
  flex-shrink: 0;
  min-width: 105px;
}
.ec-slots {
  display: flex;
  align-items: center;
  gap: 5px;
  flex: 1;
  flex-wrap: wrap;
  overflow: hidden;
  min-width: 0;
}
.ec-time {
  font-size: 10px;
  color: rgba(255,255,255,0.25);
  white-space: nowrap;
  flex-shrink: 0;
}
.ec-chevron { color: rgba(255,255,255,0.3); flex-shrink: 0; }
.ec-body {
  padding: 8px 12px 10px 16px;
  border-top: 1px solid rgba(48,54,61,0.4);
  background: rgba(0,0,0,0.18);
}

/* Badge styles for slot content */
:slotted(.ec-badge) {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 1px 7px;
  border-radius: 3px;
  font-size: 11px;
  font-family: 'Roboto Mono', monospace;
  white-space: nowrap;
  border: 1px solid transparent;
  line-height: 1.6;
}
:slotted(.ec-badge.blue)    { background: rgba(96,165,250,0.12);  color: #60a5fa; border-color: rgba(96,165,250,0.22); }
:slotted(.ec-badge.green)   { background: rgba(52,211,153,0.12);  color: #34d399; border-color: rgba(52,211,153,0.22); }
:slotted(.ec-badge.purple)  { background: rgba(167,139,250,0.12); color: #a78bfa; border-color: rgba(167,139,250,0.22); }
:slotted(.ec-badge.cyan)    { background: rgba(34,211,238,0.12);  color: #22d3ee; border-color: rgba(34,211,238,0.22); }
:slotted(.ec-badge.amber)   { background: rgba(251,191,36,0.12);  color: #fbbf24; border-color: rgba(251,191,36,0.22); }
:slotted(.ec-badge.error)   { background: rgba(248,113,113,0.12); color: #f87171; border-color: rgba(248,113,113,0.22); }
:slotted(.ec-badge.success) { background: rgba(52,211,153,0.12);  color: #34d399; border-color: rgba(52,211,153,0.22); }
:slotted(.ec-badge.dim)     { background: rgba(255,255,255,0.05); color: rgba(255,255,255,0.42); border-color: rgba(255,255,255,0.08); }
:slotted(.ec-badge.warn)    { background: rgba(248,113,113,0.12); color: #f87171; border-color: rgba(248,113,113,0.22); }
</style>
