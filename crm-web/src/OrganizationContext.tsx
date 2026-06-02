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
  currentOrganization: OrgItem | null   // active org for creating new objects
  setCurrentOrganization: (org: OrgItem) => void
  selectedIds: string[]                  // [] = all orgs (for list filtering)
  setSelectedIds: (ids: string[]) => void
  isLoading: boolean
  reload: () => void
  /** Comma-separated org IDs for ?organizationIds= query param, "" = all */
  filterString: string
}

const OrgContext = createContext<OrgContextValue | null>(null)

const STORAGE_KEY_FILTER = 'selected_organization_ids_filter'
const STORAGE_KEY_CURRENT = 'current_organization_id'

function readStoredIds(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_FILTER)
    if (raw) {
      const parsed = JSON.parse(raw)
      if (Array.isArray(parsed)) return parsed as string[]
    }
  } catch { /* ignore */ }
  return []
}

function readStoredCurrentId(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY_CURRENT)
  } catch { return null }
}

export function OrganizationProvider({ children, companyId }: { children: ReactNode; companyId: string | null }) {
  const [organizations, setOrganizations] = useState<OrgItem[]>([])
  const [currentOrganization, setCurrentOrganizationState] = useState<OrgItem | null>(null)
  const [selectedIds, setSelectedIdsState] = useState<string[]>(readStoredIds)
  const [isLoading, setIsLoading] = useState(false)

  const load = useCallback(() => {
    if (!companyId) { setOrganizations([]); setCurrentOrganizationState(null); setIsLoading(false); return }
    setIsLoading(true)
    api.get<OrgItem[]>('/organizations')
      .then(orgs => {
        setOrganizations(orgs)
        // Restore or pick default current org
        const storedId = readStoredCurrentId()
        const restored = storedId ? orgs.find(o => o.id === storedId) : null
        const picked = restored ?? orgs.find(o => o.isDefault) ?? orgs[0] ?? null
        setCurrentOrganizationState(picked)
        if (picked) localStorage.setItem(STORAGE_KEY_CURRENT, picked.id)
      })
      .catch(() => { setOrganizations([]); setCurrentOrganizationState(null) })
      .finally(() => setIsLoading(false))
  }, [companyId])

  useEffect(() => { load() }, [load])

  useEffect(() => {
    const handleOrganizationsChanged = () => load()
    const handleFocus = () => load()
    const handleVisibility = () => {
      if (document.visibilityState === 'visible') load()
    }

    window.addEventListener('organizations:changed', handleOrganizationsChanged)
    window.addEventListener('focus', handleFocus)
    document.addEventListener('visibilitychange', handleVisibility)

    return () => {
      window.removeEventListener('organizations:changed', handleOrganizationsChanged)
      window.removeEventListener('focus', handleFocus)
      document.removeEventListener('visibilitychange', handleVisibility)
    }
  }, [load])

  useEffect(() => {
    const stored = readStoredIds()
    setSelectedIdsState(stored)
  }, [companyId])

  const setCurrentOrganization = useCallback((org: OrgItem) => {
    setCurrentOrganizationState(org)
    localStorage.setItem(STORAGE_KEY_CURRENT, org.id)
  }, [])

  const setSelectedIds = useCallback((ids: string[]) => {
    setSelectedIdsState(ids)
    if (ids.length > 0) {
      localStorage.setItem(STORAGE_KEY_FILTER, JSON.stringify(ids))
    } else {
      localStorage.removeItem(STORAGE_KEY_FILTER)
    }
  }, [])

  const filterString = selectedIds.length > 0 ? selectedIds.join(',') : ''

  return (
    <OrgContext.Provider value={{
      organizations, currentOrganization, setCurrentOrganization,
      selectedIds, setSelectedIds, isLoading, reload: load, filterString
    }}>
      {children}
    </OrgContext.Provider>
  )
}

export function useOrganizations(): OrgContextValue {
  const ctx = useContext(OrgContext)
  if (!ctx) throw new Error('useOrganizations must be inside OrganizationProvider')
  return ctx
}
