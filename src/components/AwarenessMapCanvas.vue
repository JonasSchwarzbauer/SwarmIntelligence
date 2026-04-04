<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, watch } from 'vue';
import { Application, Graphics, Text, Container, FederatedPointerEvent } from 'pixi.js';
import type { AgentDataDto, MapUpdatedDto, CellInfo, VirtualObstaclesDto } from '@/models/models';
import { ObstacleType } from '@/models/models';
import { connection, subscribeToTopic, unsubscribeFromTopic } from '@/plugins/signalr';
import { useAppStore } from '@/stores/app';
import { apiUrl } from '@/config';

const appStore = useAppStore();

const TOPIC = 'AwarenessMap';

/* ── Palette ────────────────────────────────────────────────────────────── */
const C = {
  bg:           0x0d1117,
  cell:         0x161b22,
  gridLine:     0x21262d,
  axis:         0x58a6ff,
  physical:     0xff6b6b,
  virtual:      0xa78bfa,
  agentDefault: 0x34d399,
  agentActive:  0x60a5fa,
  agentWarn:    0xfbbf24,
  agentError:   0xf87171,
  arrow:        0xfbbf24,
  target:       0x60a5fa,
  frontal:      0xfde68a,
  formation:    0xc4b5fd,
} as const;

/* ── Reactive refs ──────────────────────────────────────────────────────── */
const containerRef  = ref<HTMLDivElement | null>(null);
const agentCount    = ref(0);
const physicalCount = ref(0);
const virtualCount  = ref(0);
const containerW    = ref(800);
const containerH    = ref(600);
const isLoading     = ref(true);
const fetchError    = ref(false);
const configOpen    = ref(false);

// Decimal places to show for coordinate labels — matches cellSize precision
const coordDecimals = computed(() => {
  const s = cellSize.value.toString();
  const dot = s.indexOf('.');
  return dot < 0 ? 0 : s.length - dot - 1;
});

const cfg = reactive({
  showAgentIds:      true,
  showTargetLines:   true,
  showTargetLabels:  false,
  showFrontalRays:   true,
  showFormationPath: true,
  showGridCoords:    false,
  hideOverlays:      false,
});

interface HoverInfo {
  cellX: number;
  cellY: number;
  mx: number;
  my: number;
  type: 'Free' | 'Physical' | 'Virtual';
  agentId: number | null;
}
const hoverInfo = ref<HoverInfo | null>(null);

/* ── PixiJS objects ─────────────────────────────────────────────────────── */
let app: Application | null = null;
let gridGfx: Graphics;
let obstaclesGfx: Graphics;
let agentsLayer: Container;
let formationLayer: Container;
let gridLabels: Container;
let resizeObs: ResizeObserver | null = null;

/* ── Domain state ───────────────────────────────────────────────────────── */
const physicalObstacles = new Set<string>();
const virtualObstacles  = new Set<string>();
const agentDataMap      = new Map<number, AgentDataDto>();

interface AgentDisplay {
  root: Container;
  body: Graphics;
  arrow: Graphics;
  targetGfx: Graphics;
  frontalGfx: Graphics;
  idLabel: Text;
  infoLabel: Text;
  targetLabel: Text;
}
const agentDisplayMap = new Map<number, AgentDisplay>();

/* ── Grid geometry ──────────────────────────────────────────────────────── */
const cellSize = computed(() => appStore.gridSize || 0.25);
const maxCol   = computed(() => appStore.yMax > 0 ? Math.floor(appStore.yMax / cellSize.value) : 2);
const maxRow   = computed(() => appStore.xMax > 0 ? Math.floor(appStore.xMax / cellSize.value) : 2);

const cellPx = computed(() => {
  const cols = maxCol.value;
  const rows = maxRow.value;
  if (cols <= 0 || rows <= 0) return 10;
  return Math.max(4, Math.min(containerW.value / cols, containerH.value / rows));
});

/* ── Coordinate helpers ─────────────────────────────────────────────────── */
// data-y → horizontal (canvas X), data-x → vertical (canvas Y)
const toCx = (dataY: number) => (dataY / cellSize.value) * cellPx.value;
const toCy = (dataX: number) => (dataX / cellSize.value) * cellPx.value;

