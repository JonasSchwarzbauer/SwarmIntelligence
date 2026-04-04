<route lang="yaml">
meta:
  layout: blank
</route>

<template>
  <v-container class="fill-height d-flex align-center justify-center" fluid>
    <v-card width="700" elevation="8" class="pa-4">
      <v-card-title class="text-h4 text-center pb-2">
        <v-icon size="40" class="mr-2">mdi-cog-outline</v-icon>
        Swarm Setup
      </v-card-title>
      <v-card-subtitle class="text-center mb-4">
        Configure your swarm before getting started
      </v-card-subtitle>

      <v-stepper
        v-model="currentStep"
        :items="stepItems"
        alt-labels
        :class="`step-${currentStep}`"
      >
        <template v-slot:item.1>
          <v-card flat>
            <v-card-text>
              <h3 class="text-h6 mb-4">Number of Agents</h3>
              <p class="text-body-2 mb-6 text-medium-emphasis">
                How many slave agents will participate in the swarm?
              </p>
              <v-text-field
                v-model.number="requiredSlavesAmount"
                label="Number of Agents"
                type="number"
                step="1"
                min="0"
                max="255"
                hint="Value between 0 and 255"
                persistent-hint
                prepend-inner-icon="mdi-robot"
                variant="outlined"
                :rules="[rules.required, rules.agentRange]"
              />
            </v-card-text>
          </v-card>
        </template>

        <template v-slot:item.2>
          <v-card flat>
            <v-card-text>
              <h3 class="text-h6 mb-4">Field Size</h3>
              <p class="text-body-2 mb-6 text-medium-emphasis">
                Enter the field dimensions. Anchor positions will be automatically placed at:
                (0, 0), (0, Y), (X, 0)
              </p>

              <v-row>
                <v-col cols="6">
                  <v-text-field
                    v-model.number="fieldSizeX"
                    label="Field Size X (metres)"
                    type="number"
                    step="0.1"
                    min="0.1"
                    hint="X dimension of the field"
                    persistent-hint
                    prepend-inner-icon="mdi-arrow-left-right"
                    variant="outlined"
                    :rules="[rules.required, rules.positive]"
                  />
                </v-col>
                <v-col cols="6">
                  <v-text-field
                    v-model.number="fieldSizeY"
                    label="Field Size Y (metres)"
                    type="number"
                    step="0.1"
                    min="0.1"
                    hint="Y dimension of the field"
                    persistent-hint
                    prepend-inner-icon="mdi-arrow-up-down"
                    variant="outlined"
                    :rules="[rules.required, rules.positive]"
                  />
                </v-col>
              </v-row>

              <v-divider class="my-4" />

              <h4 class="text-subtitle-1 mb-3">Resulting Anchor Positions</h4>
              <v-table density="compact">
                <thead>
                  <tr>
                    <th>Anchor</th>
                    <th>X</th>
                    <th>Y</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(anchor, index) in computedAnchors" :key="index">
                    <td>Anchor {{ index + 1 }}</td>
                    <td>{{ anchor.X }}</td>
                    <td>{{ anchor.Y }}</td>
                  </tr>
                </tbody>
              </v-table>
            </v-card-text>
          </v-card>
        </template>

        <template v-slot:item.3>
          <v-card flat>
            <v-card-text>
              <h3 class="text-h6 mb-4">Compass Offset</h3>
              <p class="text-body-2 mb-6 text-medium-emphasis">
                Set the compass offset angle for calibration.
              </p>
              <v-text-field
                v-model.number="compassOffset"
                label="Compass Offset"
                type="number"
                step="0.1"
                min="0"
                max="360"
                hint="Value between 0 and 360 degrees"
                persistent-hint
                prepend-inner-icon="mdi-compass"
                variant="outlined"
                :rules="[rules.required, rules.compassRange]"
              />
            </v-card-text>
          </v-card>
        </template>

        <template v-slot:actions>
          <v-card-actions class="pa-4">
            <v-btn
              v-if="currentStep > 1"
              variant="text"
              @click="currentStep--"
            >
              Back
            </v-btn>
            <v-spacer />
            <v-btn
              v-if="currentStep < 3"
              color="primary"
              variant="flat"
              @click="currentStep++"
              :disabled="!isStepValid"
            >
              Next
            </v-btn>
            <v-btn
              v-else
              color="success"
              variant="flat"
              prepend-icon="mdi-check"
              @click="finishSetup"
              :loading="sending"
              :disabled="!isStepValid"
            >
              Finish Setup
            </v-btn>
          </v-card-actions>
        </template>
      </v-stepper>
    </v-card>

    <v-snackbar
      v-model="snackbar"
      :color="snackbarColor"
      timeout="3000"
    >
      {{ snackbarText }}
      <template v-slot:actions>
        <v-btn variant="text" @click="snackbar = false">Close</v-btn>
      </template>
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { connection } from '@/plugins/signalr'
import { useAppStore } from '@/stores/app'
import { markSetupComplete } from '@/router'

const router = useRouter()
const appStore = useAppStore()

const currentStep = ref(1)

const stepItems = [
  { title: 'Agents', value: 1 },
  { title: 'Anchor Positions', value: 2 },
  { title: 'Compass', value: 3 },
]

// Form fields
const requiredSlavesAmount = ref<number>(1)
const fieldSizeX = ref<number>(2)
const fieldSizeY = ref<number>(2)
const compassOffset = ref<number>(353)

