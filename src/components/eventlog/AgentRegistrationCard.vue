<script setup lang="ts">
import type { AgentRegistrationDto } from '@/models/models'
import EventCard from './EventCard.vue'
defineProps<{ event: AgentRegistrationDto }>()
</script>

<template>
  <EventCard title="Registration" icon="mdi-badge-account" accent-color="#a78bfa" :timestamp="event.timestamp">
    <template #header>
      <span class="ec-badge blue">#{{ event.agentId }}</span>
      <span class="ec-badge purple">{{ event.registrationType }}</span>
    </template>
  </EventCard>
</template>
