/**
 * Zod-схемы для API-ответов CRM.
 * Используются для валидации данных при разработке и в тестах.
 *
 * Zod v4 notes:
 * - z.uuid() строго валидирует version/variant биты — для API-ответов используем z.string()
 * - z.email() теперь top-level, а не метод строки
 */
import { z } from 'zod'

// helpers — Zod v4 .uuid() валидирует version/variant биты, для API-ответов используем z.string()
const uuid = () => z.string()    // ID из API — просто строки
const nullableStr = () => z.string().nullable().optional()

// ── Общие ──────────────────────────────────────────────────────────────────

export const PagedResponseSchema = <T extends z.ZodTypeAny>(itemSchema: T) =>
  z.object({
    items: z.array(itemSchema),
    total: z.number(),
    page: z.number(),
    pageSize: z.number(),
    totalPages: z.number(),
  })

// ── Организации ─────────────────────────────────────────────────────────────

export const OrganizationSchema = z.object({
  id: uuid(),
  name: z.string(),
  inn: nullableStr(),
  kpp: nullableStr(),
})
export type Organization = z.infer<typeof OrganizationSchema>

// ── Контрагенты ──────────────────────────────────────────────────────────────

export const CounterpartySchema = z.object({
  id: uuid(),
  name: z.string(),
  inn: nullableStr(),
  kpp: nullableStr(),
  phone: nullableStr(),
  email: nullableStr(),
})
export type Counterparty = z.infer<typeof CounterpartySchema>

// ── Номенклатура ──────────────────────────────────────────────────────────────

export const ItemSchema = z.object({
  id: uuid(),
  name: z.string(),
  sku: nullableStr(),
  unit: z.string(),
  vatRate: z.string(),
  price: z.number().nullable().optional(),
})
export type Item = z.infer<typeof ItemSchema>

// ── Заказы ───────────────────────────────────────────────────────────────────

export const OrderLineSchema = z.object({
  id: uuid(),
  itemId: uuid(),
  nameSnapshot: z.string(),
  vatRateSnapshot: z.string(),
  qty: z.number(),
  unitPrice: z.number(),
  discountType: z.string(),
  discountValue: z.number(),
  net: z.number(),
  vat: z.number(),
  gross: z.number(),
})
export type OrderLine = z.infer<typeof OrderLineSchema>

export const OrderSchema = z.object({
  id: uuid(),
  number: z.string().nullable(),
  date: z.string(),
  status: z.string(),
  vatMode: nullableStr(),
  totalNet: z.number(),
  totalVat: z.number(),
  totalGross: z.number(),
  comment: nullableStr(),
  isAssemblyChecklistClosed: z.boolean(),
  pickingChecklistJson: nullableStr(),
  bankAccountSnapshotJson: nullableStr(),
  deliveryAddressSnapshotJson: nullableStr(),
  rowVersion: z.number().optional(),
  organization: OrganizationSchema.nullable().optional(),
  counterparty: CounterpartySchema.nullable().optional(),
  lines: z.array(OrderLineSchema).optional().default([]),
  pickerId: nullableStr(),
  driverId: nullableStr(),
  bankAccount: nullableStr(),
  contract: nullableStr(),
  deliveryAddress: nullableStr(),
})
export type Order = z.infer<typeof OrderSchema>

export const OrderStatusHistoryEntrySchema = z.object({
  id: uuid(),
  status: z.string(),
  statusName: z.string(),
  changedAt: z.string(),
  changedBy: z.string().nullable(),
  comment: z.string().nullable(),
})
export type OrderStatusHistoryEntry = z.infer<typeof OrderStatusHistoryEntrySchema>

export const PickingChecklistItemSchema = z.object({
  itemId: uuid(),
  name: z.string(),
  qty: z.number(),
  done: z.boolean(),
  doneBy: nullableStr(),
  doneAt: nullableStr(),
})
export type PickingChecklistItem = z.infer<typeof PickingChecklistItemSchema>

// ── Счета ─────────────────────────────────────────────────────────────────────

export const InvoiceLineSchema = z.object({
  id: uuid().optional(),
  itemId: uuid(),
  nameSnapshot: z.string(),
  vatRateSnapshot: z.string(),
  qty: z.number(),
  unitPrice: z.number(),
  discountType: z.string(),
  discountValue: z.number(),
  net: z.number().optional(),
  vat: z.number().optional(),
  gross: z.number().optional(),
})
export type InvoiceLine = z.infer<typeof InvoiceLineSchema>

export const InvoiceSchema = z.object({
  id: uuid(),
  number: nullableStr(),
  date: z.string(),
  status: z.string(),
  comment: nullableStr(),
  organizationId: uuid(),
  counterpartyId: uuid(),
  orderId: nullableStr(),
  totalNet: z.number().optional(),
  totalVat: z.number().optional(),
  totalGross: z.number().optional(),
  organization: OrganizationSchema.nullable().optional(),
  counterparty: CounterpartySchema.nullable().optional(),
  lines: z.array(InvoiceLineSchema).optional().default([]),
  counterpartyInnSnapshot: nullableStr(),
  counterpartyKppSnapshot: nullableStr(),
  bankRequisitesSnapshotJson: nullableStr(),
  calculationSnapshotJson: nullableStr(),
})
export type Invoice = z.infer<typeof InvoiceSchema>

// ── Пользователи ──────────────────────────────────────────────────────────────

export const UserSchema = z.object({
  id: uuid(),
  email: z.string(),
  displayName: nullableStr(),
  role: z.string(),
  isOrderPicker: z.boolean().optional().default(false),
  isDriver: z.boolean().optional().default(false),
})
export type User = z.infer<typeof UserSchema>