/* ── Hover tracking ─────────────────────────────────────────────────────── */
const onPointerMove = (e: FederatedPointerEvent) => {
  if (!app) return;
  const pos = e.getLocalPosition(app.stage);
  const cp  = cellPx.value;
  const col = Math.floor(pos.x / cp);
  const row = Math.floor(pos.y / cp);

  if (col < 0 || col >= maxCol.value || row < 0 || row >= maxRow.value) {
    hoverInfo.value = null;
    return;
  }

  const key  = `${row},${col}`;
  const type: HoverInfo['type'] = physicalObstacles.has(key) ? 'Physical'
    : virtualObstacles.has(key) ? 'Virtual'
    : 'Free';

  const cs = cellSize.value;
  let agentId: number | null = null;
  for (const [id, a] of agentDataMap) {
    if (Math.floor(a.x / cs) === row && Math.floor(a.y / cs) === col) {
      agentId = id;
      break;
    }
  }

  hoverInfo.value = { cellX: row, cellY: col, mx: row * cs, my: col * cs, type, agentId };
};

const onPointerLeave = () => { hoverInfo.value = null; };

/* ── API fetch ──────────────────────────────────────────────────────────── */
const fetchInitialData = async () => {
  isLoading.value = true;
  fetchError.value = false;
  try {
    const [obstRes, agentRes] = await Promise.all([
      fetch(apiUrl('/api/cache/obstacles/cells?occupiedOnly=true')),
      fetch(apiUrl('/api/cache/agents')),
    ]);

    if (obstRes.ok) {
      const cells: CellInfo[] = await obstRes.json();
      physicalObstacles.clear();
      virtualObstacles.clear();
      for (const c of cells) {
        const key = `${c.x},${c.y}`;
        if (c.type === ObstacleType.Physical) physicalObstacles.add(key);
        else if (c.type === ObstacleType.Virtual) virtualObstacles.add(key);
      }
      updateObstacleCounts();
      drawObstacles();
    }

    if (agentRes.ok) {
      const agents: AgentDataDto[] = await agentRes.json();
      agentDataMap.clear();
      for (const a of agents) {
        agentDataMap.set(a.agentId, a);
        renderAgent(a);
      }
      agentCount.value = agentDataMap.size;
    }
  } catch {
    fetchError.value = true;
    console.error('Failed to fetch initial awareness map data');
  } finally {
    isLoading.value = false;
  }
};

const updateObstacleCounts = () => {
  physicalCount.value = physicalObstacles.size;
  virtualCount.value  = virtualObstacles.size;
};

/* ── Lifecycle ──────────────────────────────────────────────────────────── */
onMounted(async () => {
  if (!containerRef.value) return;

  app = new Application();
  await app.init({
    background: C.bg,
    antialias: true,
    width: containerRef.value.clientWidth,
    height: containerRef.value.clientHeight,
  });
  containerRef.value.appendChild(app.canvas as HTMLCanvasElement);

  gridGfx        = new Graphics();
  obstaclesGfx   = new Graphics();
  agentsLayer    = new Container();
  formationLayer = new Container();
  gridLabels     = new Container();

  app.stage.addChild(gridGfx);
  app.stage.addChild(obstaclesGfx);
  app.stage.addChild(agentsLayer);
  app.stage.addChild(formationLayer);
  app.stage.addChild(gridLabels);

  // Enable pointer events for hover tracking
  app.stage.eventMode = 'static';
  app.stage.hitArea   = app.screen;
  app.stage.on('pointermove',  onPointerMove);
  app.stage.on('pointerleave', onPointerLeave);

  containerW.value = containerRef.value.clientWidth;
  containerH.value = containerRef.value.clientHeight;

  resizeObs = new ResizeObserver(entries => {
    for (const e of entries) {
      const w = Math.floor(e.contentRect.width);
      const h = Math.floor(e.contentRect.height);
      if (w < 1 || h < 1) continue;
      containerW.value = w;
      containerH.value = h;
      app?.renderer.resize(w, h);
      if (app) app.stage.hitArea = app.screen;
      redrawAll();
    }
  });
  resizeObs.observe(containerRef.value);

  drawGrid();
  drawObstacles();

  watch([() => appStore.xMax, () => appStore.yMax, () => appStore.gridSize], () => redrawAll());
  watch(cfg, () => redrawAll(), { deep: true });
  watch(() => appStore.lastFormationPath, () => drawFormationPath());

  subscribeToTopic(TOPIC);
  connection.on('AgentData',               handleAgentUpdate);
  connection.on('MapUpdated',              handleMapUpdate);
  connection.on('VirtualObstaclesUpdated', handleVirtualObstacles);

  await fetchInitialData();
});

