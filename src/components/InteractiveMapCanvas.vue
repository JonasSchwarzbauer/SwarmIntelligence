<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { Application, Graphics, Text, Container, FederatedPointerEvent } from 'pixi.js';
import type { CellInfo, ObstacleCellDto, AgentDataDto } from '@/models/models';
import { ObstacleType } from '@/models/models';
import type { FormationPathDto, VehicleTargetsDto } from '@/models/user-inputs';
import { FormationShapes } from '@/models/user-inputs';
import { connection, subscribeToTopic, unsubscribeFromTopic } from '@/plugins/signalr';
import { useAppStore } from '@/stores/app';
import { apiUrl } from '@/config';

const appStore = useAppStore();

const TOPIC = 'AwarenessMap';

/* ── Palette ────────────────────────────────────────────────────────────── */
const C = {
  bg:           0x0d1117,
  cell:         0x161b22,
  cellHov:      0x1c2333,
  gridLine:     0x21262d,
  axis:         0x58a6ff,
  physical:     0xff6b6b,
  virtual:      0xa78bfa,
  pending:      0xc4b5fd,
  wpForm:       0xc4b5fd,
  wpVeh:        0x34d399,
  agentDefault: 0x34d399,
  agentActive:  0x60a5fa,
  agentWarn:    0xfbbf24,
  agentError:   0xf87171,
} as const;

/* ── Reactive state ─────────────────────────────────────────────────────── */
const containerRef  = ref<HTMLDivElement | null>(null);
const physicalCount = ref(0);
const virtualCount  = ref(0);
const pendingCount  = ref(0);
const containerW    = ref(800);
const containerH    = ref(600);
const isLoading     = ref(true);
const fetchError    = ref(false);
const editing       = ref(false);
const saving        = ref(false);
const hasChanges    = ref(false);

type CanvasMode = 'obstacles' | 'formationPath' | 'vehicleTarget';
const canvasMode = ref<CanvasMode>('obstacles');

interface HoverInfo { cellX: number; cellY: number; mx: number; my: number; }
const hoverInfo = ref<HoverInfo | null>(null);

// ── Formation Path ───────────────────────────────────────────────────────
interface MapWaypoint { cellX: number; cellY: number; heading: number; maxSpeed: number; }
const formationWaypoints   = ref<MapWaypoint[]>([]);
const formationShape       = ref<FormationShapes>(FormationShapes.Line);
const formationSize        = ref(1);
const formationSending     = ref(false);

// ── Vehicle Target ────────────────────────────────────────────────────────
const vehicleWaypoints  = ref<MapWaypoint[]>([]);
const vehicleAgentId    = ref<number | null>(null);
const vehicleOverride   = ref(false);
const vehicleSending    = ref(false);
const agentIds          = ref<number[]>([0, 1, 2, 3, 4, 5]);

const shapeOptions = [
  { title: 'Line',     value: FormationShapes.Line },
  { title: 'Square',   value: FormationShapes.Square },
  { title: 'Box',      value: FormationShapes.Box },
  { title: 'Triangle', value: FormationShapes.Triangle },
  { title: 'V',        value: FormationShapes.V },
  { title: 'Circle',   value: FormationShapes.Circle },
];

/* ── PixiJS objects ─────────────────────────────────────────────────────── */
let app: Application | null = null;
let gridGfx: Graphics;
let obstaclesGfx: Graphics;
let hoverGfx: Graphics;
let interactionArea: Graphics;
let agentsLayer: Container;
let waypointLayer: Container;
let resizeObs: ResizeObserver | null = null;

/* ── Domain state ───────────────────────────────────────────────────────── */
const physicalObstacles = new Set<string>();
const virtualObstacles  = new Set<string>();
const pendingVirtual    = new Set<string>();
let   snapshotVirtual   = new Set<string>();

const agentDataMap    = new Map<number, AgentDataDto>();
interface AgentDot { root: Container; body: Graphics; label: Text; }
const agentDotMap = new Map<number, AgentDot>();

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
const toCx = (dataY: number) => (dataY / cellSize.value) * cellPx.value;
const toCy = (dataX: number) => (dataX / cellSize.value) * cellPx.value;

/* ── Pointer → cell ─────────────────────────────────────────────────────── */
const canvasToCell = (px: number, py: number): [number, number] | null => {
  const cp  = cellPx.value;
  const col = Math.floor(px / cp);
  const row = Math.floor(py / cp);
  if (col < 0 || col >= maxCol.value || row < 0 || row >= maxRow.value) return null;
  return [row, col];
};

/* ── Obstacle edit mode ─────────────────────────────────────────────────── */
let painting   = false;
let paintMode: 'add' | 'remove' = 'add';
let hoveredCell: string | null = null;

const startEditing = () => {
  snapshotVirtual = new Set(virtualObstacles);
  pendingVirtual.clear();
  for (const k of virtualObstacles) pendingVirtual.add(k);
  hasChanges.value = false;
  editing.value = true;
  updateInteractionArea();
  drawObstacles();
};

