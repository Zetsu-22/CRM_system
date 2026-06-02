import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../index'
import { QK } from '../queryKeys'
import type { Invoice } from '../schemas'

export function useInvoices(orgFilter?: string) {
  return useQuery({
    queryKey: QK.invoices(orgFilter),
    queryFn: () => api.get<Invoice[]>('/invoices'),
  })
}

export function useCreateInvoice() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: unknown) => api.post<Invoice>('/invoices', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['invoices'] }),
  })
}

export function useUpdateInvoice() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: unknown }) =>
      api.put<Invoice>(`/invoices/${id}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['invoices'] }),
  })
}

export function useDeleteInvoice() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.delete(`/invoices/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['invoices'] }),
  })
}
