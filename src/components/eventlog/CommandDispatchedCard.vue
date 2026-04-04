<script setup lang="ts">
import type { CommandDispatchedDto } from '@/models/models'
import EventCard from './EventCard.vue'
import WaypointTable from './WaypointTable.vue'

const props = defineProps<{ event: CommandDispatchedDto }>()

const latencyColor = (ms: number) => ms < 50 ? '#34d399' : ms < 200 ? '#fbbf24' : '#f87171'
</script>

<template>
  <EventCard title="Cmd Dispatched" icon="mdi-send-check-outline" accent-color="#34d399" :timestamp="event.dispatchedAt" :expandable="true">
    <template #header>
      <span class="ec-badge blue">#{{ event.agentId }}</span>
      <span class="ec-badge dim" :style="{ color: latencyColor(event.dispatchLatencyMs) }">{{ event.dispatchLatencyMs }}ms</span>
      <span class="ec-badge dim">{{ event.command?.waypoints?.length ?? 0 }} wps</span>
    </template>
    <template #detail>
      <WaypointTable :waypoints="event.command?.waypoints ?? []" />
    </template>
  </EventCard>
</template>
