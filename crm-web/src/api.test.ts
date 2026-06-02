/**
 * Тесты для error-extraction логики в api.ts.
 * Проверяем, что сообщение правильно извлекается из разных форматов 4xx-ответов.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

// Хелпер — эмулирует логику из api.ts request()
function extractErrorMessage(jsonText: string, statusText: string): string {
  let message = jsonText || `HTTP ${statusText}`
  try {
    const json = JSON.parse(jsonText)
    const errField = json?.error
    const errMsg = errField == null ? undefined
      : typeof errField === 'string' ? errField
      : errField?.message ?? JSON.stringify(errField)
    message = errMsg ?? json?.title ?? json?.detail ?? json?.message ?? message
  } catch { /* use raw text */ }
  return message
}

describe('API error extraction', () => {
  it('extracts message from nested {error: {code, message}}', () => {
    const body = JSON.stringify({
      error: { code: 'no_issued_invoice', message: 'Невозможно подтвердить оплату' }
    })
    expect(extractErrorMessage(body, '409')).toBe('Невозможно подтвердить оплату')
  })

  it('extracts message from {error: "string"}', () => {
    const body = JSON.stringify({ error: 'Запись не найдена' })
    expect(extractErrorMessage(body, '404')).toBe('Запись не найдена')
  })

  it('falls back to title from ProblemDetails', () => {
    const body = JSON.stringify({
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.2',
      title: 'Unauthorized',
      status: 401,
    })
    expect(extractErrorMessage(body, '401')).toBe('Unauthorized')
  })

  it('falls back to top-level message field', () => {
    const body = JSON.stringify({ message: 'Internal error' })
    expect(extractErrorMessage(body, '500')).toBe('Internal error')
  })

  it('returns raw text when JSON parse fails', () => {
    const body = 'Not JSON at all'
    expect(extractErrorMessage(body, '503')).toBe('Not JSON at all')
  })

  it('handles empty error body gracefully', () => {
    const result = extractErrorMessage('', '500')
    expect(result).toBe('HTTP 500')
  })

  it('handles {error: null} without crashing', () => {
    const body = JSON.stringify({ error: null, title: 'Fallback' })
    expect(extractErrorMessage(body, '400')).toBe('Fallback')
  })
})

// ── fetch mock для реального request() ────────────────────────────────────
// Мокаем localStorage и fetch чтобы проверить полный цикл
describe('API fetch mock integration', () => {
  const originalFetch = global.fetch
  const originalLocalStorage = global.localStorage

  beforeEach(() => {
    // Мок localStorage
    const store: Record<string, string> = { crm_token: 'test-token' }
    vi.stubGlobal('localStorage', {
      getItem: (k: string) => store[k] ?? null,
      setItem: (k: string, v: string) => { store[k] = v },
      removeItem: (k: string) => { delete store[k] },
    })
  })

  afterEach(() => {
    global.fetch = originalFetch
    global.localStorage = originalLocalStorage
    vi.unstubAllGlobals()
  })

  it('throws with correct message on 409 guard error', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 409,
      statusText: 'Conflict',
      text: async () => JSON.stringify({
        error: { code: 'no_issued_invoice', message: 'Невозможно подтвердить оплату: нет выставленного счёта.' }
      }),
    } as Response)

    // Динамически импортируем api чтобы получить свежий модуль с замоканным fetch
    const { api } = await import('./api')
    await expect(api.post('/orders/123/next-status', {}))
      .rejects.toThrow('Невозможно подтвердить оплату: нет выставленного счёта.')
  })
})
