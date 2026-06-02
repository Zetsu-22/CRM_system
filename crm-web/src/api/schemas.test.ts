import { describe, it, expect } from 'vitest'
import {
  OrderSchema,
  CounterpartySchema,
  ItemSchema,
  InvoiceSchema,
  OrderStatusHistoryEntrySchema,
  PickingChecklistItemSchema,
} from './schemas'

// ── OrderSchema ────────────────────────────────────────────────────────────

describe('OrderSchema', () => {
  const minimalOrder = {
    id: '00000000-0000-0000-0000-000000000001',
    number: 'TST-001',
    date: '2026-05-31',
    status: 'Processing',
    totalNet: 1000,
    totalVat: 200,
    totalGross: 1200,
    isAssemblyChecklistClosed: false,
  }

  it('parses a minimal valid order', () => {
    const result = OrderSchema.safeParse(minimalOrder)
    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.lines).toEqual([])   // default([])
    }
  })

  it('parses order with lines', () => {
    const order = {
      ...minimalOrder,
      lines: [{
        id: '00000000-0000-0000-0000-000000000002',
        itemId: '00000000-0000-0000-0000-000000000003',
        nameSnapshot: 'Товар 1',
        vatRateSnapshot: 'Vat22',
        qty: 2,
        unitPrice: 500,
        discountType: 'None',
        discountValue: 0,
        net: 1000,
        vat: 220,
        gross: 1220,
      }],
    }
    const result = OrderSchema.safeParse(order)
    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.lines).toHaveLength(1)
      expect(result.data.lines[0].nameSnapshot).toBe('Товар 1')
    }
  })

  it('rejects order with missing required fields', () => {
    const result = OrderSchema.safeParse({ id: 'not-a-uuid' })
    expect(result.success).toBe(false)
  })

  it('allows nullable number and snapshots', () => {
    const order = {
      ...minimalOrder,
      number: null,
      pickingChecklistJson: null,
      bankAccountSnapshotJson: null,
    }
    const result = OrderSchema.safeParse(order)
    expect(result.success).toBe(true)
  })
})

// ── CounterpartySchema ─────────────────────────────────────────────────────

describe('CounterpartySchema', () => {
  it('parses valid counterparty', () => {
    const cp = {
      id: '00000000-0000-0000-0000-000000000001',
      name: 'ООО Тест',
      inn: '1234567890',
    }
    expect(CounterpartySchema.safeParse(cp).success).toBe(true)
  })

  it('rejects missing name', () => {
    const result = CounterpartySchema.safeParse({ id: '00000000-0000-0000-0000-000000000001' })
    expect(result.success).toBe(false)
  })
})

// ── ItemSchema ─────────────────────────────────────────────────────────────

describe('ItemSchema', () => {
  it('parses valid item', () => {
    const item = {
      id: '00000000-0000-0000-0000-000000000001',
      name: 'Товар 1',
      unit: 'шт',
      vatRate: 'Vat22',
    }
    expect(ItemSchema.safeParse(item).success).toBe(true)
  })
})

// ── InvoiceSchema ──────────────────────────────────────────────────────────

describe('InvoiceSchema', () => {
  it('parses valid invoice with snapshot fields', () => {
    const inv = {
      id: '00000000-0000-0000-0000-000000000001',
      date: '2026-05-31',
      status: 'Issued',
      organizationId: '00000000-0000-0000-0000-000000000002',
      counterpartyId: '00000000-0000-0000-0000-000000000003',
      counterpartyInnSnapshot: '1234567890',
      counterpartyKppSnapshot: '123456789',
      bankRequisitesSnapshotJson: '{"bik":"044525225"}',
      calculationSnapshotJson: '[{"name":"Товар"}]',
    }
    const result = InvoiceSchema.safeParse(inv)
    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.counterpartyInnSnapshot).toBe('1234567890')
      expect(result.data.lines).toEqual([])
    }
  })
})

// ── OrderStatusHistoryEntrySchema ──────────────────────────────────────────

describe('OrderStatusHistoryEntrySchema', () => {
  it('parses history entry', () => {
    const entry = {
      id: '00000000-0000-0000-0000-000000000001',
      status: 'Processing',
      statusName: 'Processing',
      changedAt: '2026-05-31T17:08:40.215254+00:00',
      changedBy: '7726bd58-63ec-410c-a80a-43f82cadf4c8',
      comment: 'Заказ подтверждён',
    }
    expect(OrderStatusHistoryEntrySchema.safeParse(entry).success).toBe(true)
  })

  it('allows null changedBy and comment', () => {
    const entry = {
      id: '00000000-0000-0000-0000-000000000001',
      status: 'WaitingForPayment',
      statusName: 'WaitingForPayment',
      changedAt: '2026-05-31T17:08:40.215254+00:00',
      changedBy: null,
      comment: null,
    }
    expect(OrderStatusHistoryEntrySchema.safeParse(entry).success).toBe(true)
  })
})

// ── PickingChecklistItemSchema ─────────────────────────────────────────────

describe('PickingChecklistItemSchema', () => {
  it('parses checklist item not done', () => {
    const item = {
      itemId: '00000000-0000-0000-0000-000000000001',
      name: 'Товар 1',
      qty: 2,
      done: false,
    }
    expect(PickingChecklistItemSchema.safeParse(item).success).toBe(true)
  })

  it('parses checklist item done with timestamp', () => {
    const item = {
      itemId: '00000000-0000-0000-0000-000000000001',
      name: 'Товар 1',
      qty: 2,
      done: true,
      doneBy: '7726bd58-63ec-410c-a80a-43f82cadf4c8',
      doneAt: '2026-06-02T13:42:00+00:00',
    }
    expect(PickingChecklistItemSchema.safeParse(item).success).toBe(true)
  })
})