const discardEdits = () => {
  pendingVirtual.clear();
  editing.value    = false;
  hasChanges.value = false;
  hoveredCell      = null;
  updateInteractionArea();
  drawObstacles();
  drawHover();
};

const resetField = () => {
  pendingVirtual.clear();
  hasChanges.value   = pendingVirtual.size !== snapshotVirtual.size ||
    [...pendingVirtual].some(k => !snapshotVirtual.has(k));
  pendingCount.value = pendingVirtual.size;
  drawObstacles();
};

const saveEdits = async () => {
  saving.value = true;
  try {
    const obstacles: ObstacleCellDto[] = [];
    for (const k of pendingVirtual) {
      if (!snapshotVirtual.has(k)) {
        const [x, y] = k.split(',').map(Number) as [number, number];
        obstacles.push({ x, y, type: ObstacleType.Virtual });
      }
    }
    for (const k of snapshotVirtual) {
      if (!pendingVirtual.has(k)) {
        const [x, y] = k.split(',').map(Number) as [number, number];
        obstacles.push({ x, y, type: ObstacleType.Free });
      }
    }
    await connection.invoke('OnVirtualObstaclesReceived', { obstacles });
    virtualObstacles.clear();
    for (const k of pendingVirtual) virtualObstacles.add(k);
    updateObstacleCounts();
    editing.value    = false;
    hasChanges.value = false;
    hoveredCell      = null;
    updateInteractionArea();
    drawObstacles();
    drawHover();
  } catch (e) {
    console.error('Failed to save virtual obstacles:', e);
  } finally {
    saving.value = false;
  }
};

/* ── Pointer handlers ───────────────────────────────────────────────────── */
const onPointerDown = (e: FederatedPointerEvent) => {
  const pos  = e.getLocalPosition(app!.stage);
  const cell = canvasToCell(pos.x, pos.y);

  if (canvasMode.value === 'obstacles') {
    if (!editing.value || !cell) return;
    const key = `${cell[0]},${cell[1]}`;
    if (physicalObstacles.has(key)) return;
    painting  = true;
    paintMode = pendingVirtual.has(key) ? 'remove' : 'add';
    applyPaint(key);
  } else {
    onWaypointClick(pos.x, pos.y);
  }
};

const onPointerMove = (e: FederatedPointerEvent) => {
  const pos  = e.getLocalPosition(app!.stage);
  const cell = canvasToCell(pos.x, pos.y);

  if (cell) {
    const cs = cellSize.value;
    hoverInfo.value = { cellX: cell[0], cellY: cell[1], mx: cell[0] * cs, my: cell[1] * cs };
  } else {
    hoverInfo.value = null;
  }

  if (canvasMode.value === 'obstacles') {
    const newKey = cell ? `${cell[0]},${cell[1]}` : null;
    if (newKey !== hoveredCell) { hoveredCell = newKey; drawHover(); }
    if (!painting || !cell || !editing.value) return;
    const key = `${cell[0]},${cell[1]}`;
    if (physicalObstacles.has(key)) return;
    applyPaint(key);
  }
};

const onPointerUp = () => { painting = false; };

const onPointerLeave = () => {
  hoverInfo.value = null;
  hoveredCell = null;
  if (canvasMode.value === 'obstacles') drawHover();
};

const applyPaint = (key: string) => {
  if (paintMode === 'add') pendingVirtual.add(key);
  else                     pendingVirtual.delete(key);
  pendingCount.value = pendingVirtual.size;
  hasChanges.value   = pendingVirtual.size !== snapshotVirtual.size ||
    [...pendingVirtual].some(k => !snapshotVirtual.has(k));
  drawObstacles();
};

/* ── Waypoint placement ─────────────────────────────────────────────────── */
const onWaypointClick = (canvasX: number, canvasY: number) => {
  const cell = canvasToCell(canvasX, canvasY);
  if (!cell) return;

  const key = `${cell[0]},${cell[1]}`;
  // Only allow free cells (no obstacles)
  if (physicalObstacles.has(key) || virtualObstacles.has(key)) return;

  const waypoints = canvasMode.value === 'formationPath' ? formationWaypoints.value : vehicleWaypoints.value;
  const cp        = cellPx.value;
  const pinR      = Math.max(6, cp * 0.4) + 3;

  for (let i = 0; i < waypoints.length; i++) {
    const pinCx = waypoints[i]!.cellY * cp + cp / 2;
    const pinCy = waypoints[i]!.cellX * cp + cp / 2;
    if (Math.sqrt((canvasX - pinCx) ** 2 + (canvasY - pinCy) ** 2) <= pinR) {
      waypoints.splice(i, 1);
      drawWaypoints();
      return;
    }
  }
  waypoints.push({ cellX: cell[0], cellY: cell[1], heading: 0, maxSpeed: 0.5 });
  drawWaypoints();
};

