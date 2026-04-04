<template>
  <v-container>
    <v-row>
      <v-col cols="12">
        <!-- handle Enter key: @keyup.enter triggers sendMessage -->
        <v-text-field
          v-model="message"
          label="Enter message"
          placeholder="Type something..."
          @keyup.enter="sendMessage"
        ></v-text-field>
      </v-col>
    </v-row>
    <v-row>
      <v-col class="d-flex" cols="12" gap="2">
        <v-btn @click="sendMessage" color="primary">
          Send Message
        </v-btn>
        <v-btn @click="clearHistory" color="error" variant="outlined">
          Clear History
        </v-btn>
      </v-col>
    </v-row>

    <!-- Message history -->
    <v-row>
      <v-col cols="12">
        <v-card outlined>
          <v-card-title>Message History</v-card-title>
          <v-divider />
          <v-list dense>
            <v-list-item
              v-for="(m, i) in messages"
              :key="m.id"
            >
              <v-list-item-content>
                <div style="display:flex; gap:8px; align-items:center;">
                  <v-chip small :color="m.direction === 'sent' ? 'primary' : 'grey lighten-2'">
                    {{ m.direction === 'sent' ? 'Sent' : 'Received' }}
                  </v-chip>
                  <div>
                    <div>{{ m.text }}</div>
                    <small class="text--secondary">{{ m.time }}</small>
                  </div>
                </div>
              </v-list-item-content>
            </v-list-item>
            <v-list-item v-if="messages.length === 0">
              <v-list-item-content>
                <small class="text--secondary">No messages yet</small>
              </v-list-item-content>
            </v-list-item>
          </v-list>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { connection } from '@/plugins/signalr'

type Msg = {
  id: string
  text: string
  direction: 'sent' | 'received'
  time: string
}

const message = ref('')
const messages = ref<Msg[]>([])

const now = () => new Date().toLocaleString()

const pushMessage = (text: string, direction: Msg['direction']) => {
  // Insert at the beginning so newest messages are shown on top
  messages.value.unshift({
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`,
    text,
    direction,
    time: now()
  })
}

const sendMessage = async () => {
  if (message.value.trim()) {
    const textToSend = message.value
    try {
      // Record sent message at top
      pushMessage(textToSend, 'sent')

      // Ensure connection started (plugin normally starts it)
      if (connection.state !== 'Connected') {
        try { await connection.start() } catch { /* ignore; plugin may have started earlier */ }
      }

      await connection.invoke('SendMessage', textToSend)
      message.value = '' // Clear input after sending
    } catch (err) {
      console.error('Error sending message:', err)
    }
  }
}

const clearHistory = () => {
  messages.value = []
}

let receiveHandler: ((msg: string) => void) | null = null

onMounted(() => {
  receiveHandler = (msg: string) => {
    pushMessage(msg, 'received')
  }
  connection.on('ReceiveMessage', receiveHandler)
})

onBeforeUnmount(() => {
  if (receiveHandler) {
    connection.off('ReceiveMessage', receiveHandler)
    receiveHandler = null
  }
})
</script>
