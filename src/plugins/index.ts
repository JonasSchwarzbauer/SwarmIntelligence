/**
 * plugins/index.ts
 *
 * Automatically included in `./src/main.ts`
 */

// Import all the plugins we want to use
import vuetify from './vuetify'      // UI component framework
import pinia from '../stores'         // State management
import router from '../router'        // Page navigation
import signalR from './signalr'      // Real-time communication

// Import Vue App type for TypeScript
import type { App } from 'vue'

// Function to register all plugins with Vue
export function registerPlugins (app: App) {
  app
    .use(vuetify)    // Add UI components
    .use(router)     // Add routing
    .use(pinia)      // Add state management
    .use(signalR)    // Add real-time communication
}