const clearWaypoints = () => {
  if (canvasMode.value === 'formationPath') formationWaypoints.value = [];
  else vehicleWaypoints.value = [];
  drawWaypoints();
};

/* ── Send formation path ────────────────────────────────────────────────── */
const sendFormationPath = async () => {
  if (formationWaypoints.value.length < 1) return;
  formationSending.value = true;
  try {
    const payload: FormationPathDto = {
      Waypoints: formationWaypoints.value.map(w => ({
        Position: { X: w.cellX, Y: w.cellY },
        Heading:  w.heading,
        MaxSpeed: w.maxSpeed,
      })),
      ModeChanged: false,
    };

    await connection.invoke('OnFormationShapeReceived', { Shape: formationShape.value, Size: formationSize.value });
    await connection.invoke('OnFormationPathReceived', payload);
    appStore.lastFormationPath = payload.Waypoints;
  } catch (err) {
    console.error('Failed to send formation path:', err);
  } finally {
    formationSending.value = false;
  }
};

/* ── Send vehicle targets ───────────────────────────────────────────────── */
const sendVehicleTargets = async () => {
  if (vehicleAgentId.value === null || vehicleWaypoints.value.length < 1) return;
  vehicleSending.value = true;
  try {
    const payload: VehicleTargetsDto = {
      Waypoints: vehicleWaypoints.value.map(w => ({
        Position: { X: w.cellX, Y: w.cellY },
        Heading:  w.heading,
        MaxSpeed: w.maxSpeed,
      })),
      AgentId:     vehicleAgentId.value,
      OverRide:    vehicleOverride.value,
      ModeChanged: false,
    };
    await connection.invoke('OnVehicleTargetsReceived', payload);
  } catch (err) {
    console.error('Failed to send vehicle targets:', err);
  } finally {
    vehicleSending.value = false;
  }
};

/* ── API fetch ──────────────────────────────────────────────────────────── */
const fetchInitialData = async () => {
  isLoading.value  = true;
  fetchError.value = false;
  try {
    const res = await fetch(apiUrl('/api/cache/obstacles/cells?occupiedOnly=true'));
    if (res.ok) {
      const cells: CellInfo[] = await res.json();
      physicalObstacles.clear();
      virtualObstacles.clear();
      for (const c of cells) {
        const key = `${c.x},${c.y}`;
        if (c.type === ObstacleType.Physical)     physicalObstacles.add(key);
        else if (c.type === ObstacleType.Virtual) virtualObstacles.add(key);
      }
      updateObstacleCounts();
      drawObstacles();
    }
  } catch {
    fetchError.value = true;
    console.error('Failed to fetch obstacle data');
  } finally {
    isLoading.value = false;
  }
};

const updateObstacleCounts = () => {
  physicalCount.value = physicalObstacles.size;
  virtualCount.value  = virtualObstacles.size;
  pendingCount.value  = editing.value ? pendingVirtual.size : virtualObstacles.size;
};

/* ── Lifecycle ──────────────────────────────────────────────────────────── */
onMounted(async () => {
  if (!containerRef.value) return;

  app = new Application();
  await app.init({
    background: C.bg,
    antialias: true,
    width:  containerRef.value.clientWidth,
    height: containerRef.value.clientHeight,
  });
  containerRef.value.appendChild(app.canvas as HTMLCanvasElement);

  gridGfx         = new Graphics();
  obstaclesGfx    = new Graphics();
  hoverGfx        = new Graphics();
  interactionArea = new Graphics();
  agentsLayer     = new Container();
  waypointLayer   = new Container();
  interactionArea.eventMode = 'none';

  app.stage.addChild(gridGfx);
  app.stage.addChild(obstaclesGfx);
  app.stage.addChild(hoverGfx);
  app.stage.addChild(agentsLayer);
  app.stage.addChild(waypointLayer);
  app.stage.addChild(interactionArea);

  app.stage.eventMode = 'static';
  app.stage.hitArea   = app.screen;
  app.stage.on('pointerdown',      onPointerDown);
  app.stage.on('pointermove',      onPointerMove);
  app.stage.on('pointerup',        onPointerUp);
  app.stage.on('pointerupoutside', onPointerUp);
  app.stage.on('pointerleave',     onPointerLeave);

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

  watch(canvasMode, (newMode, oldMode) => {
    if (oldMode === 'obstacles' && editing.value) discardEdits();
    hoveredCell     = null;
    hoverInfo.value = null;
    drawHover();
    drawWaypoints();
    updateInteractionArea();
  });

  subscribeToTopic(TOPIC);
  connection.on('VirtualObstaclesUpdated', handleVirtualObstacles);
  connection.on('MapUpdated',              handleMapUpdate);
  connection.on('AgentData',               handleAgentData);

  await fetchInitialData();
});

