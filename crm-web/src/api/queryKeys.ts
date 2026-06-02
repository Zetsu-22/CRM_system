/**
 * Централизованные query keys — исключают опечатки и облегчают инвалидацию.
 * Используем tuple-формат для точечной инвалидации (invalidate по префиксу).
 */
export const QK = {
  orders:        (orgFilter?: string) => ['orders', orgFilter ?? 'all'] as const,
  order:         (id: string)          => ['orders', id]               as const,
  orderHistory:  (id: string)          => ['orders', id, 'history']    as const,
  counterparties:(orgFilter?: string)  => ['counterparties', orgFilter ?? 'all'] as const,
  items:         (orgFilter?: string)  => ['items', orgFilter ?? 'all']          as const,
  invoices:      (orgFilter?: string)  => ['invoices', orgFilter ?? 'all']       as const,
  upds:          (orgFilter?: string)  => ['upds', orgFilter ?? 'all']           as const,
  organizations: ()                    => ['organizations']            as const,
  users:         ()                    => ['users']                    as const,
} as const
