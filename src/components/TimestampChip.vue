<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';

const props = defineProps<{ timestamp: string }>();

const now = ref(Date.now());
let timer: ReturnType<typeof setInterval> | null = null;

onMounted(() => { timer = setInterval(() => { now.value = Date.now(); }, 1000); });
onUnmounted(() => { if (timer) clearInterval(timer); });

const date = computed(() => props.timestamp ? new Date(props.timestamp) : null);

const fullTime = computed(() =>
  date.value
    ? date.value.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })
    : '\u2014'
);

const ageSeconds = computed(() =>
  date.value ? (now.value - date.value.getTime()) / 1000 : Infinity
);

const relativeTime = computed(() => {
  const s = ageSeconds.value;
  if (!isFinite(s)) return '\u2014';
  if (s < 2) return 'just now';
  if (s < 60) return `${Math.floor(s)}s ago`;
  if (s < 3600) return `${Math.floor(s / 60)}m ago`;
  return `${Math.floor(s / 3600)}h ago`;
});

const chipColor = computed(() => {
  const s = ageSeconds.value;
  if (s < 5) return 'success';
  if (s < 30) return 'warning';
  return 'error';
});
</script>

<template>
  <v-tooltip location="top">
    <template #activator="{ props: tp }">
      <v-chip v-bind="tp" :color="chipColor" size="small" variant="tonal" label class="ts-chip">
        <v-icon icon="mdi-clock-outline" size="12" class="mr-1" />
        {{ relativeTime }}
      </v-chip>
    </template>
    <span>{{ fullTime }}</span>
  </v-tooltip>
</template>

<style scoped>
.ts-chip {
  font-family: "Roboto Mono", monospace;
  font-size: 0.72rem;
  min-width: 72px;
  cursor: default;
}
</style>
