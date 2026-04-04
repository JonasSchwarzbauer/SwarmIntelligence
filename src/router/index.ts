/**
 * router/index.ts
 *
 * Automatic routes for `./src/pages/*.vue`
 */

// Composables
import { createRouter, createWebHistory } from 'vue-router'
import { setupLayouts } from 'virtual:generated-layouts'
import { routes } from 'vue-router/auto-routes'
import { apiUrl } from '@/config'
import { useAppStore } from '@/stores/app'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: setupLayouts(routes),
})

// Setup guard: always check API for initialization status
let initChecked = false
let isInitialized = false

const checkInitStatus = async () => {
  try {
    const response = await fetch(apiUrl('/api/cache/swarm-settings'))
    const data = await response.json()
    isInitialized = data.isInitialized === true

    if (isInitialized && data.swarmSettings) {
      const s = data.swarmSettings
      const appStore = useAppStore()
      appStore.xMax = s.xFieldSize
      appStore.yMax = s.yFieldSize
      appStore.gridSize = s.gridSize
    }
  } catch (error) {
    console.error('Failed to check initialization status:', error)
    isInitialized = false
  }
  initChecked = true
}

router.beforeEach(async (to) => {
  // Check API on first load (covers page reload)
  if (!initChecked) {
    await checkInitStatus()
  }

  if (to.path === '/setup') {
    // Already initialized → skip setup, go to dashboard
    if (isInitialized) {
      return '/dashboard'
    }
    return
  }

  // Redirect root to dashboard
  if (to.path === '/') {
    return '/dashboard'
  }

  // Not initialized → force setup
  if (!isInitialized) {
    return '/setup'
  }
})

// Called after successful SignalR init so the guard lets navigation through
// without needing another API call in the same session
export const markSetupComplete = () => {
  isInitialized = true
}

// Workaround for https://github.com/vitejs/vite/issues/11804
router.onError((err, to) => {
  if (err?.message?.includes?.('Failed to fetch dynamically imported module')) {
    if (localStorage.getItem('vuetify:dynamic-reload')) {
      console.error('Dynamic import error, reloading page did not fix it', err)
    } else {
      console.log('Reloading page to fix dynamic import error')
      localStorage.setItem('vuetify:dynamic-reload', 'true')
      location.assign(to.fullPath)
    }
  } else {
    console.error(err)
  }
})

router.isReady().then(() => {
  localStorage.removeItem('vuetify:dynamic-reload')
})

export default router
