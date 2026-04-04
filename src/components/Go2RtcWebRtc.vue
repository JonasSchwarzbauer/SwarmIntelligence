<template>
  <iframe
    :src="url"
    style="width: 100%; height: 520px; border: 0; border-radius: 16px;"
    allow="autoplay; fullscreen"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    src?: string
  }>(),
  { src: 'cam1' },
)

// Use the exact URL that you know works:
const url = computed(() => `/cam/stream.html?src=${encodeURIComponent(props.src)}&mode=webrtc`)
</script>