onUnmounted(() => {
  resizeObs?.disconnect();
  unsubscribeFromTopic(TOPIC);
  connection.off('AgentData',               handleAgentUpdate);
  connection.off('MapUpdated',              handleMapUpdate);
  connection.off('VirtualObstaclesUpdated', handleVirtualObstacles);
  app?.destroy(true);
  app = null;
});

/* ── SignalR handlers ───────────────────────────────────────────────────── */
const handleMapUpdate = (dto: MapUpdatedDto) => {
  const key = `${dto.cellX},${dto.cellY}`;
  if (dto.occupied) physicalObstacles.add(key);
  else              physicalObstacles.delete(key);
  updateObstacleCounts();
  drawObstacles();
};

const handleAgentUpdate = (dto: AgentDataDto) => {
  agentDataMap.set(dto.agentId, dto);
  agentCount.value = agentDataMap.size;
  renderAgent(dto);
};

const handleVirtualObstacles = (dto: VirtualObstaclesDto) => {
  for (const obs of dto.obstacles) {
    const key = `${obs.x},${obs.y}`;
    if (obs.type === ObstacleType.Virtual) virtualObstacles.add(key);
    else virtualObstacles.delete(key);
  }
  updateObstacleCounts();
  drawObstacles();
};

/* ── Full redraw ────────────────────────────────────────────────────────── */
const redrawAll = () => {
  drawGrid();
  drawObstacles();
  drawFormationPath();
  for (const a of agentDataMap.values()) renderAgent(a);
};

/* ── Grid drawing ───────────────────────────────────────────────────────── */
const drawGrid = () => {
  if (!gridGfx) return;
  gridGfx.clear();

  const cp     = cellPx.value;
  const cols   = maxCol.value;
  const rows   = maxRow.value;
  const totalW = cols * cp;
  const totalH = rows * cp;

  gridGfx.rect(0, 0, totalW, totalH);
  gridGfx.fill(C.cell);

  for (let c = 0; c <= cols; c++) {
    gridGfx.moveTo(c * cp, 0);
    gridGfx.lineTo(c * cp, totalH);
  }
  for (let r = 0; r <= rows; r++) {
    gridGfx.moveTo(0, r * cp);
    gridGfx.lineTo(totalW, r * cp);
  }
  gridGfx.stroke({ width: 0.5, color: C.gridLine, alpha: 0.5 });

  gridGfx.moveTo(0, 0).lineTo(totalW, 0);
  gridGfx.moveTo(0, 0).lineTo(0, totalH);
  gridGfx.stroke({ width: 2, color: C.axis, alpha: 0.6 });

  // Axis coordinate labels
  if (gridLabels) {
    gridLabels.removeChildren();
    if (cfg.showGridCoords && cp >= 6) {
      const step = Math.max(1, Math.round(50 / cp));
      const cs   = cellSize.value;
      const fsz  = Math.max(8, Math.min(11, cp * 0.7));

      // Labels placed just inside the grid edges (positive canvas coords)
      for (let c = 0; c < cols; c += step) {
        const t = new Text({ text: `${(c * cs).toFixed(coordDecimals.value)}`, style: { fontSize: fsz, fill: 0x58a6ff, fontFamily: 'monospace' } });
        t.alpha = 0.7;
        t.anchor.set(0.5, 0);
        t.position.set(c * cp + cp / 2, 3);
        gridLabels.addChild(t);
      }
      for (let r = 0; r < rows; r += step) {
        const t = new Text({ text: `${(r * cs).toFixed(coordDecimals.value)}`, style: { fontSize: fsz, fill: 0x58a6ff, fontFamily: 'monospace' } });
        t.alpha = 0.7;
        t.anchor.set(0, 0.5);
        t.position.set(3, r * cp + cp / 2);
        gridLabels.addChild(t);
      }
    }
  }
};

