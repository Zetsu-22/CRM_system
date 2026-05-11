const BASE = '/api/v1'
const TOKEN_KEY = 'crm_token'
const REFRESH_KEY = 'crm_refresh'
const PROFILE_KEY = 'crm_profile'

function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_KEY)
}

// In-flight refresh promise to deduplicate parallel 401-triggered refreshes.
let refreshInFlight: Promise<string | null> | null = null

async function tryRefreshToken(): Promise<string | null> {
  if (refreshInFlight) return refreshInFlight
  const refreshToken = getRefreshToken()
  if (!refreshToken) return null

  refreshInFlight = (async () => {
    try {
      const res = await fetch(`${BASE}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!res.ok) return null
      const data = await res.json() as { token?: string; refreshToken?: string; profile?: unknown }
      if (data.token) localStorage.setItem(TOKEN_KEY, data.token)
      if (data.refreshToken) localStorage.setItem(REFRESH_KEY, data.refreshToken)
      if (data.profile) localStorage.setItem(PROFILE_KEY, JSON.stringify(data.profile))
      return data.token ?? null
    } catch {
      return null
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

function clearAuthAndRedirect(): never {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_KEY)
  localStorage.removeItem(PROFILE_KEY)
  window.location.href = '/login'
  throw new Error('Сессия истекла. Войдите снова.')
}

async function doFetch(path: string, method: string, body: unknown, token: string | null): Promise<Response> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers['Authorization'] = `Bearer ${token}`

  return fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  let res = await doFetch(path, method, body, getToken())

  if (res.status === 401) {
    const newToken = await tryRefreshToken()
    if (newToken) {
      res = await doFetch(path, method, body, newToken)
      if (res.status === 401) clearAuthAndRedirect()
    } else {
      clearAuthAndRedirect()
    }
  }

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    let message = text || `HTTP ${res.status}`
    try {
      const json = JSON.parse(text)
      message = json?.error ?? json?.title ?? json?.detail ?? json?.message ?? message
    } catch { /* use raw text */ }
    throw new Error(message)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body: unknown) => request<T>('PUT', path, body),
  patch: <T>(path: string, body: unknown) => request<T>('PATCH', path, body),
  delete: (path: string) => request<void>('DELETE', path),
}
