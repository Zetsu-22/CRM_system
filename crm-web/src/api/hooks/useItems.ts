import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../index'
import { QK } from '../queryKeys'
import type { Item } from '../schemas'

export function useItems(orgFilter?: string) {
  return useQuery({
    queryKey: QK.items(orgFilter),
    queryFn: () => api.get<Item[]>('/items'),
  })
}

export function useCreateItem() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: unknown) => api.post<Item>('/items', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['items'] }),
  })
}

export function useUpdateItem() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: unknown }) =>
      api.put<Item>(`/items/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['items'] }),
  })
}

export function useDeleteItem() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.delete(`/items/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['items'] }),
  })
}