/* ── Obstacle drawing ───────────────────────────────────────────────────── */
const drawObstacles = () => {
  if (!obstaclesGfx) return;
  obstaclesGfx.clear();

  const cp = cellPx.value;

  for (const key of physicalObstacles) {
    const [cx, cy] = key.split(',').map(Number) as [number, number];
    obstaclesGfx.rect(cy * cp, cx * cp, cp, cp);
  }
  if (physicalObstacles.size) obstaclesGfx.fill({ color: C.physical, alpha: 0.75 });

  for (const key of virtualObstacles) {
    const [cx, cy] = key.split(',').map(Number) as [number, number];
    obstaclesGfx.rect(cy * cp, cx * cp, cp, cp);
  }
  if (virtualObstacles.size) obstaclesGfx.fill({ color: C.virtual, alpha: 0.55 });
};

/* ── Formation path drawing ─────────────────────────────────────────────── */
const drawFormationPath = () => {
  if (!formationLayer) return;
  formationLayer.removeChildren();
  const path = appStore.lastFormationPath;
  if (!cfg.showFormationPath || !path?.length) return;

  const cp = cellPx.value;

  // Connecting lines — Position stores grid cell indices (integers)
  if (path.length > 1) {
    const lineGfx = new Graphics();
    lineGfx.moveTo(path[0]!.Position.Y * cp, path[0]!.Position.X * cp);
    for (let i = 1; i < path.length; i++) {
      lineGfx.lineTo(path[i]!.Position.Y * cp, path[i]!.Position.X * cp);
    }
    lineGfx.stroke({ width: 1.5, color: C.formation, alpha: 0.5 });
    formationLayer.addChild(lineGfx);
  }

  // Numbered pins
  const pinR = Math.max(5, cp * 0.35);
  for (let i = 0; i < path.length; i++) {
    const wx = path[i]!.Position.Y * cp;
    const wy = path[i]!.Position.X * cp;

    const pin = new Graphics();
    pin.circle(wx, wy, pinR);
    pin.fill({ color: C.formation, alpha: 0.8 });
    pin.circle(wx, wy, pinR);
    pin.stroke({ width: 1.5, color: 0xffffff, alpha: 0.3 });
    formationLayer.addChild(pin);

    const label = new Text({
      text: `${i + 1}`,
      style: { fontSize: Math.max(8, Math.min(11, pinR)), fill: 0xffffff, fontWeight: 'bold', fontFamily: 'monospace' },
    });
    label.anchor.set(0.5, 0.5);
    label.position.set(wx, wy);
    formationLayer.addChild(label);
  }
};

/* ── Agent helpers ──────────────────────────────────────────────────────── */
const agentColor = (flags: string[]): number => {
  if (flags.some(f => f === 'StoppedSensorTimeout')) return C.agentError;
  if (flags.some(f => f.startsWith('Stopped')))      return C.agentWarn;
  if (flags.includes('WaypointActive'))               return C.agentActive;
  return C.agentDefault;
};

