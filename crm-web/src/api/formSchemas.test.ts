import { describe, it, expect, vi } from 'vitest'
import {
  CounterpartyFormSchema,
  ItemFormSchema,
  OrderFormSchema,
  OrganizationFormSchema,
  validateForm,
  getZodErrors,
} from './formSchemas'

// ── CounterpartyFormSchema ─────────────────────────────────────────────────

describe('CounterpartyFormSchema', () => {
  const valid = { name: 'ООО Тест', type: 'Buyer', status: 'Active' }

  it('accepts valid counterparty', () => {
    expect(CounterpartyFormSchema.safeParse(valid).success).toBe(true)
  })

  it('rejects empty name', () => {
    const r = CounterpartyFormSchema.safeParse({ ...valid, name: '' })
    expect(r.success).toBe(false)
    if (!r.success) expect(r.error.issues[0].message).toBe('Название обязательно')
  })

  it('rejects invalid type', () => {
    const r = CounterpartyFormSchema.safeParse({ ...valid, type: 'Unknown' })
    expect(r.success).toBe(false)
  })

  it('accepts optional fields as empty strings', () => {
    const r = CounterpartyFormSchema.safeParse({ ...valid, inn: '', kpp: '' })
    expect(r.success).toBe(true)
  })
})

// ── ItemFormSchema ─────────────────────────────────────────────────────────

describe('ItemFormSchema', () => {
  const valid = { name: 'Товар 1', type: 'Product', unit: 'шт', vatRate: 'Vat22' }

  it('accepts valid item', () => {
    expect(ItemFormSchema.safeParse(valid).success).toBe(true)
  })

  it('rejects empty name', () => {
    const r = ItemFormSchema.safeParse({ ...valid, name: '' })
    expect(r.success).toBe(false)
  })

  it('rejects empty unit', () => {
    const r = ItemFormSchema.safeParse({ ...valid, unit: '' })
    expect(r.success).toBe(false)
    if (!r.success) expect(r.error.issues[0].message).toBe('Единица измерения обязательна')
  })

  it('rejects invalid vatRate', () => {
    const r = ItemFormSchema.safeParse({ ...valid, vatRate: 'Vat20' })
    expect(r.success).toBe(false)
  })

  it('coerces price from string', () => {
    const r = ItemFormSchema.safeParse({ ...valid, price: '1000' })
    expect(r.success).toBe(true)
    if (r.success) expect(r.data.price).toBe(1000)
  })
})

// ── OrderFormSchema ────────────────────────────────────────────────────────

describe('OrderFormSchema', () => {
  const validLine = {
    itemId: 'item-1', nameSnapshot: 'Товар 1', vatRateSnapshot: 'Vat22',
    qty: 1, unitPrice: 100, discountType: 'None', discountValue: 0,
  }
  const valid = {
    date: '2026-06-02', organizationId: 'org-1', counterpartyId: 'cp-1',
    lines: [validLine],
  }

  it('accepts valid order', () => {
    expect(OrderFormSchema.safeParse(valid).success).toBe(true)
  })

  it('rejects missing organizationId', () => {
    const r = OrderFormSchema.safeParse({ ...valid, organizationId: '' })
    expect(r.success).toBe(false)
    if (!r.success) expect(r.error.issues[0].message).toBe('Выберите организацию')
  })

  it('rejects empty lines array', () => {
    const r = OrderFormSchema.safeParse({ ...valid, lines: [] })
    expect(r.success).toBe(false)
    if (!r.success) expect(r.error.issues[0].message).toBe('Добавьте хотя бы одну позицию')
  })

  it('rejects line with empty itemId', () => {
    const r = OrderFormSchema.safeParse({ ...valid, lines: [{ ...validLine, itemId: '' }] })
    expect(r.success).toBe(false)
  })

  it('rejects line with zero qty', () => {
    const r = OrderFormSchema.safeParse({ ...valid, lines: [{ ...validLine, qty: 0 }] })
    expect(r.success).toBe(false)
  })
})

// ── OrganizationFormSchema ─────────────────────────────────────────────────

describe('OrganizationFormSchema', () => {
  it('accepts valid organization', () => {
    const r = OrganizationFormSchema.safeParse({ name: 'Основная', address: 'Москва' })
    expect(r.success).toBe(true)
  })

  it('rejects empty name', () => {
    const r = OrganizationFormSchema.safeParse({ name: '' })
    expect(r.success).toBe(false)
  })
})

// ── validateForm helper ────────────────────────────────────────────────────

describe('validateForm', () => {
  it('returns true for valid data', () => {
    const onError = vi.fn()
    const valid = { name: 'ООО Тест', type: 'Buyer' as const, status: 'Active' as const }
    const result = validateForm(CounterpartyFormSchema, valid, onError)
    expect(result).toBe(true)
    expect(onError).not.toHaveBeenCalled()
  })

  it('calls onError with first error message for invalid data', () => {
    const onError = vi.fn()
    const result = validateForm(CounterpartyFormSchema, { name: '' }, onError)
    expect(result).toBe(false)
    expect(onError).toHaveBeenCalledWith(expect.stringContaining('Название обязательно'))
  })

  it('includes field path in error message', () => {
    const onError = vi.fn()
    validateForm(OrderFormSchema, {
      date: '2026-06-02', organizationId: 'org-1', counterpartyId: 'cp-1',
      lines: [{ itemId: '', nameSnapshot: '', vatRateSnapshot: 'Vat0', qty: 1, unitPrice: 0, discountType: 'None', discountValue: 0 }]
    }, onError)
    expect(onError).toHaveBeenCalledWith(expect.stringContaining('itemId'))
  })
})

// ── getZodErrors helper ────────────────────────────────────────────────────

describe('getZodErrors', () => {
  it('returns flat error map from ZodError', () => {
    const result = OrderFormSchema.safeParse({ organizationId: '', counterpartyId: '' })
    if (!result.success) {
      const errors = getZodErrors(result.error)
      expect(errors).toHaveProperty('organizationId')
      expect(errors['organizationId']).toBeTruthy()
    }
  })
})
