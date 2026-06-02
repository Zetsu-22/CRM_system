import { useState, useRef, useEffect } from 'react'
import { Building2, ChevronDown, Search, X } from 'lucide-react'
import { useAuth } from '../AuthContext'
import { useOrganizations } from '../OrganizationContext'
import './TopBar.css'

export function TopBar() {
  const { profile } = useAuth()
  const { organizations, selectedIds, setSelectedIds, isLoading } = useOrganizations()

  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const [search, setSearch] = useState('')

  // Close on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  if (!profile?.companyId) return null

  const isAllSelected = selectedIds.length === 0 || selectedIds.length === organizations.length

  // Label по правилам fimbiz AppHeader:
  //   · пусто/все    → "Все организации"
  //   · ровно 1     → имя этой организации
  //   · N≥2         → "Выбрано: N"
  let label = 'Организации'
  if (isLoading) {
    label = 'Загрузка...'
  } else if (organizations.length === 0) {
    label = 'Нет организаций'
  } else if (isAllSelected) {
    label = 'Все организации'
  } else if (selectedIds.length === 1) {
    label = organizations.find(o => o.id === selectedIds[0])?.name ?? 'Организация'
  } else {
    label = `Выбрано: ${selectedIds.length}`
  }

  const toggle = (id: string) => {
    // «Pure-state» логика: пустой selectedIds = выбраны все. При снятии одной из «all»
    // разворачиваем в явный список, чтобы не получить баг «odin выбран».
    let base = selectedIds
    if (isAllSelected && selectedIds.length === 0) {
      base = organizations.map(o => o.id)
    }
    if (base.includes(id)) {
      // Запрещаем снимать последнюю организацию — fimbiz parity.
      if (base.length > 1) setSelectedIds(base.filter(x => x !== id))
    } else {
      setSelectedIds([...base, id])
    }
  }

  const handleSelectAll = () => {
    if (isAllSelected) {
      // Снять выделение → оставляем первую организацию (fimbiz parity).
      if (organizations.length > 0) setSelectedIds([organizations[0].id])
    } else {
      setSelectedIds([])
    }
  }

  return (
    <header className="topBar">
      {/* ── Left: organizations multi-select (fimbiz AppHeader style) ── */}
      <div className="topBarLeft">
        <div className="topBarOrgWrap" ref={ref}>
          <button
            className={`topBarOrgBtn${open ? ' active' : ''}`}
            onClick={() => setOpen(o => !o)}
            title="Фильтр по организациям"
            disabled={isLoading}
          >
            <div className="topBarOrgIconWrap">
              <Building2 size={14} className="topBarOrgIcon" />
            </div>
            <span className="topBarOrgLabel">{label}</span>
            {!isAllSelected && organizations.length > 0 && <span className="topBarOrgDot" />}
            <ChevronDown size={13} className={`topBarOrgChevron${open ? ' open' : ''}`} />
          </button>

          {open && (
            <div className="topBarOrgDropdown topBarOrgDropdownLeft">
              <div className="topBarOrgHeader">
                <span>Фильтр организаций</span>
                {organizations.length > 0 && (
                  <button className="topBarOrgSelectAll" onClick={handleSelectAll}>
                    {isAllSelected ? 'Снять выделение' : 'Выбрать все'}
                  </button>
                )}
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

      {/* ── Right: reserved for future actions (notifications, quick-add) ── */}
      <div className="topBarRight" />
    </header>
  )
}