/* ── Agent rendering ────────────────────────────────────────────────────── */
const renderAgent = (agent: AgentDataDto) => {
  if (!agentsLayer) return;

  let d = agentDisplayMap.get(agent.agentId);

  if (!d) {
    const root       = new Container();
    const targetGfx  = new Graphics();
    const frontalGfx = new Graphics();
    const body       = new Graphics();
    const arrow      = new Graphics();

    const idLabel = new Text({
      text: `#${agent.agentId}`,
      style: { fontSize: 10, fill: 0xffffff, fontWeight: 'bold', fontFamily: 'monospace' },
    });
    idLabel.anchor.set(0.5, 0.5);

    const infoLabel = new Text({
      text: '',
      style: { fontSize: 8, fill: 0x8b949e, fontFamily: 'monospace' },
    });
    infoLabel.anchor.set(0.5, 0.5);

    const targetLabel = new Text({
      text: '',
      style: { fontSize: 8, fill: 0x60a5fa, fontFamily: 'monospace' },
    });
    targetLabel.anchor.set(0, 0.5);

    root.addChild(targetGfx, frontalGfx, body, arrow, idLabel, infoLabel, targetLabel);
    agentsLayer.addChild(root);

    d = { root, body, arrow, targetGfx, frontalGfx, idLabel, infoLabel, targetLabel };
    agentDisplayMap.set(agent.agentId, d);
  }

  const cp         = cellPx.value;
  const r          = cp * 0.4;
  const showLabels = cp >= 12;

  d.root.position.set(toCx(agent.y), toCy(agent.x));

  const col = agentColor(agent.flags);
  d.body.clear();
  d.body.circle(0, 0, r);
  d.body.fill({ color: col, alpha: 0.9 });
  d.body.circle(0, 0, r);
  d.body.stroke({ width: 1.5, color: 0x000000, alpha: 0.2 });

  const rad      = agent.orientation + Math.PI / 2;
  const arrowLen = r * 2;
  const tipX     = Math.cos(rad) * arrowLen;
  const tipY     = Math.sin(rad) * arrowLen;

  d.arrow.clear();
  d.arrow.moveTo(0, 0).lineTo(tipX, tipY);
  d.arrow.stroke({ width: 2.5, color: C.arrow, alpha: 0.9 });

  const hl = r * 0.45;
  const a1 = rad + Math.PI * 0.8;
  const a2 = rad - Math.PI * 0.8;
  d.arrow.moveTo(tipX, tipY).lineTo(tipX + Math.cos(a1) * hl, tipY + Math.sin(a1) * hl);
  d.arrow.moveTo(tipX, tipY).lineTo(tipX + Math.cos(a2) * hl, tipY + Math.sin(a2) * hl);
  d.arrow.stroke({ width: 2, color: C.arrow, alpha: 0.9 });

  // Target waypoint line
  d.targetGfx.clear();
  d.targetLabel.visible = false;
  if (cfg.showTargetLines && agent.target) {
    const tx   = toCx(agent.target.y) - toCx(agent.y);
    const ty   = toCy(agent.target.x) - toCy(agent.x);
    const dist = Math.sqrt(tx * tx + ty * ty);
    if (dist > 3) {
      d.targetGfx.moveTo(0, 0).lineTo(tx, ty);
      d.targetGfx.stroke({ width: 1, color: C.target, alpha: 0.35 });
      d.targetGfx.circle(tx, ty, Math.max(2, r * 0.35));
      d.targetGfx.stroke({ width: 1.5, color: C.target, alpha: 0.6 });

      if (cfg.showTargetLabels && showLabels) {
        d.targetLabel.text = `(${agent.target.x.toFixed(2)}, ${agent.target.y.toFixed(2)})`;
        d.targetLabel.position.set(tx + 5, ty);
        d.targetLabel.visible = true;
      }
    }
  }

  // Frontal distance ray
  d.frontalGfx.clear();
  if (cfg.showFrontalRays && agent.frontalDistance > 0) {
    const fdPx   = (agent.frontalDistance / cellSize.value) * cp;
    const capped = Math.min(fdPx, cp * 15);
    d.frontalGfx.moveTo(0, 0);
    d.frontalGfx.lineTo(Math.cos(rad) * capped, Math.sin(rad) * capped);
    d.frontalGfx.stroke({ width: 1, color: C.frontal, alpha: 0.15 });
  }

  d.idLabel.visible   = cfg.showAgentIds && showLabels;
  d.infoLabel.visible = showLabels;

  if (showLabels) {
    d.idLabel.text = `#${agent.agentId}`;
    d.idLabel.position.set(0, -(r + 8));

    const vel = agent.velocity.toFixed(2);
    const dwm = (agent.dwmSuccessRate * 100).toFixed(0);
    d.infoLabel.text = `${vel} m/s · DWM ${dwm}%`;
    d.infoLabel.position.set(0, r + 8);
  }
};
</script>

