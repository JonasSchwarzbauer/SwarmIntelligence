<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from "vue";
import { subscribeToTopic, unsubscribeFromTopic, connection } from "@/plugins/signalr";
import type { WorkerStateDto, MapWorkerStateApi } from "@/models/models";
import { apiUrl } from "@/config";

const TOPIC_NAME = "WorkerState";

const currentState = ref("Stopped");
const lastUpdated = ref<string | null>(null);
const isLoading = ref(true);
const error = ref<string | null>(null);
const actionLoading = ref(false);

const stateColor = computed(() => currentState.value === "Started" ? "success" : "grey");

const stateIcon = computed(() => currentState.value === "Started" ? "mdi-play-circle" : "mdi-stop-circle-outline");

const formattedTime = computed(() =>
  lastUpdated.value ? new Date(lastUpdated.value).toLocaleString() : "—"
);

const isActive = computed(() => currentState.value === "Started");

const fetchState = async () => {
  isLoading.value = true;
  error.value = null;
  try {
    const response = await fetch(apiUrl("/api/cache/map-worker-state"));
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const data: MapWorkerStateApi = await response.json();
    currentState.value = data.currentState;
    lastUpdated.value = data.lastUpdated;
  } catch (err: any) {
    console.error("Failed to fetch worker state:", err);
    error.value = "Failed to load";
  } finally {
    isLoading.value = false;
  }
};

const handleUpdate = (dto: WorkerStateDto) => {
  currentState.value = dto.state;
  lastUpdated.value = dto.timestamp;
};

const toggleWorker = async () => {
  actionLoading.value = true;
  error.value = null;
  try {
    await connection.invoke("OnWorkerStateToggled");
  } catch (err) {
    console.error("Worker action failed:", err);
    error.value = "Action failed";
  } finally {
    actionLoading.value = false;
  }
};

onMounted(() => {
  subscribeToTopic(TOPIC_NAME);
  fetchState();
  connection.on("WorkerState", handleUpdate);
});

onUnmounted(() => {
  unsubscribeFromTopic(TOPIC_NAME);
  connection.off("WorkerState", handleUpdate);
});
</script>

<template>
  <v-card variant="outlined" rounded="lg">
    <v-card-text class="py-3 px-4">
      <!-- Header -->
      <div class="d-flex align-center mb-3" style="gap: 8px">
        <v-icon icon="mdi-cog-outline" color="brown" size="small" />
        <span class="text-body-2 font-weight-medium">Worker</span>
        <v-spacer />
        <v-skeleton-loader v-if="isLoading" type="chip" width="84" />
        <v-chip
          v-else
          :color="stateColor"
          variant="flat"
          size="small"
          :prepend-icon="stateIcon"
        >
          {{ currentState }}
        </v-chip>
      </div>

      <v-divider class="mb-3" />

      <!-- Footer row -->
      <div class="d-flex align-center">
        <span class="text-caption text-medium-emphasis">
          <v-icon icon="mdi-clock-outline" size="x-small" class="mr-1" />
          {{ formattedTime }}
        </span>
        <v-spacer />
        <v-btn
          variant="text"
          size="x-small"
          icon="mdi-refresh"
          class="mr-1"
          :loading="isLoading"
          @click="fetchState"
        />
        <v-btn
          v-if="!isLoading"
          :color="isActive ? 'error' : 'success'"
          variant="tonal"
          size="small"
          :prepend-icon="isActive ? 'mdi-stop' : 'mdi-play'"
          :loading="actionLoading"
          :disabled="actionLoading"
          @click="toggleWorker"
        >
          {{ isActive ? 'Stop' : 'Start' }}
        </v-btn>
      </div>

      <!-- Error -->
      <div v-if="error" class="d-flex align-center text-error text-caption mt-2" style="gap: 4px">
        <v-icon icon="mdi-alert-circle-outline" size="x-small" />
        {{ error }}
      </div>
    </v-card-text>
  </v-card>
</template>
