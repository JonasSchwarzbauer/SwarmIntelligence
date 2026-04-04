<script setup lang="ts">
import type { CommandGeneratedDto } from '@/models/models'
import EventCard from './EventCard.vue'
import WaypointTable from './WaypointTable.vue'
defineProps<{ event: CommandGeneratedDto }>()
</script>

<template>
  <EventCard title="Cmd Generated" icon="mdi-robot-outline" accent-color="#60a5fa" :timestamp="event.generatedAt" :expandable="true">
    <template #header>
      <span class="ec-badge blue">#{{ event.agentId }}</span>
      <span class="ec-badge dim">{{ event.command?.waypoints?.length ?? 0 }} wps</span>
      <span v-if="event.command?.driveFlags?.length" class="ec-badge amber">{{ event.command.driveFlags.length }} flags</span>
    </template>
    <template #detail>
      <WaypointTable :waypoints="event.command?.waypoints ?? []" />
      <div v-if="event.command?.driveFlags?.length" class="cg-flags">
        <span v-for="f in event.command.driveFlags" :key="f" class="cg-flag">{{ f }}</span>
      </div>
    </template>
  </EventCard>
</template>

<style scoped>
.cg-flags { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 8px; border-top: 1px solid rgba(48,54,61,0.35); padding-top: 6px; }
.cg-flag  { padding: 1px 6px; border-radius: 3px; font-size: 10px; font-family: 'Roboto Mono', monospace; background: rgba(96,165,250,0.1); color: #60a5fa; border: 1px solid rgba(96,165,250,0.2); }
</style>
