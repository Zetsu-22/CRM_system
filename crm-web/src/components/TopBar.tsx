import { useState, useRef, useEffect } from 'react'
import { useAuth } from '../AuthContext'
import { useOrganizations } from '../OrganizationContext'
import './TopBar.css'

export function TopBar() {
  const { profile } = useAuth()
  const { organizations, selectedIds, setSelectedIds, isLoading } = useOrganizations()

  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  // Close on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const companyName = profile?.companyName ?? profile?.fullName ?? '—'
  const hasOrgs = organizations.length > 0

  const isAllSelected = selectedIds.length === 0 || selectedIds.length === organizations.length
  const displayCount = isAllSelected ? organizations.length : selectedIds.length

  const toggle = (id: string) => {
    if (selectedIds.includes(id)) {
      const next = selectedIds.filter(x => x !== id)
      setSelectedIds(next)
    } else {
      setSelectedIds([...selectedIds, id])
    }
  }

  const selectAll = () => setSelectedIds([])

  if (!profile?.companyId) return null

  return (
    <header className="topBar">
      <div className="topBarLeft">
        <span className="topBarCompany">{companyName}</span>
      </div>

      {hasOrgs && (
        <div className="topBarOrgWrap" ref={ref}>
          <button
            className={`topBarOrgBtn${open ? ' active' : ''}`}
            onClick={() => setOpen(o => !o)}
            title="Фильтр по организациям"
          >
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
              <polyline points="9 22 9 12 15 12 15 22"/>
            </svg>
            <span className="topBarOrgCount">{displayCount}</span>
            {!isAllSelected && <span className="topBarOrgDot" />}
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" style={{ marginLeft: 2 }}>
              <polyline points={open ? '18 15 12 9 6 15' : '6 9 12 15 18 9'} />
            </svg>
          </button>

          {open && (
            <div className="topBarOrgDropdown">
              <div className="topBarOrgHeader">
                <span>Организации</span>
                {!isAllSelected && (
                  <button className="topBarOrgSelectAll" onClick={selectAll}>Все</button>
                )}
              </div>
              {isLoading ? (
                <div className="topBarOrgEmpty">Загрузка...</div>
              ) : organizations.length === 0 ? (
                <div className="topBarOrgEmpty">Нет организаций</div>
              ) : (
                <div className="topBarOrgList">
                  {organizations.map(org => {
                    const checked = selectedIds.length === 0 || selectedIds.includes(org.id)
                    const explicit = selectedIds.includes(org.id)
                    return (
                      <label key={org.id} className={`topBarOrgItem${checked ? ' checked' : ''}`}>
                        <input
                          type="checkbox"
                          checked={isAllSelected ? true : explicit}
                          onChange={() => toggle(org.id)}
                        />
                        <span className="topBarOrgName">{org.name}</span>
                        {org.isDefault && <span className="topBarOrgDefault">✓</span>}
                      </label>
                    )
                  })}
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </header>
  )
}
