import { useState, useRef, useEffect } from 'react'
import { Building2, Landmark, ChevronDown, Search, X } from 'lucide-react'
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

  const [search, setSearch] = useState('')

  if (!profile?.companyId) return null

  return (
    <header className="topBar">
      {/* ── Left: company badge ── */}
      <div className="topBarLeft">
        <div className="topBarCompanyBadge">
          <Building2 size={14} className="topBarCompanyIcon" />
          <span className="topBarCompany">{companyName}</span>
        </div>
      </div>

      {/* ── Center: search ── */}
      <div className="topBarSearch">
        <Search size={14} className="topBarSearchIcon" />
        <input
          className="topBarSearchInput"
          placeholder="Поиск..."
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
        {search && (
          <button className="topBarSearchClear" onClick={() => setSearch('')}>
            <X size={13} />
          </button>
        )}
      </div>

      {/* ── Right: org filter ── */}
      <div className="topBarRight">
        {hasOrgs && (
          <div className="topBarOrgWrap" ref={ref}>
            <button
              className={`topBarOrgBtn${open ? ' active' : ''}`}
              onClick={() => setOpen(o => !o)}
              title="Фильтр по организациям"
            >
              <Landmark size={14} />
              <span className="topBarOrgLabel">
                {isAllSelected ? 'Все организации' : `${displayCount} из ${organizations.length}`}
              </span>
              {!isAllSelected && <span className="topBarOrgDot" />}
              <ChevronDown size={13} className={`topBarOrgChevron${open ? ' open' : ''}`} />
            </button>

            {open && (
              <div className="topBarOrgDropdown">
                <div className="topBarOrgHeader">
                  <span>Организации</span>
                  <button className="topBarOrgSelectAll" onClick={selectAll}>
                    {isAllSelected ? 'Сбросить' : 'Все'}
                  </button>
                </div>
                {isLoading ? (
                  <div className="topBarOrgEmpty">Загрузка...</div>
                ) : organizations.length === 0 ? (
                  <div className="topBarOrgEmpty">Нет организаций</div>
                ) : (
                  <div className="topBarOrgList">
                    {organizations.map(org => {
                      const explicit = selectedIds.includes(org.id)
                      const checked = isAllSelected || explicit
                      return (
                        <label key={org.id} className={`topBarOrgItem${checked ? ' checked' : ''}`}>
                          <input
                            type="checkbox"
                            checked={checked}
                            onChange={() => toggle(org.id)}
                          />
                          <span className="topBarOrgName">{org.name}</span>
                          {org.isDefault && <span className="topBarOrgDefault">осн.</span>}
                        </label>
                      )
                    })}
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  )
}