onUnmounted(() => {
  resizeObs?.disconnect();
  unsubscribeFromTopic(TOPIC);
  connection.off('VirtualObstaclesUpdated', handleVirtualObstacles);
  connection.off('MapUpdated',              handleMapUpdate);
  connection.off('AgentData',               handleAgentData);
  app?.destroy(true);
  app = null;
});

/* ── SignalR handlers ───────────────────────────────────────────────────── */
const handleMapUpdate = (dto: { cellX: number; cellY: number; occupied: boolean }) => {
  const key = `${dto.cellX},${dto.cellY}`;
  if (dto.occupied) physicalObstacles.add(key);
  else              physicalObstacles.delete(key);
  updateObstacleCounts();
  if (!editing.value) drawObstacles();
};

const handleVirtualObstacles = (dto: { obstacles: ObstacleCellDto[] }) => {
  if (editing.value) return;
  for (const obs of dto.obstacles) {
    const key = `${obs.x},${obs.y}`;
    if (obs.type === ObstacleType.Virtual) virtualObstacles.add(key);
    else virtualObstacles.delete(key);
  }
  updateObstacleCounts();
  drawObstacles();
};

const handleAgentData = (dto: AgentDataDto) => {
  agentDataMap.set(dto.agentId, dto);
  if (!agentIds.value.includes(dto.agentId)) {
    agentIds.value = [...agentIds.value, dto.agentId].sort((a, b) => a - b);
  }
  renderAgentDot(dto);
};

/* ── Interaction area ───────────────────────────────────────────────────── */
const updateInteractionArea = () => {
  if (!interactionArea) return;
  interactionArea.clear();
  const isActive = (canvasMode.value === 'obstacles' && editing.value)
    || canvasMode.value === 'formationPath'
    || canvasMode.value === 'vehicleTarget';
  if (isActive) {
    interactionArea.eventMode = 'static';
    interactionArea.cursor    = 'crosshair';
    const cp = cellPx.value;
    interactionArea.rect(0, 0, maxCol.value * cp, maxRow.value * cp);
    interactionArea.fill({ color: 0x000000, alpha: 0.001 });
  } else {
    interactionArea.eventMode = 'none';
    interactionArea.cursor    = 'default';
  }
};

/* ── Full redraw ────────────────────────────────────────────────────────── */
const redrawAll = () => {
  drawGrid();
  drawObstacles();
  drawHover();
  drawWaypoints();
  updateInteractionArea();
  for (const a of agentDataMap.values()) renderAgentDot(a);
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

  for (let c = 0; c <= cols; c++) { gridGfx.moveTo(c * cp, 0); gridGfx.lineTo(c * cp, totalH); }
  for (let r = 0; r <= rows; r++) { gridGfx.moveTo(0, r * cp); gridGfx.lineTo(totalW, r * cp); }
  gridGfx.stroke({ width: 0.5, color: C.gridLine, alpha: 0.5 });

  gridGfx.moveTo(0, 0).lineTo(totalW, 0);
  gridGfx.moveTo(0, 0).lineTo(0, totalH);
  gridGfx.stroke({ width: 2, color: C.axis, alpha: 0.6 });
};

/* ── Obstacle drawing ───────────────────────────────────────────────────── */
const drawObstacles = () => {
  if (!obstaclesGfx) return;
  obstaclesGfx.clear();

  const cp         = cellPx.value;
  const virtualSet = editing.value ? pendingVirtual : virtualObstacles;

  for (const key of physicalObstacles) {
    const [cx, cy] = key.split(',').map(Number) as [number, number];
    obstaclesGfx.rect(cy * cp, cx * cp, cp, cp);
  }
  if (physicalObstacles.size) obstaclesGfx.fill({ color: C.physical, alpha: 0.75 });

  for (const key of virtualSet) {
    const [cx, cy] = key.split(',').map(Number) as [number, number];
    obstaclesGfx.rect(cy * cp, cx * cp, cp, cp);
  }
  if (virtualSet.size) {
    obstaclesGfx.fill({ color: editing.value ? C.pending : C.virtual, alpha: editing.value ? 0.7 : 0.55 });
  }
};

/* ── Hover highlight ────────────────────────────────────────────────────── */
const drawHover = () => {
  if (!hoverGfx) return;
  hoverGfx.clear();
  if (canvasMode.value !== 'obstacles' || !editing.value || !hoveredCell) return;
  if (physicalObstacles.has(hoveredCell)) return;

  const [cx, cy] = hoveredCell.split(',').map(Number) as [number, number];
  const cp = cellPx.value;
  hoverGfx.rect(cy * cp, cx * cp, cp, cp);
  hoverGfx.fill({ color: C.cellHov, alpha: 0.4 });
  hoverGfx.rect(cy * cp, cx * cp, cp, cp);
  hoverGfx.stroke({ width: 1.5, color: 0xffffff, alpha: 0.3 });
};

/* ── Agent dot rendering ────────────────────────────────────────────────── */
const agentColor = (flags: string[]): number => {
  if (flags.some(f => f === 'StoppedSensorTimeout')) return C.agentError;
  if (flags.some(f => f.startsWith('Stopped')))      return C.agentWarn;
  if (flags.includes('WaypointActive'))               return C.agentActive;
  return C.agentDefault;
};

