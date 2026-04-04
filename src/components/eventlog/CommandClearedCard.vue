<script setup lang="ts">
import type { CommandClearedDto } from '@/models/models'
import EventCard from './EventCard.vue'
defineProps<{ event: CommandClearedDto }>()
</script>

<template>
  <EventCard title="Cmd Cleared" icon="mdi-broom" accent-color="#60a5fa" :timestamp="event.timestamp">
    <template #header>
      <span class="ec-badge blue">#{{ event.agentId }}</span>
      <span class="ec-badge dim">queue cleared</span>
    </template>
  </EventCard>
</template>