// Validation rules
const rules = {
  required: (v: any) => v !== null && v !== undefined && v !== '' || 'Required',
  agentRange: (v: number) => (v >= 0 && v <= 255) || 'Must be between 0 and 255',
  positive: (v: number) => v > 0 || 'Must be greater than 0',
  compassRange: (v: number) => (v >= 0 && v <= 360) || 'Must be between 0 and 360',
}

// Computed anchor positions from field size
const computedAnchors = computed(() => [
  { X: 0, Y: 0 },
  { X: 0, Y: fieldSizeY.value },
  { X: fieldSizeX.value, Y: 0 },
])

// Step validation
const isStepValid = computed(() => {
  switch (currentStep.value) {
    case 1:
      return requiredSlavesAmount.value >= 0 && requiredSlavesAmount.value <= 255
    case 2:
      return fieldSizeX.value > 0 && fieldSizeY.value > 0
    case 3:
      return compassOffset.value !== null && compassOffset.value !== undefined && compassOffset.value >= 0 && compassOffset.value <= 360
    default:
      return false
  }
})

const sending = ref(false)
const snackbar = ref(false)
const snackbarText = ref('')
const snackbarColor = ref('success')

const finishSetup = async () => {
  sending.value = true
  try {
    if (connection.state !== 'Connected') {
      try {
        await connection.start()
      } catch (e) {
        console.error('Failed to start SignalR connection', e)
        throw new Error('SignalR not connected')
      }
    }

    const payload = {
      CompassOffset: compassOffset.value,
      AnchorPositions: computedAnchors.value,
      RequiredSlavesAmount: requiredSlavesAmount.value,
    }

    console.log('Sending Init Data:', payload)

    appStore.yMax = fieldSizeY.value
    appStore.xMax = fieldSizeX.value

    await connection.invoke('OnInitDataReceived', payload)

    snackbarText.value = 'Setup completed successfully!'
    snackbarColor.value = 'success'
    snackbar.value = true

    markSetupComplete()

    // Navigate to home after a short delay so the user sees the success message
    setTimeout(() => {
      router.push('/dashboard')
    }, 1000)
  } catch (error) {
    console.error('Error sending init data:', error)
    snackbarText.value = 'Failed to complete setup.'
    snackbarColor.value = 'error'
    snackbar.value = true
  } finally {
    sending.value = false
  }
}
</script>

<style scoped>
.v-container {
  background: var(--bg) !important;
}

:deep(.v-card) {
  background: var(--surface) !important;
  color: var(--text) !important;
  border: 1px solid var(--border) !important;
  box-shadow: none !important;
  border-radius: var(--radius) !important;
}

:deep(.v-card-title) {
  color: var(--text) !important;
}

:deep(.v-card-subtitle) {
  color: var(--text-2) !important;
}

:deep(.v-stepper) {
  background: transparent !important;
  box-shadow: none !important;
}

:deep(.v-stepper-header) {
  box-shadow: none !important;
  border-bottom: 1px solid var(--border-hi) !important;
  padding-bottom: 12px !important;
}

:deep(.v-stepper-item) {
  color: var(--text-3) !important;
}

:deep(.v-stepper-item .v-stepper-item__avatar) {
  background: var(--surface-raised) !important;
  border: 1px solid var(--border-hi) !important;
  color: var(--text-2) !important;
}

:deep(.v-stepper-item--selected .v-stepper-item__avatar) {
  background: var(--accent) !important;
  border-color: var(--accent) !important;
  color: #fff !important;
}

:deep(.v-stepper-item--complete .v-stepper-item__avatar) {
  background: var(--green) !important;
  border-color: var(--green) !important;
  color: #fff !important;
}

:deep(.v-stepper-item--selected) {
  color: var(--accent) !important;
}

:deep(.v-stepper-item--complete) {
  color: var(--green) !important;
}

:deep(.v-stepper-item__subtitle) {
  color: var(--text-3) !important;
}

:deep(.v-stepper-header .v-divider) {
  border-color: var(--border-hi) !important;
  opacity: 1 !important;
  border-width: 2px 0 0 !important;
  transition: border-color 0.3s !important;
}

/* Step 2: first divider is completed */
.step-2 :deep(.v-stepper-header .v-divider:nth-of-type(1)) {
  border-color: var(--accent) !important;
}

/* Step 3: both dividers are completed */
.step-3 :deep(.v-stepper-header .v-divider) {
  border-color: var(--accent) !important;
}

:deep(.v-stepper-window) {
  background: transparent !important;
}

:deep(.v-field) {
  color: var(--text) !important;
}

:deep(.v-field--variant-outlined .v-field__outline) {
  --v-field-border-opacity: 0.15 !important;
}

:deep(.v-btn--variant-flat[color="primary"]) {
  background: var(--accent) !important;
  color: #fff !important;
}

:deep(.v-btn--variant-flat[color="success"]) {
  background: var(--green) !important;
  color: #fff !important;
}

:deep(.v-table) {
  background: transparent !important;
  color: var(--text) !important;
}

:deep(.v-table th) {
  color: var(--text-2) !important;
  border-bottom-color: var(--border) !important;
}

:deep(.v-table td) {
  color: var(--text) !important;
  border-bottom-color: var(--border) !important;
}

:deep(.v-divider) {
  border-color: var(--border) !important;
}

:deep(h3), :deep(h4) {
  color: var(--text) !important;
}
</style>