<template>
  <div ref="containerRef" class="map-container">
    <!-- Top-left: Stats -->
    <div v-show="!cfg.hideOverlays" class="overlay stats-panel">
      <div class="panel-row">
        <v-icon icon="mdi-robot" size="14" color="green-lighten-2" />
        <span class="panel-value">{{ agentCount }}</span>
        <span class="panel-label">Agents</span>
      </div>
      <div class="panel-row">
        <span class="swatch physical-sw" />
        <span class="panel-value">{{ physicalCount }}</span>
        <span class="panel-label">Physical</span>
      </div>
      <div class="panel-row">
        <span class="swatch virtual-sw" />
        <span class="panel-value">{{ virtualCount }}</span>
        <span class="panel-label">Virtual</span>
      </div>
    </div>

    <!-- Top-right: Grid info + config toggle -->
    <div class="top-right-controls">
      <div class="grid-info-row">
        <div class="overlay grid-info-chip">
          <span class="panel-label">{{ maxCol }} × {{ maxRow }} cells · {{ cellSize * 100 }}cm</span>
        </div>
        <button class="config-toggle" :class="{ active: configOpen }" @click="configOpen = !configOpen" title="Display settings">
          <v-icon size="14" :icon="configOpen ? 'mdi-cog' : 'mdi-cog-outline'" />
        </button>
      </div>

      <!-- Config panel -->
      <div v-if="configOpen" class="config-panel">
        <div class="config-title">Display</div>
        <label class="cfg-row"><input type="checkbox" v-model="cfg.showAgentIds" />Agent IDs</label>
        <label class="cfg-row"><input type="checkbox" v-model="cfg.showTargetLines" />Target waypoint lines</label>
        <label class="cfg-row"><input type="checkbox" v-model="cfg.showTargetLabels" />Target waypoint labels</label>
        <label class="cfg-row"><input type="checkbox" v-model="cfg.showFrontalRays" />Frontal distance rays</label>
        <label class="cfg-row"><input type="checkbox" v-model="cfg.showFormationPath" />Formation path</label>
        <label class="cfg-row"><input type="checkbox" v-model="cfg.showGridCoords" />Grid coordinates</label>
        <div class="cfg-divider"></div>
        <label class="cfg-row cfg-row-dim"><input type="checkbox" v-model="cfg.hideOverlays" />Hide all overlays</label>
      </div>
    </div>

    <!-- Legend: positioned just below the grid content -->
    <div v-show="!cfg.hideOverlays" class="overlay legend-panel">
      <div class="legend-row"><span class="swatch physical-sw" /> Physical obstacle</div>
      <div class="legend-row"><span class="swatch virtual-sw" />  Virtual obstacle</div>
      <div class="legend-row"><span class="swatch agent-sw" />    Agent</div>
      <div class="legend-row"><span class="swatch arrow-sw" />    Orientation</div>
      <div class="legend-row"><span class="swatch target-sw" />   Waypoint target</div>
      <div class="legend-row"><span class="swatch axis-sw" />     Origin axes</div>
    </div>

    <!-- Bottom-right: Hover info -->
    <div v-if="hoverInfo && !cfg.hideOverlays" class="overlay hover-info">
      <span class="hi-cell">({{ hoverInfo.cellX }}, {{ hoverInfo.cellY }})</span>
      <span class="hi-sep"> · </span>
      <span class="hi-meters">{{ hoverInfo.mx.toFixed(2) }}m, {{ hoverInfo.my.toFixed(2) }}m</span>
      <span class="hi-sep"> · </span>
      <span class="hi-type" :class="`hi-${hoverInfo.type.toLowerCase()}`">{{ hoverInfo.type }}</span>
      <template v-if="hoverInfo.agentId !== null">
        <span class="hi-sep"> · </span>
        <span class="hi-agent">Agent #{{ hoverInfo.agentId }}</span>
      </template>
    </div>

    <!-- Loading / error indicator -->
    <div v-if="isLoading" class="overlay center-overlay">
      <v-progress-circular indeterminate size="28" width="3" color="primary" />
    </div>
    <div v-else-if="fetchError" class="overlay center-overlay">
      <v-chip size="small" color="error" variant="flat" prepend-icon="mdi-alert-circle">
        Failed to load initial data
      </v-chip>
    </div>
  </div>
</template>

<style scoped>
.map-container {
  width: 100%;
  height: 100%;
  flex: 1 1 0;
  min-height: 0;
  position: relative;
  overflow: hidden;
  background-color: #0d1117;
}
.map-container :deep(canvas) {
  display: block;
  width: 100%;
  height: 100%;
}

/* ── Shared overlay base ─────────────────────────────────────────── */
.overlay {
  position: absolute;
  pointer-events: none;
  z-index: 10;
  background: rgba(13, 17, 23, 0.82);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(48, 54, 61, 0.65);
  border-radius: 10px;
  padding: 8px 13px;
  color: rgba(255, 255, 255, 0.85);
  font-size: 13px;
  font-family: 'Roboto Mono', monospace;
}