const renderAgentDot = (agent: AgentDataDto) => {
  if (!agentsLayer) return;

  let d = agentDotMap.get(agent.agentId);
  if (!d) {
    const root  = new Container();
    const body  = new Graphics();
    const label = new Text({
      text: `#${agent.agentId}`,
      style: { fontSize: 9, fill: 0xffffff, fontWeight: 'bold', fontFamily: 'monospace' },
    });
    label.anchor.set(0.5, 0.5);
    root.addChild(body, label);
    agentsLayer.addChild(root);
    d = { root, body, label };
    agentDotMap.set(agent.agentId, d);
  }

  const cp = cellPx.value;
  const r  = Math.max(4, cp * 0.35);

  d.root.position.set(toCx(agent.y), toCy(agent.x));

  d.body.clear();
  d.body.circle(0, 0, r);
  d.body.fill({ color: agentColor(agent.flags), alpha: 0.85 });
  d.body.circle(0, 0, r);
  d.body.stroke({ width: 1.5, color: 0x000000, alpha: 0.25 });

  d.label.visible = cp >= 14;
  if (cp >= 14) {
    d.label.text = `#${agent.agentId}`;
    d.label.position.set(0, -(r + 7));
  }
};

/* ── Waypoint drawing ───────────────────────────────────────────────────── */
const drawWaypoints = () => {
  if (!waypointLayer) return;
  waypointLayer.removeChildren();

  const waypoints = canvasMode.value === 'formationPath' ? formationWaypoints.value
    : canvasMode.value === 'vehicleTarget' ? vehicleWaypoints.value
    : [];
  if (waypoints.length === 0) return;

  const cp    = cellPx.value;
  const color = canvasMode.value === 'vehicleTarget' ? C.wpVeh : C.wpForm;
  const pinR  = Math.max(6, cp * 0.4);

  if (waypoints.length > 1) {
    const lineGfx = new Graphics();
    lineGfx.moveTo(waypoints[0]!.cellY * cp + cp / 2, waypoints[0]!.cellX * cp + cp / 2);
    for (let i = 1; i < waypoints.length; i++) {
      lineGfx.lineTo(waypoints[i]!.cellY * cp + cp / 2, waypoints[i]!.cellX * cp + cp / 2);
    }
    lineGfx.stroke({ width: 1.5, color, alpha: 0.45 });
    waypointLayer.addChild(lineGfx);
  }

  for (let i = 0; i < waypoints.length; i++) {
    const wx = waypoints[i]!.cellY * cp + cp / 2;
    const wy = waypoints[i]!.cellX * cp + cp / 2;

    const pin = new Graphics();
    pin.circle(wx, wy, pinR);
    pin.fill({ color, alpha: 0.85 });
    pin.circle(wx, wy, pinR);
    pin.stroke({ width: 1.5, color: 0xffffff, alpha: 0.3 });
    waypointLayer.addChild(pin);

    const lbl = new Text({
      text: `${i + 1}`,
      style: { fontSize: Math.max(8, Math.min(12, pinR)), fill: 0xffffff, fontWeight: 'bold', fontFamily: 'monospace' },
    });
    lbl.anchor.set(0.5, 0.5);
    lbl.position.set(wx, wy);
    waypointLayer.addChild(lbl);
  }
};

defineExpose({});
</script>

