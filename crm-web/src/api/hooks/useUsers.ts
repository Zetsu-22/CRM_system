import { useQuery } from '@tanstack/react-query'
import { api } from '../index'
import { QK } from '../queryKeys'
import type { User } from '../schemas'

export function useUsers() {
  return useQuery({
    queryKey: QK.users(),
    queryFn: () => api.get<User[]>('/users'),
  })
}
