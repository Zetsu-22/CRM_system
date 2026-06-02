import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../index'
import { QK } from '../queryKeys'
import type { Order, OrderStatusHistoryEntry } from '../schemas'

// ── Список заказов ─────────────────────────────────────────────────────────

export function useOrders(orgFilter?: string) {
  return useQuery({
    queryKey: QK.orders(orgFilter),
    queryFn: () => api.get<Order[]>('/orders'),
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
