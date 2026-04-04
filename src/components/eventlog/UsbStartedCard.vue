<script setup lang="ts">
import type { UsbStartedDto } from '@/models/models'
import EventCard from './EventCard.vue'
defineProps<{ event: UsbStartedDto }>()
</script>

<template>
  <EventCard title="USB Started" icon="mdi-usb" accent-color="#22d3ee" :timestamp="event.timestamp" :expandable="true">
    <template #header>
      <span class="ec-badge" :class="event.success ? 'success' : 'error'">
        {{ event.success ? 'Connected' : 'Failed' }}
      </span>
      <span class="ec-badge cyan">{{ event.portName }}</span>
      <span class="ec-badge dim">{{ event.baudRate }} baud</span>
    </template>
    <template #detail>
      <div class="usb-msg">{{ event.message }}</div>
    </template>
  </EventCard>
</template>

<style scoped>
.usb-msg { font-family: 'Roboto Mono', monospace; font-size: 11px; color: rgba(255,255,255,0.65); }
</style>
