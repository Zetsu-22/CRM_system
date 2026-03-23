const BASE = '/api/v1'
const TOKEN_KEY = 'crm_token'

function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const token = getToken()
  const headers: Record<string, string> = {}
  if (body) headers['Content-Type'] = 'application/json'
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (res.status === 401) {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem('crm_profile')
    window.location.href = '/login'
    throw new Error('Сессия истекла. Войдите снова.')
  }

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    let message = text || `HTTP ${res.status}`
    try {
      const json = JSON.parse(text)
      message = json?.error ?? json?.message ?? message
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
