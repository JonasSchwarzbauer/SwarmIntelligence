/**
 * Central API configuration.
 *
 * In development VITE_API_BASE points to the local backend.
 * In production the build is served from the C# backend's wwwroot,
 * so the base is empty (same-origin relative paths).
 */
const API_BASE = (import.meta.env.VITE_API_BASE as string | undefined)?.replace(/\/+$/, '') ?? '';

/** Full URL for the SignalR hub */
export const SIGNALR_HUB_URL = `${API_BASE}/uihub`;

/** Build an API URL, e.g. apiUrl('/api/cache/agents') */
export const apiUrl = (path: string) => `${API_BASE}${path}`;
