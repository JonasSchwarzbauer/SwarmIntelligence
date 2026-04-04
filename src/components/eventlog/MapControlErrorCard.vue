<script setup lang="ts">
import type { MapControlErrorDto } from '@/models/models'
import EventCard from './EventCard.vue'
import ErrorDetail from './ErrorDetail.vue'
defineProps<{ event: MapControlErrorDto }>()
</script>

<template>
  <EventCard title="MC Error" icon="mdi-alert-circle-outline" accent-color="#f87171" :timestamp="event.timestamp" :expandable="true">
    <template #header>
      <span class="ec-badge error">{{ event.message.length > 50 ? event.message.slice(0, 50) + '…' : event.message }}</span>
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
