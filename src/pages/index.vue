<route lang="yaml">
meta:
  layout: default
</route>

<script setup lang="ts">
import ManagerStateWidget from '@/components/ManagerStateWidget.vue'
import WorkerStateWidget from '@/components/WorkerStateWidget.vue'

const items = [
  {
    title: 'Dashboard',
    to: '/dashboard',
    icon: 'mdi-view-dashboard-outline',
    desc: 'Live KPIs, agent status, buffer & error overview',
    accent: '#00c8ff',
  },
  {
    title: 'Fleet Management',
    to: '/fleet-management',
    icon: 'mdi-robot-industrial-outline',
    desc: 'Full telemetry table for all connected agents',
    accent: '#10b981',
  },
  {
    title: 'Awareness Map',
    to: '/awarenessmap',
    icon: 'mdi-map-marker-multiple-outline',
    desc: 'Read-only spatial visualisation of the swarm field',
    accent: '#60a5fa',
  },
  {
    title: 'Command Center',
    to: '/command-center',
    icon: 'mdi-crosshairs-gps',
    desc: 'Interactive map for planning formations & paths',
    accent: '#a78bfa',
  },
  {
    title: 'Drive Control',
    to: '/drive-control',
    icon: 'mdi-car-connected',
    desc: 'Command state timeline and latency metrics',
    accent: '#f59e0b',
  },
  {
    title: 'Lead Vehicle',
    to: '/leadvehicle-detail',
    icon: 'mdi-crown-outline',
    desc: 'Heading, speed, position and live camera stream',
    accent: '#f472b6',
  },
  {
    title: 'Event Log',
    to: '/event-log',
    icon: 'mdi-text-box-multiple-outline',
    desc: 'Real-time categorised event stream from the swarm',
    accent: '#22d3ee',
  },
  {
    title: 'Settings',
    to: '/settings',
    icon: 'mdi-cog-outline',
    desc: 'System configuration and preferences',
    accent: '#6b7280',
  },
]
</script>

<template>
  <!-- hidden state widgets (needed for internal subscriptions) -->
  <manager-state-widget style="display:none" />
  <worker-state-widget  style="display:none" />

  <div class="si-home">

    <!-- ── Header ─────────────────────────────────────────────────── -->
    <div class="si-home__header">
      <div class="si-home__hex">
        <v-icon icon="mdi-hexagon-multiple" size="28" />
      </div>
      <div>
        <div class="si-home__title">SWARM INTELLIGENCE</div>
        <div class="si-home__sub">Multi-agent autonomous control system · select a module</div>
      </div>
    </div>

    <!-- ── Grid ───────────────────────────────────────────────────── -->
    <div class="si-home__grid">
      <RouterLink
        v-for="item in items"
        :key="item.to"
        :to="item.to"
        class="si-module"
        :style="{ '--accent': item.accent }"
      >
        <div class="si-module__corner si-module__corner--tl" />
        <div class="si-module__corner si-module__corner--br" />

        <div class="si-module__icon-wrap">
          <v-icon :icon="item.icon" size="26" class="si-module__icon" />
        </div>

        <div class="si-module__body">
          <div class="si-module__title">{{ item.title }}</div>
          <div class="si-module__desc">{{ item.desc }}</div>
        </div>

        <div class="si-module__arrow">
          <v-icon icon="mdi-chevron-right" size="16" />
        </div>
      </RouterLink>
    </div>
  </div>
</template>

<style scoped>
.si-home {
  padding: 28px 28px 40px;
  min-height: 100%;
  background: var(--si-bg);
}

/* ── Header ─────────────────────────────────────────────────────── */
.si-home__header {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 32px;
}

.si-home__hex {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 52px;
  height: 52px;
  background: rgba(0, 200, 255, 0.08);
  border: 1px solid rgba(0, 200, 255, 0.3);
  color: var(--si-cyan);
  clip-path: polygon(50% 0%, 100% 25%, 100% 75%, 50% 100%, 0% 75%, 0% 25%);
  flex-shrink: 0;
}

.si-home__title {
  font-family: var(--si-font-display);
  font-size: 1.1rem;
  font-weight: 700;
  letter-spacing: 0.15em;
  color: var(--si-text-primary);
}

.si-home__sub {
  font-family: var(--si-font-mono);
  font-size: 0.65rem;
  letter-spacing: 0.08em;
  color: var(--si-text-muted);
  margin-top: 4px;
}

/* ── Grid ───────────────────────────────────────────────────────── */
.si-home__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 12px;
}

/* ── Module card ────────────────────────────────────────────────── */
.si-module {
  --accent: #00c8ff;
  position: relative;
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px 14px;
  background: var(--si-surface);
  border: 1px solid rgba(255,255,255,0.06);
  text-decoration: none;
  cursor: pointer;
  transition: border-color 0.2s, background 0.2s, box-shadow 0.2s;
  overflow: hidden;
}

.si-module::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(var(--accent-r,0),var(--accent-g,200),var(--accent-b,255),0.04) 0%, transparent 60%);
  opacity: 0;
  transition: opacity 0.2s;
}

.si-module:hover {
  border-color: var(--accent);
  background: var(--si-surface-alt);
  box-shadow: 0 0 20px rgba(0,0,0,0.3), inset 0 0 20px rgba(0,0,0,0.1);
}

.si-module:hover::before {
  opacity: 1;
}

/* Corner decoration */
.si-module__corner {
  position: absolute;
  width: 8px;
  height: 8px;
  border-color: var(--accent);
  border-style: solid;
  opacity: 0;
  transition: opacity 0.2s;
}
.si-module:hover .si-module__corner { opacity: 1; }

.si-module__corner--tl {
  top: 4px; left: 4px;
  border-width: 1px 0 0 1px;
}
.si-module__corner--br {
  bottom: 4px; right: 4px;
  border-width: 0 1px 1px 0;
}

/* Icon */
.si-module__icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  background: rgba(0, 0, 0, 0.3);
  border: 1px solid rgba(255,255,255,0.06);
  flex-shrink: 0;
  transition: border-color 0.2s, background 0.2s;
}

.si-module:hover .si-module__icon-wrap {
  border-color: var(--accent);
  background: rgba(0, 0, 0, 0.5);
}

.si-module__icon {
  color: var(--accent) !important;
}

/* Body */
.si-module__body {
  flex: 1;
  min-width: 0;
}

.si-module__title {
  font-family: var(--si-font-body);
  font-size: 0.85rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  color: var(--si-text-primary);
  margin-bottom: 3px;
}

.si-module__desc {
  font-family: var(--si-font-body);
  font-size: 0.7rem;
  color: var(--si-text-muted);
  line-height: 1.4;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Arrow */
.si-module__arrow {
  color: var(--si-text-dim);
  flex-shrink: 0;
  transition: color 0.2s, transform 0.2s;
}

.si-module:hover .si-module__arrow {
  color: var(--accent);
  transform: translateX(3px);
}
</style>