/* ── Positioning ─────────────────────────────────────────────────── */
/* Stats + legend both on the left, stacked from top */
.stats-panel  { top: 12px; left: 12px; display: flex; flex-direction: column; gap: 5px; }
/* Legend sits just below the stats panel (~110px from top) */
.legend-panel { top: 115px; left: 12px; display: flex; flex-direction: column; gap: 4px; }
/* Hover info top-right (below config controls) */
.hover-info   { top: 52px; right: 12px; display: flex; align-items: center; flex-wrap: wrap; gap: 3px; }
.center-overlay {
  top: 50%; left: 50%;
  transform: translate(-50%, -50%);
  background: rgba(13, 17, 23, 0.9);
}

/* ── Top-right controls ──────────────────────────────────────────── */
.top-right-controls {
  position: absolute;
  top: 10px;
  right: 10px;
  z-index: 11;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 6px;
  pointer-events: none;
}
.grid-info-row {
  display: flex;
  align-items: center;
  gap: 4px;
  pointer-events: none;
}
.grid-info-chip {
  position: relative;
  top: unset;
  right: unset;
  padding: 4px 8px;
  pointer-events: none;
}
.config-toggle {
  pointer-events: auto;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(13, 17, 23, 0.75);
  backdrop-filter: blur(6px);
  border: 1px solid rgba(48, 54, 61, 0.6);
  border-radius: 6px;
  color: rgba(255, 255, 255, 0.5);
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s;
}
.config-toggle:hover,
.config-toggle.active {
  color: rgba(255, 255, 255, 0.9);
  border-color: rgba(88, 166, 255, 0.5);
}

/* ── Config panel ────────────────────────────────────────────────── */
.config-panel {
  pointer-events: auto;
  background: rgba(13, 17, 23, 0.9);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(48, 54, 61, 0.7);
  border-radius: 8px;
  padding: 8px 12px;
  min-width: 190px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.config-title {
  font-size: 11px;
  font-family: 'Roboto Mono', monospace;
  color: rgba(255, 255, 255, 0.35);
  text-transform: uppercase;
  letter-spacing: 0.08em;
  margin-bottom: 3px;
}
.cfg-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  font-family: 'Roboto Mono', monospace;
  color: rgba(255, 255, 255, 0.75);
  cursor: pointer;
  user-select: none;
}
.cfg-row input[type="checkbox"] {
  accent-color: #58a6ff;
  width: 13px;
  height: 13px;
  cursor: pointer;
}
.cfg-divider {
  height: 1px;
  background: rgba(48,54,61,0.5);
  margin: 3px 0;
}
.cfg-row-dim { color: rgba(255,255,255,0.4); }

/* ── Hover info ──────────────────────────────────────────────────── */
.hi-cell    { font-size: 12px; color: rgba(255,255,255,0.9); }
.hi-sep     { color: rgba(255,255,255,0.3); }
.hi-meters  { font-size: 11px; color: rgba(255,255,255,0.55); }
.hi-type    { font-size: 12px; font-weight: 600; }
.hi-free      { color: rgba(255,255,255,0.5); }
.hi-physical  { color: #ff6b6b; }
.hi-virtual   { color: #a78bfa; }
.hi-agent   { font-size: 12px; color: #34d399; }

/* ── Stats rows ──────────────────────────────────────────────────── */
.panel-row {
  display: flex;
  align-items: center;
  gap: 7px;
}
.panel-value {
  font-weight: 700;
  font-size: 14px;
  min-width: 24px;
  text-align: right;
}
.panel-label {
  color: rgba(255, 255, 255, 0.55);
  font-size: 12px;
}

/* ── Swatches ────────────────────────────────────────────────────── */
.swatch {
  display: inline-block;
  width: 11px;
  height: 11px;
  border-radius: 2px;
  flex-shrink: 0;
}
.physical-sw { background-color: #ff6b6b; }
.virtual-sw  { background-color: #a78bfa; }
.agent-sw    { background-color: #34d399; border-radius: 50%; }
.arrow-sw    { background-color: #fbbf24; border-radius: 1px; height: 3px; width: 13px; }
.target-sw   { background-color: #60a5fa; border-radius: 50%; width: 9px; height: 9px; border: 1px solid #60a5fa; background: transparent; }
.axis-sw     { background-color: #58a6ff; border-radius: 1px; height: 3px; }

/* ── Legend ───────────────────────────────────────────────────────── */
.legend-row {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 12px;
  color: rgba(255, 255, 255, 0.55);
}
</style>
