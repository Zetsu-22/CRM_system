import { createContext, useContext, useEffect, useState, useCallback } from 'react'
import type { ReactNode } from 'react'

export interface CompanyProfile {
  companyId: string | null
  name: string
  role: string
  isBasicProfile: boolean
  isCompanyOwner: boolean
}

export interface UserProfile {
  id: string
  email: string
  fullName: string | null
  phone: string | null
  companyId: string | null
  companyName: string | null
  isCompanyOwner: boolean
  isBasicProfile: boolean
  isAccessOrdersEdit: boolean
  isOrderPicker: boolean
  isExpeditor: boolean
  isDriver: boolean
  isAccessContractors: boolean
  isAccessContractorsEdit: boolean
  isAccessContractorDiscountEdit: boolean
  isAccessNomenclature: boolean
  isAccessNomenclatureEdit: boolean
  isAccessFinancialJournal: boolean
  isAccessFinancialJournalEdit: boolean
  isAccessWarehouses: boolean
  isAccessWarehousesEdit: boolean
  availableProfiles: CompanyProfile[]
}

interface AuthState {
  token: string | null
  profile: UserProfile | null
  isAuthenticated: boolean
  isLoading: boolean
}

interface AuthContextValue extends AuthState {
  login: (token: string, refreshToken: string | null, profile: UserProfile) => void
  logout: () => void
  updateProfile: (profile: UserProfile) => void
  switchProfile: (companyId: string | null) => Promise<void>
  // permission helpers — IsCompanyOwner OR IsBasicProfile bypasses all
  can: (perm: keyof UserProfile) => boolean
  canManageUsers: boolean
  hasCompany: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

const TOKEN_KEY = 'crm_token'
const REFRESH_KEY = 'crm_refresh'
const PROFILE_KEY = 'crm_profile'
const TARGET_COMPANY_KEY = 'targetCompanyId'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    token: null,
    profile: null,
    isAuthenticated: false,
    isLoading: true,
  })

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY)
    const raw = localStorage.getItem(PROFILE_KEY)
    if (token && raw) {
      try {
        const profile: UserProfile = JSON.parse(raw)
        if (isTokenValid(token)) {
          setState({ token, profile, isAuthenticated: true, isLoading: false })
          return
        }
      } catch { /* ignore */ }
    }
    // Expired or invalid
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_KEY)
    localStorage.removeItem(PROFILE_KEY)
    setState({ token: null, profile: null, isAuthenticated: false, isLoading: false })
  }, [])

  // Refresh token+profile from server on every app load.
  // /auth/me now returns { token, profile } with a fresh JWT built from DB state,
  // so stale tokens (e.g. pre-company-creation) are self-healed.
  useEffect(() => {
    if (!state.token || !state.isAuthenticated) return
    const controller = new AbortController()
    ;(async () => {
      try {
        const res = await fetch('/api/v1/auth/me', {
          method: 'GET',
          headers: { 'Authorization': `Bearer ${state.token}` },
          signal: controller.signal,
        })
        if (!res.ok) return
        const data: { token?: string; profile: UserProfile } = await res.json()
        const fresh = data.profile ?? (data as unknown as UserProfile)
        // Use fresh token if provided (self-heal stale companyId claims)
        const newToken = data.token ?? state.token ?? ''
        if (newToken) localStorage.setItem(TOKEN_KEY, newToken)
        localStorage.setItem(PROFILE_KEY, JSON.stringify(fresh))
        localStorage.setItem(TARGET_COMPANY_KEY, fresh.companyId ?? '0')
        setState(s => ({ ...s, token: newToken || s.token, profile: fresh }))
      } catch {
        // ignore
      }
    })()
    return () => controller.abort()
  }, [state.isAuthenticated])

  const login = (token: string, refreshToken: string | null, profile: UserProfile) => {
    localStorage.setItem(TOKEN_KEY, token)
    if (refreshToken) localStorage.setItem(REFRESH_KEY, refreshToken)
    localStorage.setItem(PROFILE_KEY, JSON.stringify(profile))
    localStorage.setItem(TARGET_COMPANY_KEY, profile.companyId ?? '0')
    setState({ token, profile, isAuthenticated: true, isLoading: false })
  }

  const logout = () => {
    const refreshToken = localStorage.getItem(REFRESH_KEY)
    // Best-effort revoke on the server. Не ждём, не блокируем UI.
    if (refreshToken) {
      fetch('/api/v1/auth/logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      }).catch(() => { /* ignore */ })
    }
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_KEY)
    localStorage.removeItem(PROFILE_KEY)
    localStorage.removeItem(TARGET_COMPANY_KEY)
    setState({ token: null, profile: null, isAuthenticated: false, isLoading: false })
  }

  const updateProfile = (profile: UserProfile) => {
    localStorage.setItem(PROFILE_KEY, JSON.stringify(profile))
    localStorage.setItem(TARGET_COMPANY_KEY, profile.companyId ?? '0')
    setState(s => ({ ...s, profile }))
  }

  const switchProfile = useCallback(async (companyId: string | null) => {
    const token = localStorage.getItem(TOKEN_KEY)
    if (!token) return
    try {
      const res = await fetch('/api/v1/auth/switch-company', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({ companyId }),
      })
      if (!res.ok) return
      const data = await res.json()
      if (data.token && data.profile) {
        localStorage.setItem(TARGET_COMPANY_KEY, data.profile.companyId ?? '0')
        try {
          window.dispatchEvent(new StorageEvent('storage', {
            key: TARGET_COMPANY_KEY,
            newValue: data.profile.companyId ?? '0',
            oldValue: localStorage.getItem(TARGET_COMPANY_KEY),
            storageArea: localStorage,
            url: window.location.href,
          }))
        } catch { /* ignore */ }
        login(data.token, data.refreshToken ?? null, data.profile)
      }
    } catch { /* ignore */ }
  }, [])

  const can = (perm: keyof UserProfile): boolean => {
    if (!state.profile) return false
    if (state.profile.isCompanyOwner || state.profile.isBasicProfile) return true
    return !!state.profile[perm]
  }

  const canManageUsers = !!(state.profile?.isCompanyOwner || state.profile?.isBasicProfile)
  const hasCompany = !!state.profile?.companyId

  return (
    <AuthContext.Provider value={{ ...state, login, logout, updateProfile, switchProfile, can, canManageUsers, hasCompany }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}

// Decode JWT payload (no signature verification — server validates)
function isTokenValid(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const expMs = payload.exp * 1000
    return Date.now() < expMs
  } catch {
    return false
  }
}
