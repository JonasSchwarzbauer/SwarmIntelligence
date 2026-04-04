<script setup lang="ts">
import type { MapUpdatedDto } from '@/models/models'
import EventCard from './EventCard.vue'
defineProps<{ event: MapUpdatedDto }>()
</script>

<template>
  <EventCard title="Map Updated" icon="mdi-map" accent-color="#34d399" :timestamp="event.timestamp">
    <template #header>
      <span class="ec-badge dim">cell({{ event.cellX }},{{ event.cellY }})</span>
      <span class="ec-badge dim">({{ event.x.toFixed(2) }}, {{ event.y.toFixed(2) }})</span>
      <span class="ec-badge" :class="event.occupied ? 'error' : 'success'">
        {{ event.occupied ? 'Occupied' : 'Cleared' }}
      </span>
    </template>
  </EventCard>
</template>
