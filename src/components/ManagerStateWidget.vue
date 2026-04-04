<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from "vue";
import { subscribeToTopic, unsubscribeFromTopic, connection } from "@/plugins/signalr";
import type { ManagerStateDto, ManagerStateApi } from "@/models/models";
import { apiUrl } from "@/config";

const TOPIC_NAME = "ManagerState";

const isRunning = ref(false);
const lastUpdated = ref<string | null>(null);
const isLoading = ref(true);
const error = ref<string | null>(null);
const actionLoading = ref(false);

const statusColor = computed(() => (isRunning.value ? "success" : "grey"));
const statusIcon = computed(() => (isRunning.value ? "mdi-play-circle" : "mdi-stop-circle-outline"));
const statusText = computed(() => (isRunning.value ? "Running" : "Stopped"));
const formattedTime = computed(() =>
  lastUpdated.value ? new Date(lastUpdated.value).toLocaleString() : "—"
);

const fetchState = async () => {
  isLoading.value = true;
  error.value = null;
  try {
    const response = await fetch(apiUrl("/api/cache/manager-state"));
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const data: ManagerStateApi = await response.json();
    isRunning.value = data.isRunning;
    lastUpdated.value = data.lastUpdated;
  } catch (err: any) {
    console.error("Failed to fetch manager state:", err);
    error.value = "Failed to load";
  } finally {
    isLoading.value = false;
  }
};

const handleUpdate = (dto: ManagerStateDto) => {
  isRunning.value = dto.isRunning;
  lastUpdated.value = dto.timestamp;
};

const toggleManager = async () => {
  actionLoading.value = true;
  error.value = null;
  try {
    await connection.invoke("OnManagerStateToggled");
  } catch (err) {
    console.error("Manager action failed:", err);
    error.value = "Action failed";
  } finally {
    actionLoading.value = false;
  }
};

onMounted(() => {
  subscribeToTopic(TOPIC_NAME);
  fetchState();
  connection.on("ManagerState", handleUpdate);
});

onUnmounted(() => {
  unsubscribeFromTopic(TOPIC_NAME);
  connection.off("ManagerState", handleUpdate);
});
</script>

<template>
  <v-card variant="outlined" rounded="lg">
    <v-card-text class="py-3 px-4">
      <!-- Header -->
      <div class="d-flex align-center mb-3" style="gap: 8px">
        <v-icon icon="mdi-clipboard-pulse" color="teal" size="small" />
        <span class="text-body-2 font-weight-medium">Manager</span>
        <v-spacer />
        <v-skeleton-loader v-if="isLoading" type="chip" width="76" />
        <v-chip
          v-else
          :color="statusColor"
          variant="flat"
          size="small"
          :prepend-icon="statusIcon"
        >
          {{ statusText }}
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
          :color="isRunning ? 'error' : 'success'"
          variant="tonal"
          size="small"
          :prepend-icon="isRunning ? 'mdi-stop' : 'mdi-play'"
          :loading="actionLoading"
          :disabled="actionLoading"
          @click="toggleManager"
        >
          {{ isRunning ? 'Stop' : 'Start' }}
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
