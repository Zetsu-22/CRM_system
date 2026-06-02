import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../index'
import { QK } from '../queryKeys'
import type { Order, OrderStatusHistoryEntry } from '../schemas'

// ── Параметры поиска/фильтрации (Phase 6.11) ──────────────────────────────

export interface OrderSearchParams {
  search?: string
  statuses?: string[]   // OrderStatus enum names
  counterpartyId?: string
  dateFrom?: string     // ISO date string (YYYY-MM-DD)
  dateTo?: string
  totalFrom?: number
  totalTo?: number
}

function buildOrdersUrl(orgFilter?: string, params?: OrderSearchParams): string {
  const qs = new URLSearchParams()
  if (orgFilter) qs.set('organizationIds', orgFilter)
  if (params?.search) qs.set('search', params.search)
  if (params?.statuses?.length) qs.set('statuses', params.statuses.join(','))
  if (params?.counterpartyId) qs.set('counterpartyId', params.counterpartyId)
  if (params?.dateFrom) qs.set('dateFrom', params.dateFrom)
  if (params?.dateTo) qs.set('dateTo', params.dateTo)
  if (params?.totalFrom != null) qs.set('totalFrom', String(params.totalFrom))
  if (params?.totalTo != null) qs.set('totalTo', String(params.totalTo))
  const str = qs.toString()
  return str ? `/orders?${str}` : '/orders'
}

// ── Список заказов ─────────────────────────────────────────────────────────

export function useOrders(orgFilter?: string, searchParams?: OrderSearchParams) {
  return useQuery({
    queryKey: [...QK.orders(orgFilter), searchParams ?? {}],
    queryFn: () => api.get<Order[]>(buildOrdersUrl(orgFilter, searchParams)),
  })
}

// ── Один заказ ─────────────────────────────────────────────────────────────

export function useOrder(id: string | null) {
  return useQuery({
    queryKey: QK.order(id ?? ''),
    queryFn: () => api.get<Order>(`/orders/${id}`),
    enabled: !!id,
  })
}

// ── История статусов ───────────────────────────────────────────────────────

export function useOrderHistory(id: string | null, enabled: boolean) {
  return useQuery({
    queryKey: QK.orderHistory(id ?? ''),
    queryFn: () => api.get<OrderStatusHistoryEntry[]>(`/orders/${id}/status-history`),
    enabled: !!id && enabled,
  })
}

// ── Переход статуса (confirm / cancel / next-status) ──────────────────────

export function useOrderTransition() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, action }: { id: string; action: 'confirm' | 'cancel' | 'next-status' }) =>
      api.post<Order>(`/orders/${id}/${action}`, {}),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: ['orders'] })
      qc.invalidateQueries({ queryKey: QK.order(id) })
      qc.invalidateQueries({ queryKey: QK.orderHistory(id) })
    },
  })
}

// ── Создание заказа ────────────────────────────────────────────────────────

export function useCreateOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: unknown) => api.post<Order>('/orders', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['orders'] }),
  })
}

// ── Обновление заказа ──────────────────────────────────────────────────────

export function useUpdateOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: unknown }) => api.put<Order>(`/orders/${id}`, body),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: ['orders'] })
      qc.invalidateQueries({ queryKey: QK.order(id) })
    },
  })
}

// ── Удаление заказа ────────────────────────────────────────────────────────

export function useDeleteOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.delete(`/orders/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['orders'] }),
  })
}

// ── Отметить позицию чеклиста выполненной ──────────────────────────────────

export function useMarkPickingItemDone() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ orderId, itemId }: { orderId: string; itemId: string }) =>
      api.post<Order>(`/orders/${orderId}/picking-checklist/items/${itemId}/done`, {}),
    onSuccess: (_data, { orderId }) => {
      qc.invalidateQueries({ queryKey: QK.order(orderId) })
      qc.invalidateQueries({ queryKey: ['orders'] })
    },
  })
}
