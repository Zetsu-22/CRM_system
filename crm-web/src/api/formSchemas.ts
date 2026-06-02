/**
 * Zod-схемы для валидации форм.
 * В отличие от schemas.ts (API-ответы), эти схемы применяются
 * к данным, которые пользователь вводит перед отправкой.
 */
import { z } from 'zod'

// ── Контрагент ─────────────────────────────────────────────────────────────

export const CounterpartyFormSchema = z.object({
  name: z.string().min(1, 'Название обязательно').max(200, 'Слишком длинное название'),
  fullName: z.string().max(500).optional(),
  type: z.enum(['Buyer', 'Supplier', 'Both']),
  status: z.enum(['Active', 'Blocked']),
  inn: z.string().max(12).optional().or(z.literal('')),
  kpp: z.string().max(9).optional().or(z.literal('')),
  ogrn: z.string().max(15).optional().or(z.literal('')),
  paymentDeferDays: z.coerce.number().int().min(0).max(365).optional(),
})
export type CounterpartyForm = z.infer<typeof CounterpartyFormSchema>

// ── Номенклатура ───────────────────────────────────────────────────────────

export const ItemFormSchema = z.object({
  name: z.string().min(1, 'Название обязательно').max(200),
  sku: z.string().max(100).optional().or(z.literal('')),
  type: z.enum(['Product', 'Service']),
  unit: z.string().min(1, 'Единица измерения обязательна').max(20),
  vatRate: z.enum(['Vat0', 'Vat5', 'Vat7', 'Vat10', 'Vat22']),
  price: z.coerce.number().min(0).optional(),
})
export type ItemForm = z.infer<typeof ItemFormSchema>

// ── Строка заказа ─────────────────────────────────────────────────────────

export const OrderLineFormSchema = z.object({
  itemId: z.string().min(1, 'Выберите товар'),
  nameSnapshot: z.string(),
  vatRateSnapshot: z.string(),
  qty: z.coerce.number().positive('Количество должно быть > 0'),
  unitPrice: z.coerce.number().min(0, 'Цена не может быть отрицательной'),
  discountType: z.enum(['None', 'Percent', 'Fixed']),
  discountValue: z.coerce.number().min(0),
})
export type OrderLineForm = z.infer<typeof OrderLineFormSchema>

// ── Заказ ─────────────────────────────────────────────────────────────────

export const OrderFormSchema = z.object({
  number: z.string().max(32).optional().or(z.literal('')),
  date: z.string().min(1, 'Дата обязательна'),
  organizationId: z.string().min(1, 'Выберите организацию'),
  counterpartyId: z.string().min(1, 'Выберите контрагента'),
  comment: z.string().max(1000).optional().or(z.literal('')),
  bankAccount: z.string().max(100).optional().or(z.literal('')),
  contract: z.string().max(100).optional().or(z.literal('')),
  deliveryAddress: z.string().max(500).optional().or(z.literal('')),
  pickerId: z.string().optional().or(z.literal('')),
  driverId: z.string().optional().or(z.literal('')),
  lines: z.array(OrderLineFormSchema).min(1, 'Добавьте хотя бы одну позицию'),
})
export type OrderForm = z.infer<typeof OrderFormSchema>

// ── Счёт ──────────────────────────────────────────────────────────────────

export const InvoiceLineFormSchema = z.object({
  itemId: z.string().min(1, 'Выберите товар'),
  nameSnapshot: z.string(),
  vatRateSnapshot: z.string(),
  qty: z.coerce.number().positive('Количество должно быть > 0'),
  unitPrice: z.coerce.number().min(0),
  discountType: z.enum(['None', 'Percent', 'Fixed']),
  discountValue: z.coerce.number().min(0),
})

export const InvoiceFormSchema = z.object({
  organizationId: z.string().min(1, 'Выберите организацию'),
  counterpartyId: z.string().min(1, 'Выберите контрагента'),
  orderId: z.string().optional().or(z.literal('')),
  date: z.string().min(1, 'Дата обязательна'),
  comment: z.string().max(1000).optional().or(z.literal('')),
  lines: z.array(InvoiceLineFormSchema).min(1, 'Добавьте хотя бы одну позицию'),
})
export type InvoiceForm = z.infer<typeof InvoiceFormSchema>

// ── Организация ───────────────────────────────────────────────────────────

export const OrganizationFormSchema = z.object({
  name: z.string().min(1, 'Название обязательно').max(200),
  address: z.string().max(500).optional().or(z.literal('')),
  isDefault: z.boolean().optional(),
})
export type OrganizationForm = z.infer<typeof OrganizationFormSchema>

// ── Хелпер: извлечь сообщения об ошибках из ZodError ────────────────────

export function getZodErrors(error: z.ZodError): Record<string, string> {
  const result: Record<string, string> = {}
  for (const issue of error.issues) {
    const path = issue.path.join('.')
    if (!result[path]) result[path] = issue.message
  }
  return result
}

/**
 * Валидирует форму и показывает toast с первой ошибкой.
 * Возвращает true если форма валидна.
 */
export function validateForm<T>(
  schema: z.ZodSchema<T>,
  data: unknown,
  onError: (msg: string) => void,
): data is T {
  const result = schema.safeParse(data)
  if (!result.success) {
    const firstError = result.error.issues[0]
    const path = firstError.path.length > 0 ? `${firstError.path.join('.')}: ` : ''
    onError(path + firstError.message)
    return false
  }
  return true
}