<template>
  <div class="interactive-map-root">
    <!-- Toolbar -->
    <div class="toolbar">
      <!-- Stats: only relevant in obstacles mode -->
      <div class="toolbar-left">
        <template v-if="canvasMode === 'obstacles'">
          <v-chip size="small" variant="outlined" class="mr-2 chip-dim">
            <span class="swatch physical-sw mr-1" /> {{ physicalCount }} Physical
          </v-chip>
          <v-chip size="small" variant="outlined" class="mr-2 chip-dim">
            <span class="swatch virtual-sw mr-1" /> {{ editing ? pendingCount : virtualCount }} Virtual
          </v-chip>
          <v-chip size="small" variant="outlined" class="chip-dim">
            {{ maxCol }} × {{ maxRow }} · {{ cellSize * 100 }}cm
          </v-chip>
        </template>
      </div>

      <!-- Mode tabs -->
      <div class="toolbar-center">
        <v-btn-toggle
          v-model="canvasMode"
          mandatory
          density="compact"
          divided
          variant="outlined"
          color="primary"
        >
          <v-btn value="formationPath" size="small" prepend-icon="mdi-map-marker-path">Formation Path</v-btn>
          <v-btn value="vehicleTarget" size="small" prepend-icon="mdi-car-arrow-right">Vehicle Target</v-btn>
          <v-btn value="obstacles" size="small" prepend-icon="mdi-wall">Obstacles</v-btn>
        </v-btn-toggle>
      </div>

      <!-- Obstacles mode actions -->
      <div class="toolbar-right">
        <template v-if="canvasMode === 'obstacles'">
          <template v-if="!editing">
            <v-btn variant="flat" color="primary" size="small" prepend-icon="mdi-pencil" @click="startEditing">
              Edit Obstacles
            </v-btn>
          </template>
          <template v-else>
            <v-btn variant="outlined" color="error" size="small" prepend-icon="mdi-eraser" class="mr-2" @click="resetField">
              Clear All
            </v-btn>
            <v-btn variant="text" size="small" prepend-icon="mdi-close" class="mr-2" @click="discardEdits">
              Discard
            </v-btn>
            <v-btn variant="flat" color="success" size="small" prepend-icon="mdi-content-save" :loading="saving" :disabled="!hasChanges" @click="saveEdits">
              Save
            </v-btn>
          </template>
        </template>
      </div>
    </div>

    <!-- Canvas area + side panel -->
    <div class="canvas-area">
      <div ref="containerRef" class="map-canvas">
        <!-- Edit mode banners -->
        <div v-if="canvasMode === 'obstacles' && editing" class="overlay edit-banner obstacle-banner">
          <v-icon icon="mdi-pencil" size="14" class="mr-1" />
          Click or drag to toggle virtual obstacles
        </div>
        <div v-if="canvasMode === 'formationPath'" class="overlay edit-banner formation-banner">
          <v-icon icon="mdi-map-marker-path" size="14" class="mr-1" />
          Click free cells to place waypoints · click a pin to remove
        </div>
        <div v-if="canvasMode === 'vehicleTarget'" class="overlay edit-banner vehicle-banner">
          <v-icon icon="mdi-car-arrow-right" size="14" class="mr-1" />
          Click free cells to place waypoints · click a pin to remove
        </div>

        <!-- Hover coordinate overlay -->
        <div v-if="hoverInfo" class="overlay hover-info">
          <span class="hi-cell">({{ hoverInfo.cellX }}, {{ hoverInfo.cellY }})</span>
          <span class="hi-sep"> · </span>
          <span class="hi-meters">{{ hoverInfo.mx.toFixed(2) }}m, {{ hoverInfo.my.toFixed(2) }}m</span>
        </div>

        <!-- Legend -->
        <div class="overlay legend-panel">
          <div class="legend-row"><span class="swatch physical-sw" /> Physical obstacle (locked)</div>
          <div class="legend-row"><span class="swatch virtual-sw" /> Virtual obstacle</div>
          <div class="legend-row"><span class="swatch agent-sw" />   Agent position</div>
          <div class="legend-row"><span class="swatch axis-sw" />    Origin axes</div>
        </div>

        <!-- Loading -->
        <div v-if="isLoading" class="overlay center-overlay">
          <v-progress-circular indeterminate size="28" width="3" color="primary" />
        </div>
        <div v-else-if="fetchError" class="overlay center-overlay">
          <v-chip size="small" color="error" variant="flat" prepend-icon="mdi-alert-circle">
            Failed to load obstacle data
          </v-chip>
        </div>
      </div>

      <!-- Formation Path side panel -->
      <div v-if="canvasMode === 'formationPath'" class="side-panel">
        <div class="sp-header">Formation Path</div>

        <div class="sp-section">
          <div class="sp-label">Shape</div>
          <v-select v-model="formationShape" :items="shapeOptions" item-title="title" item-value="value"
            density="compact" hide-details variant="outlined" />
        </div>

        <div class="sp-section">
          <div class="sp-label">Size</div>
          <v-text-field v-model.number="formationSize" type="number" density="compact" hide-details variant="outlined" min="1" />
        </div>

        <div class="sp-section sp-waypoints">
          <div class="sp-label-row">
            <span class="sp-label">Waypoints ({{ formationWaypoints.length }})</span>
            <button class="sp-clear-btn" @click="clearWaypoints">Clear</button>
          </div>
          <div class="sp-wp-header">
            <span class="sp-wp-idx"></span>
            <span class="sp-wp-th" style="flex:1">X</span>
            <span class="sp-wp-th" style="flex:1">Y</span>
            <span class="sp-wp-th" style="flex:1.3">Hdg</span>
            <span class="sp-wp-th" style="flex:1.3">Spd</span>
            <span style="width:24px"></span>
          </div>
          <div v-if="formationWaypoints.length === 0" class="sp-empty">Click free cells on the map</div>
          <div v-for="(wp, i) in formationWaypoints" :key="i" class="sp-wp-row">
            <span class="sp-wp-idx">#{{ i + 1 }}</span>
            <input class="sp-wp-input" type="number" v-model.number="wp.cellX" step="1" title="Cell X" @change="drawWaypoints()" />
            <input class="sp-wp-input" type="number" v-model.number="wp.cellY" step="1" title="Cell Y" @change="drawWaypoints()" />
            <input class="sp-wp-input sp-wp-wide" type="number" v-model.number="wp.heading" step="0.1" title="Heading (rad)" />
            <input class="sp-wp-input sp-wp-wide" type="number" v-model.number="wp.maxSpeed" step="0.1" min="0" title="Max Speed (m/s)" />
            <button class="sp-wp-del" @click="formationWaypoints.splice(i, 1); drawWaypoints()">✕</button>
          </div>
        </div>

        <div class="sp-footer">
          <v-btn variant="flat" color="success" size="small" prepend-icon="mdi-send"
            :loading="formationSending" :disabled="formationWaypoints.length < 1"
            @click="sendFormationPath" block>
            Send Formation Path
          </v-btn>
        </div>
      </div>

      <!-- Vehicle Target side panel -->
      <div v-if="canvasMode === 'vehicleTarget'" class="side-panel">
        <div class="sp-header">Vehicle Target</div>

        <div class="sp-section">
          <div class="sp-label">Agent</div>
          <v-select v-model="vehicleAgentId" :items="agentIds" density="compact" hide-details
            variant="outlined" placeholder="Select agent" :prefix="vehicleAgentId !== null ? '#' : ''" />
        </div>

        <div class="sp-section">
          <label class="sp-check">
            <input type="checkbox" v-model="vehicleOverride" /> Override
          </label>
        </div>

        <div class="sp-section sp-waypoints">
          <div class="sp-label-row">
            <span class="sp-label">Waypoints ({{ vehicleWaypoints.length }})</span>
            <button class="sp-clear-btn" @click="clearWaypoints">Clear</button>
          </div>
          <div class="sp-wp-header">
            <span class="sp-wp-idx"></span>
            <span class="sp-wp-th" style="flex:1">X</span>
            <span class="sp-wp-th" style="flex:1">Y</span>
            <span class="sp-wp-th" style="flex:1.3">Hdg</span>
            <span class="sp-wp-th" style="flex:1.3">Spd</span>
            <span style="width:24px"></span>
          </div>
          <div v-if="vehicleWaypoints.length === 0" class="sp-empty">Click free cells on the map</div>
          <div v-for="(wp, i) in vehicleWaypoints" :key="i" class="sp-wp-row">
            <span class="sp-wp-idx">#{{ i + 1 }}</span>
            <input class="sp-wp-input" type="number" v-model.number="wp.cellX" step="1" title="Cell X" @change="drawWaypoints()" />
            <input class="sp-wp-input" type="number" v-model.number="wp.cellY" step="1" title="Cell Y" @change="drawWaypoints()" />
            <input class="sp-wp-input sp-wp-wide" type="number" v-model.number="wp.heading" step="0.1" title="Heading (rad)" />
            <input class="sp-wp-input sp-wp-wide" type="number" v-model.number="wp.maxSpeed" step="0.1" min="0" title="Max Speed (m/s)" />
            <button class="sp-wp-del" @click="vehicleWaypoints.splice(i, 1); drawWaypoints()">✕</button>
          </div>
        </div>

        <div class="sp-footer">
          <v-btn variant="flat" color="success" size="small" prepend-icon="mdi-send"
            :loading="vehicleSending" :disabled="vehicleAgentId === null || vehicleWaypoints.length < 1"
            @click="sendVehicleTargets" block>
            Send Targets
          </v-btn>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.interactive-map-root {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  flex: 1 1 0;
  width: 100%;
}

