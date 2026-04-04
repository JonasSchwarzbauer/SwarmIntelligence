/**
 * TypeScript models for SwarmIntelligence event DTOs
 * Auto-generated from C# DTOs
 * One-to-one mapping with backend event types
 */

// ============================================
// Base types and shared models
// ============================================

export interface WaypointDto {
  x: number;
  y: number;
  heading: number;
  maxSpeed: number;
}

export interface DriveCommandDto {
  agentId: number;
  driveFlags: string[];
  waypoints: WaypointDto[];
  timestamp: string; // ISO 8601 DateTime
}

// ============================================
// DriveControl Events
// ============================================

export interface CommandGeneratedDto {
  command: DriveCommandDto;
  generatedAt: string;
  agentId: number;
}

export interface CommandAssignedDto {
  command: DriveCommandDto;
  agentId: number;
  assignedAt: string;
}

export interface CommandDispatchedDto {
  command: DriveCommandDto;
  agentId: number;
  dispatchLatencyMs: number;
  dispatchedAt: string;
}

export interface CommandClearedDto {
  agentId: number;
  timestamp: string;
}

export interface AgentRegistrationDto {
  agentId: number;
  timestamp: string;
  registrationType: string;
}

export interface DriveControlErrorDto {
  agentId: number;
  message: string;
  source: string;
  sourceContext: string;
  exceptionMessage: string;
  exception: string; // serialized exception
  timestamp: string;
}

export interface ManagerStateDto {
  isRunning: boolean;
  timestamp: string;
}

// ============================================
// MapControl Events
// ============================================

export interface AgentDataDto {
  agentId: number;
  x: number;
  y: number;
  orientation: number;
  velocity: number;
  frontalDistance: number;
  target: WaypointDto;
  flags: string[];
  dwmSuccessRate: number;
  dataReceived: string;
  timestamp: string;
}

export interface MapUpdatedDto {
  x: number;
  y: number;
  cellX: number;
  cellY: number;
  occupied: boolean;
  timestamp: string;
}

// ============================================
// Obstacle / Grid types
// ============================================

export enum ObstacleType {
  Free = 0,
  Physical = 1,
  Virtual = 2,
}

export interface CellInfo {
  x: number;
  y: number;
  type: ObstacleType;
}

export interface ObstacleCellDto {
  x: number;
  y: number;
  type: ObstacleType;
}

export interface VirtualObstaclesDto {
  obstacles: ObstacleCellDto[];
}

export interface BufferInformationDto {
  success: boolean;
  bufferCount: number;
  bufferCapacity: number;
  bufferUsagePercentage: number;
  timestamp: string;
}

export interface UsbStartedDto {
  success: boolean;
  portName: string;
  baudRate: number;
  message: string;
  timestamp: string;
}

export interface MapControlErrorDto {
  message: string;
  source: string;
  sourceContext: string;
  exceptionMessage: string;
  exception: string; // serialized exception
  timestamp: string;
}

export interface WorkerStateDto {
  state: string;
  timestamp: string;
}

// ============================================
// API response models (initial fetch)
// ============================================

export interface ManagerStateApi {
  isRunning: boolean;
  lastUpdated: string;
}

export interface MapWorkerStateApi {
  currentState: string;
  lastUpdated: string;
}

export const EVENT_TYPES = {
  COMMAND_GENERATED: 'CommandGenerated',
  COMMAND_ASSIGNED: 'CommandAssigned',
  COMMAND_DISPATCHED: 'CommandDispatched',
  COMMAND_CLEARED: 'CommandCleared',
  AGENT_REGISTRATION: 'AgentRegistration',
  DRIVE_CONTROL_ERROR: 'DriveControlError',
  AGENT_DATA: 'AgentData',
  MAP_UPDATED: 'MapUpdated',
  BUFFER_INFORMATION: 'BufferInformation',
  USB_STARTED: 'UsbStarted',
  MAP_CONTROL_ERROR: 'MapControlError',
  MANAGER_STATE: 'ManagerState',
  WORKER_STATE: 'WorkerState',
} as const;

export type EventType = typeof EVENT_TYPES[keyof typeof EVENT_TYPES];

export type EventPayloadByType = {
  [EVENT_TYPES.COMMAND_GENERATED]: CommandGeneratedDto;
  [EVENT_TYPES.COMMAND_ASSIGNED]: CommandAssignedDto;
  [EVENT_TYPES.COMMAND_DISPATCHED]: CommandDispatchedDto;
  [EVENT_TYPES.COMMAND_CLEARED]: CommandClearedDto;
  [EVENT_TYPES.AGENT_REGISTRATION]: AgentRegistrationDto;
  [EVENT_TYPES.DRIVE_CONTROL_ERROR]: DriveControlErrorDto;
  [EVENT_TYPES.AGENT_DATA]: AgentDataDto;
  [EVENT_TYPES.MAP_UPDATED]: MapUpdatedDto;
  [EVENT_TYPES.BUFFER_INFORMATION]: BufferInformationDto;
  [EVENT_TYPES.USB_STARTED]: UsbStartedDto;
  [EVENT_TYPES.MAP_CONTROL_ERROR]: MapControlErrorDto;
  [EVENT_TYPES.MANAGER_STATE]: ManagerStateDto;
  [EVENT_TYPES.WORKER_STATE]: WorkerStateDto;
};

export type EventPayload = EventPayloadByType[EventType];

export interface EventLogEntry<TType extends EventType = EventType> {
  id: string; // UUID generated on client
  type: TType; // EventDtoTypeName for routing
  timestamp: string; // ISO 8601
  payload: EventPayloadByType[TType];
}