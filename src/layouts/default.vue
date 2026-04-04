<template>
  <v-app class="si-app">

    <!-- ── Navigation Drawer ─────────────────────────────────────── -->
    <v-navigation-drawer
      v-model="drawerOpen"
      :rail="rail"
      :width="220"
      :rail-width="56"
      permanent
      class="si-drawer"
    >
      <!-- Toggle -->
      <div class="si-drawer__logo" @click="rail = !rail" title="Toggle sidebar">
        <v-icon :icon="rail ? 'mdi-menu' : 'mdi-menu-open'" size="20" class="si-drawer__burger" />
        <Transition name="si-fade">
          <span v-if="!rail" class="si-drawer__logo-text">Swarm<span class="si-drawer__logo-dim">.sys</span></span>
        </Transition>
      </div>

      <div class="si-drawer__divider" />

      <!-- Navigation list -->
      <v-list nav density="compact" class="si-nav-list pa-2">
        <v-list-item
          v-for="item in items"
          :key="item.to"
          :to="item.to"
          :value="item.to"
          :title="item.title"
          :prepend-icon="item.icon"
          class="si-nav-item"
          rounded="lg"
        >
          <template #prepend>
            <v-icon :icon="item.icon" size="18" class="si-nav-item__icon mr-3" />
          </template>
          <template #title>
            <span class="si-nav-item__label">{{ item.title }}</span>
          </template>
        </v-list-item>
      </v-list>

    </v-navigation-drawer>

    <!-- ── App Bar ───────────────────────────────────────────────── -->
    <v-app-bar class="si-appbar" flat height="52">
      <v-app-bar-nav-icon
        class="si-appbar__nav d-md-none"
        @click="drawerOpen = !drawerOpen"
        size="small"
      />

      <div class="si-appbar__breadcrumb">
        <span class="si-appbar__page">{{ pageTitle }}</span>
      </div>

      <v-spacer />

      <v-btn
        :to="'/awarenessmap'"
        :class="route.path === '/awarenessmap' ? 'si-appbar__btn--active' : ''"
        class="si-appbar__btn mr-1"
        variant="text"
        size="small"
        icon="mdi-map-marker-multiple"
        title="Awareness Map"
      />
      <v-btn
        :to="'/command-center'"
        :class="route.path === '/command-center' ? 'si-appbar__btn--active' : ''"
        class="si-appbar__btn mr-2"
        variant="text"
        size="small"
        icon="mdi-crosshairs-gps"
        title="Command Center"
      />
    </v-app-bar>

    <!-- ── Main Content ──────────────────────────────────────────── -->
    <v-main class="si-main">
      <router-view />
    </v-main>

  </v-app>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const rail = ref(false)
const drawerOpen = ref(true)

const items = [
  { title: 'Dashboard',      to: '/dashboard',         icon: 'mdi-view-dashboard-outline' },
  { title: 'Fleet',          to: '/fleet-management',  icon: 'mdi-robot-industrial-outline' },
  { title: 'Awareness Map',  to: '/awarenessmap',      icon: 'mdi-map-marker-multiple-outline' },
  { title: 'Command Center', to: '/command-center',    icon: 'mdi-crosshairs-gps' },
  { title: 'Drive Control',  to: '/drive-control',     icon: 'mdi-car-connected' },
  { title: 'Lead Vehicle',   to: '/leadvehicle-detail',icon: 'mdi-crown-outline' },
  { title: 'Event Log',      to: '/event-log',         icon: 'mdi-text-box-multiple-outline' },
  { title: 'Settings',       to: '/settings',          icon: 'mdi-cog-outline' },
]

const titleMap: Record<string, string> = {
  '/dashboard':         'Dashboard',
  '/fleet-management':  'Fleet Management',
  '/awarenessmap':      'Awareness Map',
  '/command-center':    'Command Center',
  '/drive-control':     'Drive Control',
  '/leadvehicle-detail':'Lead Vehicle',
  '/event-log':         'Event Log',
  '/settings':          'Settings',
}

const pageTitle = computed(() => titleMap[route.path] ?? route.path.replace('/', ''))
</script>

<style scoped>
.si-app {
  background: var(--bg) !important;
}

/* ── Drawer ────────────────────────────────────────────────────── */
.si-drawer {
  background: var(--surface) !important;
  border-right: 1px solid var(--border) !important;
  box-shadow: none !important;
}

.si-drawer__logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 14px 12px;
  cursor: pointer;
  user-select: none;
}

.si-drawer__burger {
  color: var(--text-2) !important;
  flex-shrink: 0;
  transition: color 0.15s;
}
.si-drawer__logo:hover .si-drawer__burger {
  color: var(--text) !important;
}

.si-drawer__logo-text {
  font-size: 0.92rem;
  font-weight: 600;
  color: var(--text);
  white-space: nowrap;
  overflow: hidden;
}

.si-drawer__logo-dim {
  color: var(--text-3);
  font-weight: 400;
}

.si-drawer__divider {
  height: 1px;
  background: var(--border);
  margin: 0 0 4px;
}

/* ── Nav list ──────────────────────────────────────────────────── */
.si-nav-list {
  padding-top: 4px !important;
}

.si-nav-item {
  min-height: 38px !important;
  margin-bottom: 1px !important;
  border-radius: var(--radius) !important;
  transition: background 0.15s !important;
}

.si-nav-item:hover {
  background: var(--surface-hover) !important;
}

:deep(.v-list-item--active.si-nav-item) {
  background: var(--accent-dim) !important;
}

:deep(.v-list-item--active.si-nav-item) .si-nav-item__icon {
  color: var(--accent) !important;
}

:deep(.v-list-item--active.si-nav-item) .si-nav-item__label {
  color: var(--accent) !important;
  font-weight: 600;
}

.si-nav-item__icon {
  color: var(--text-3) !important;
  transition: color 0.15s !important;
  flex-shrink: 0;
}

.si-nav-item:hover .si-nav-item__icon {
  color: var(--text-2) !important;
}

.si-nav-item__label {
  font-family: var(--font);
  font-size: 0.92rem;
  font-weight: 500;
  color: var(--text-2);
  transition: color 0.15s;
}

.si-nav-item:hover .si-nav-item__label {
  color: var(--text);
}

/* ── App bar ───────────────────────────────────────────────────── */
.si-appbar {
  background: var(--bg) !important;
  border-bottom: 1px solid var(--border) !important;
  box-shadow: none !important;
}

.si-appbar__nav {
  color: var(--text-2) !important;
}

.si-appbar__breadcrumb {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-left: 8px;
}

.si-appbar__page {
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--text);
  letter-spacing: -0.01em;
}

.si-appbar__btn {
  color: var(--text-3) !important;
  border-radius: var(--radius) !important;
  transition: color 0.15s, background 0.15s !important;
}

.si-appbar__btn:hover {
  color: var(--accent) !important;
  background: var(--accent-dim) !important;
}

.si-appbar__btn--active {
  color: var(--accent) !important;
  background: var(--accent-dim) !important;
}

/* ── Main ──────────────────────────────────────────────────────── */
.si-main {
  background: var(--bg) !important;
}

/* ── Transitions ───────────────────────────────────────────────── */
.si-fade-enter-active,
.si-fade-leave-active {
  transition: opacity 0.15s, transform 0.15s;
}
.si-fade-enter-from,
.si-fade-leave-to {
  opacity: 0;
  transform: translateX(-6px);
}
</style>
