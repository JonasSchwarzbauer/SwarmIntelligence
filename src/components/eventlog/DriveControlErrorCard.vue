<script setup lang="ts">
import type { DriveControlErrorDto } from '@/models/models'
import EventCard from './EventCard.vue'
import ErrorDetail from './ErrorDetail.vue'
defineProps<{ event: DriveControlErrorDto }>()
</script>

<template>
  <EventCard title="DC Error" icon="mdi-alert-octagon" accent-color="#f87171" :timestamp="event.timestamp" :expandable="true">
    <template #header>
      <span class="ec-badge blue">#{{ event.agentId }}</span>
      <span class="ec-badge error">{{ event.message.length > 40 ? event.message.slice(0, 40) + '…' : event.message }}</span>
    </template>
    <template #detail>
      <ErrorDetail
        :source="event.source"
        :source-context="event.sourceContext"
        :exception-message="event.exceptionMessage"
        :exception="event.exception"
      />
    </template>
  </EventCard>
</template>
