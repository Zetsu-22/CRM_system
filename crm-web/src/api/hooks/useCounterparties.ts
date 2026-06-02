import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../index'
import { QK } from '../queryKeys'
import type { Counterparty } from '../schemas'

export function useCounterparties(orgFilter?: string) {
  return useQuery({
    queryKey: QK.counterparties(orgFilter),
    queryFn: () => api.get<Counterparty[]>('/counterparties'),
  })
}

export function useCreateCounterparty() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: unknown) => api.post<Counterparty>('/counterparties', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['counterparties'] }),
  })
}

export function useUpdateCounterparty() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: unknown }) =>
      api.put<Counterparty>(`/counterparties/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['counterparties'] }),
  })
}

export function useDeleteCounterparty() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.delete(`/counterparties/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['counterparties'] }),
  })
}
