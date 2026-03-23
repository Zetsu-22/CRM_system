import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react'
import { api } from './api'

export interface OrgItem {
  id: string
  name: string
  address: string | null
  isDefault: boolean
}

interface OrgContextValue {
  organizations: OrgItem[]
  selectedIds: string[]             // [] = все организации
  setSelectedIds: (ids: string[]) => void
  isLoading: boolean
  reload: () => void
  /** Строка для query-param: "id1,id2" или "" (все) */
  filterString: string
}

const OrgContext = createContext<OrgContextValue | null>(null)

const STORAGE_KEY = 'selected_organization_ids_filter'

function readStoredIds(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) {
      const parsed = JSON.parse(raw)
      if (Array.isArray(parsed)) return parsed as string[]
    }
  } catch { /* ignore */ }
  return []
}

export function OrganizationProvider({ children, companyId }: { children: ReactNode; companyId: string | null }) {
  const [organizations, setOrganizations] = useState<OrgItem[]>([])
  const [selectedIds, setSelectedIdsState] = useState<string[]>(readStoredIds)
  const [isLoading, setIsLoading] = useState(false)

  const load = useCallback(() => {
    if (!companyId) { setOrganizations([]); return }
    setIsLoading(true)
    api.get<OrgItem[]>('/organizations')
      .then(setOrganizations)
      .catch(() => { /* silent */ })
      .finally(() => setIsLoading(false))
  }, [companyId])

  useEffect(() => { load() }, [load])

  // Reset stored selection when company changes
  useEffect(() => {
    const stored = readStoredIds()
    setSelectedIdsState(stored)
  }, [companyId])

  const setSelectedIds = useCallback((ids: string[]) => {
    setSelectedIdsState(ids)
    if (ids.length > 0) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(ids))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  }, [])

  const filterString = selectedIds.length > 0 ? selectedIds.join(',') : ''

  return (
    <OrgContext.Provider value={{ organizations, selectedIds, setSelectedIds, isLoading, reload: load, filterString }}>
      {children}
    </OrgContext.Provider>
  )
}

export function useOrganizations(): OrgContextValue {
  const ctx = useContext(OrgContext)
  if (!ctx) throw new Error('useOrganizations must be inside OrganizationProvider')
  return ctx
}
