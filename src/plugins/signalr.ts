// plugins/signalr.ts
import * as signalR from '@microsoft/signalr'
import type { App } from 'vue'
import { EVENT_TYPES, type AgentDataDto } from '@/models/models'
import { useEventStore } from '@/stores/eventStore'
import { SIGNALR_HUB_URL } from '@/config'

// Exported singleton so components (e.g., SignalRTest) can reuse the live connection
export const connection = new signalR.HubConnectionBuilder()
  .withUrl(SIGNALR_HUB_URL)
  .withAutomaticReconnect()
  .build()

// Reference counts — only invoke Subscribe/Unsubscribe when count crosses zero
// so multiple components can independently subscribe to the same topic without
// one component's cleanup silently killing another's subscription.
const subscriptionCounts = new Map<string, number>()

export const subscribeToTopic = async (topic: string) => {
  const prev = subscriptionCounts.get(topic) ?? 0
  subscriptionCounts.set(topic, prev + 1)
  if (prev === 0) {
    await invokeSubscribe(topic)
  }
}

async function invokeSubscribe(topic: string) {
  if (connection.state === signalR.HubConnectionState.Connected) {
    try {
      await connection.invoke('Subscribe', topic)
      console.log(`Subscribed to ${topic}`)
    } catch (err) {
      console.error(`Error subscribing to ${topic}:`, err)
    }
  }
  // If not connected yet, startConnection() will resubscribe all tracked topics once connected.
}

export const unsubscribeFromTopic = async (topic: string) => {
  const prev = subscriptionCounts.get(topic) ?? 0
  if (prev <= 1) {
    subscriptionCounts.delete(topic)
    if (connection.state === signalR.HubConnectionState.Connected) {
      try {
        await connection.invoke('Unsubscribe', topic)
        console.log(`Unsubscribed from ${topic}`)
      } catch (err) {
        console.error(`Error unsubscribing from ${topic}:`, err)
      }
    }
  } else {
    subscriptionCounts.set(topic, prev - 1)
  }
}

const registerHandlers = () => {
  const store = useEventStore()

  // DriveControl events
  connection.on('CommandGenerated', dto => store.addEvent(EVENT_TYPES.COMMAND_GENERATED, dto))
  connection.on('CommandAssigned', dto => store.addEvent(EVENT_TYPES.COMMAND_ASSIGNED, dto))
  connection.on('CommandDispatched', dto => store.addEvent(EVENT_TYPES.COMMAND_DISPATCHED, dto))
  connection.on('CommandCleared', dto => store.addEvent(EVENT_TYPES.COMMAND_CLEARED, dto))
  connection.on('AgentRegistration', dto => store.addEvent(EVENT_TYPES.AGENT_REGISTRATION, dto))
  connection.on('DriveControlError', dto => store.addEvent(EVENT_TYPES.DRIVE_CONTROL_ERROR, dto))
  connection.on('ManagerState', dto => store.addEvent(EVENT_TYPES.MANAGER_STATE, dto))

  // MapControl events
  connection.on('AgentData', (dto: AgentDataDto) => {
    store.addEvent(EVENT_TYPES.AGENT_DATA, dto)
  })
  connection.on('MapUpdated', dto => store.addEvent(EVENT_TYPES.MAP_UPDATED, dto))
  connection.on('BufferInformation', dto => store.addEvent(EVENT_TYPES.BUFFER_INFORMATION, dto))
  connection.on('UsbStarted', dto => store.addEvent(EVENT_TYPES.USB_STARTED, dto))
  connection.on('MapControlError', dto => store.addEvent(EVENT_TYPES.MAP_CONTROL_ERROR, dto))
  connection.on('WorkerState', dto => store.addEvent(EVENT_TYPES.WORKER_STATE, dto))

  connection.onreconnected(async () => {
    store.setConnectionState('connected')
    // SignalR group memberships are connection-scoped and lost on reconnect — restore them all.
    await resubscribeAll()
  })
  connection.onreconnecting(() => store.setConnectionState('disconnected'))
  connection.onclose(() => store.setConnectionState('disconnected'))
}

async function resubscribeAll() {
  for (const [topic, count] of subscriptionCounts.entries()) {
    if (count > 0) await invokeSubscribe(topic)
  }
}

const startConnection = async () => {
  const store = useEventStore()
  if (connection.state === signalR.HubConnectionState.Disconnected) {
    try {
      await connection.start()
      console.log('SignalR Connected.')
      store.setConnectionState('connected')
      // Subscribe to any topics that were registered before the connection was ready.
      await resubscribeAll()
    } catch (err) {
      console.error('SignalR Connection Error: ', err)
      store.setConnectionState('disconnected')
    }
  }
}

export default {
  install: (app: App) => {
    registerHandlers()
    startConnection()
  }
}
