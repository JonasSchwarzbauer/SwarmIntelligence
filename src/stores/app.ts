// Utilities
import { defineStore } from 'pinia'
import type { WaypointGrid } from '@/models/user-inputs'

export const useAppStore = defineStore('app', {
  state: () => ({
    /** Real-world X extent in metres (horizontal) */
    xMax: 0 as number,
    /** Real-world Y extent in metres (vertical) */
    yMax: 0 as number,
    /** Grid cell size in metres */
    gridSize: 0.25 as number,
    /** Last sent formation path waypoints — shared with AwarenessMapCanvas for display */
    lastFormationPath: null as WaypointGrid[] | null,
  }),
})
