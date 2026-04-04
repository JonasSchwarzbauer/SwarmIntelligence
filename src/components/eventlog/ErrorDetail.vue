<script setup lang="ts">
defineProps<{
  source: string
  sourceContext: string
  exceptionMessage: string
  exception: string
}>()
</script>

<template>
  <div class="ed-wrap">
    <div class="ed-row">
      <span class="ed-key">Source</span>
      <span class="ed-val">{{ source }} <span class="ed-ctx">{{ sourceContext }}</span></span>
    </div>
    <div class="ed-row ed-exc">
      <span class="ed-key">Exception</span>
      <span class="ed-val">{{ exceptionMessage }}</span>
    </div>
    <details v-if="exception" class="ed-trace">
      <summary>Stack trace</summary>
      <pre class="ed-pre">{{ exception }}</pre>
    </details>
  </div>
</template>

<style scoped>
.ed-wrap { font-family: 'Roboto Mono', monospace; font-size: 11px; }
.ed-row  { display: flex; gap: 10px; padding: 4px 0; border-bottom: 1px solid rgba(48,54,61,0.25); }
.ed-row:last-of-type { border-bottom: none; }
.ed-key  { color: rgba(255,255,255,0.3); font-size: 10px; text-transform: uppercase; letter-spacing: 0.04em; min-width: 68px; flex-shrink: 0; padding-top: 1px; }
.ed-val  { color: rgba(255,255,255,0.72); word-break: break-all; }
.ed-ctx  { color: rgba(255,255,255,0.35); }
.ed-exc .ed-val { color: #f87171; }
.ed-trace { margin-top: 8px; font-size: 10px; color: rgba(255,255,255,0.35); }
.ed-trace summary { cursor: pointer; padding: 2px 0; }
.ed-pre  { margin-top: 6px; font-size: 10px; color: rgba(255,255,255,0.3); white-space: pre-wrap; word-break: break-all; overflow-x: auto; }
</style>
