import { describe, it, expect } from 'vitest'
import { QK } from './queryKeys'

describe('QK (query keys)', () => {
  it('orders key without filter', () => {
    expect(QK.orders()).toEqual(['orders', 'all'])
  })

  it('orders key with filter', () => {
    expect(QK.orders('org-123')).toEqual(['orders', 'org-123'])
  })

  it('order key is scoped to id', () => {
    const key = QK.order('abc-123')
    expect(key).toEqual(['orders', 'abc-123'])
  })

  it('orderHistory key includes history suffix', () => {
    const key = QK.orderHistory('abc-123')
    expect(key).toEqual(['orders', 'abc-123', 'history'])
  })

  it('different entity types have different root keys', () => {
    expect(QK.orders()[0]).not.toBe(QK.counterparties()[0])
    expect(QK.items()[0]).not.toBe(QK.invoices()[0])
  })
})