.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  background: #161b22;
  border-bottom: 1px solid rgba(48, 54, 61, 0.6);
  flex-shrink: 0;
  gap: 8px;
  min-height: 48px;
}
.toolbar-left,
.toolbar-right  { display: flex; align-items: center; min-width: 0; }
.toolbar-center { display: flex; align-items: center; justify-content: center; flex: 1; }

.chip-dim { color: rgba(255,255,255,0.7) !important; border-color: rgba(48,54,61,0.8) !important; }

/* ── Canvas area ─────────────────────────────────────────────────── */
.canvas-area {
  display: flex;
  flex: 1;
  overflow: hidden;
  min-height: 0;
}

.map-canvas {
  flex: 1;
  position: relative;
  overflow: hidden;
  background-color: #0d1117;
  min-width: 0;
}
.map-canvas :deep(canvas) {
  display: block;
  width: 100%;
  height: 100%;
}

/* ── Side panel ──────────────────────────────────────────────────── */
.side-panel {
  width: 340px;
  flex-shrink: 0;
  background: #0d1117;
  border-left: 1px solid rgba(48, 54, 61, 0.6);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}
.sp-header {
  padding: 11px 16px 9px;
  font-size: 12px;
  font-family: 'Roboto Mono', monospace;
  color: rgba(255,255,255,0.35);
  text-transform: uppercase;
  letter-spacing: 0.08em;
  border-bottom: 1px solid rgba(48,54,61,0.4);
  flex-shrink: 0;
}
.sp-section {
  padding: 10px 14px;
  border-bottom: 1px solid rgba(48,54,61,0.3);
}
.sp-label {
  font-size: 12px;
  color: rgba(255,255,255,0.45);
  font-family: 'Roboto Mono', monospace;
  margin-bottom: 6px;
}
.sp-check {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  font-family: 'Roboto Mono', monospace;
  color: rgba(255,255,255,0.75);
  cursor: pointer;
  user-select: none;
}
.sp-check input[type="checkbox"] { accent-color: #58a6ff; width: 14px; height: 14px; cursor: pointer; }

.sp-waypoints { padding-bottom: 6px; flex: 1; }
.sp-label-row { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; }
.sp-clear-btn {
  font-size: 11px; color: rgba(248,113,113,0.7); background: none; border: none;
  cursor: pointer; font-family: 'Roboto Mono', monospace; padding: 2px 6px; border-radius: 4px;
}
.sp-clear-btn:hover { color: #f87171; background: rgba(248,113,113,0.1); }
.sp-empty {
  font-size: 12px; color: rgba(255,255,255,0.25); font-family: 'Roboto Mono', monospace;
  padding: 8px 0; text-align: center;
}

/* Waypoint table header */
.sp-wp-header {
  display: flex; align-items: center; gap: 4px;
  margin-bottom: 4px; padding-bottom: 4px;
  border-bottom: 1px solid rgba(48,54,61,0.4);
}
.sp-wp-th {
  font-size: 10px; font-family: 'Roboto Mono', monospace;
  color: rgba(255,255,255,0.3); text-transform: uppercase; letter-spacing: 0.06em;
}

.sp-wp-row { display: flex; align-items: center; gap: 4px; margin-bottom: 5px; }
.sp-wp-idx { font-size: 11px; font-family: 'Roboto Mono', monospace; color: rgba(255,255,255,0.4); min-width: 24px; text-align: right; flex-shrink: 0; }
.sp-wp-input {
  flex: 1; min-width: 0;
  background: rgba(22,27,34,0.9); border: 1px solid rgba(48,54,61,0.6);
  border-radius: 5px; color: rgba(255,255,255,0.85); font-size: 12px;
  font-family: 'Roboto Mono', monospace; padding: 4px 6px; text-align: right;
}
.sp-wp-input:focus { outline: none; border-color: rgba(88,166,255,0.55); background: rgba(22,27,34,1); }
.sp-wp-wide { flex: 1.3; }
.sp-wp-del {
  background: none; border: none; color: rgba(248,113,113,0.5); cursor: pointer;
  font-size: 13px; padding: 2px 4px; border-radius: 4px; line-height: 1; flex-shrink: 0;
}
.sp-wp-del:hover { color: #f87171; background: rgba(248,113,113,0.1); }
.sp-footer {
  padding: 12px 14px; flex-shrink: 0;
  border-top: 1px solid rgba(48,54,61,0.4); margin-top: auto;
}

/* ── Shared overlay base ─────────────────────────────────────────── */
.overlay {
  position: absolute;
  pointer-events: none;
  z-index: 10;
  background: rgba(13,17,23,0.82);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(48,54,61,0.65);
  border-radius: 10px;
  padding: 8px 13px;
  color: rgba(255,255,255,0.85);
  font-size: 13px;
  font-family: 'Roboto Mono', monospace;
}

.edit-banner {
  top: 12px; left: 50%; transform: translateX(-50%);
  display: flex; align-items: center;
}
.obstacle-banner  { color: #c4b5fd; border-color: rgba(167,139,250,0.3); }
.formation-banner { color: #c4b5fd; border-color: rgba(167,139,250,0.3); }
.vehicle-banner   { color: #34d399; border-color: rgba(52,211,153,0.3); }

/* Hover info: top-right to stay visible regardless of canvas height */
.hover-info { top: 12px; right: 12px; display: flex; align-items: center; gap: 3px; }
.hi-cell    { font-size: 12px; color: rgba(255,255,255,0.9); }
.hi-sep     { color: rgba(255,255,255,0.3); }
.hi-meters  { font-size: 11px; color: rgba(255,255,255,0.55); }

/* Legend: top-left, below edit banner */
.legend-panel { top: 12px; left: 12px; display: flex; flex-direction: column; gap: 4px; }
.center-overlay {
  top: 50%; left: 50%;
  transform: translate(-50%, -50%);
  background: rgba(13,17,23,0.9);
}

/* ── Swatches ────────────────────────────────────────────────────── */
.swatch { display: inline-block; width: 11px; height: 11px; border-radius: 2px; flex-shrink: 0; }
.physical-sw { background-color: #ff6b6b; }
.virtual-sw  { background-color: #a78bfa; }
.agent-sw    { background-color: #34d399; border-radius: 50%; }
.axis-sw     { background-color: #58a6ff; border-radius: 1px; height: 3px; }

.legend-row {
  display: flex; align-items: center; gap: 7px;
  font-size: 12px; color: rgba(255,255,255,0.6);
}
</style>
