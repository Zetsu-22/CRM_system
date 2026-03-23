import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard, Building2, Landmark, Users, Package,
  Warehouse, ClipboardList, Receipt, FileText,
  ChevronLeft, ChevronRight, LogOut, ChevronDown,
  UserCog, type LucideIcon,
} from 'lucide-react'
import { useAuth } from '../AuthContext'
import './Sidebar.css'

interface NavItemDef {
  to: string
  icon: LucideIcon
  label: string
  end?: boolean
  adminOnly?: boolean
}

const NAV_ITEMS: NavItemDef[] = [
  { to: '/',              icon: LayoutDashboard, label: 'Дашборд',      end: true },
  { to: '/companies',     icon: Building2,       label: 'Компания' },
  { to: '/organizations', icon: Landmark,        label: 'Организации' },
  { to: '/counterparties',icon: Users,           label: 'Контрагенты' },
  { to: '/items',         icon: Package,         label: 'Номенклатура' },
  { to: '/warehouses',    icon: Warehouse,       label: 'Склады' },
  { to: '/orders',        icon: ClipboardList,   label: 'Заказы' },
  { to: '/invoices',      icon: Receipt,         label: 'Счета' },
  { to: '/upds',          icon: FileText,        label: 'УПД' },
  { to: '/users',         icon: UserCog,         label: 'Пользователи', adminOnly: true },
]

export function Sidebar() {
  const { profile, logout, canManageUsers, switchProfile } = useAuth()
  const navigate = useNavigate()
  const [collapsed, setCollapsed] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const [switching, setSwitching] = useState(false)

  const profiles = (profile?.availableProfiles && profile.availableProfiles.length > 0)
    ? profile.availableProfiles
    : profile
      ? [
          { companyId: null,           name: profile.fullName ?? profile.email ?? 'Личный',  role: 'Личный профиль',  isBasicProfile: true,  isCompanyOwner: false },
          ...(profile.companyId ? [{ companyId: profile.companyId, name: profile.companyName ?? 'Компания', role: profile.isCompanyOwner ? 'Владелец' : 'Сотрудник', isBasicProfile: false, isCompanyOwner: profile.isCompanyOwner }] : []),
        ]
      : []

  const currentName = profile?.companyName ?? profile?.fullName ?? profile?.email ?? ''
  const currentRole = profile?.isCompanyOwner
    ? 'Владелец'
    : profile?.isBasicProfile
      ? (profile.companyId ? 'Администратор' : 'Личный профиль')
      : 'Сотрудник'

  const initials = (currentName || 'U').slice(0, 2).toUpperCase()

  function handleLogout() { logout(); navigate('/login') }

  async function handleSwitch(companyId: string | null) {
    setSwitching(true); setProfileOpen(false)
    await switchProfile(companyId)
    setSwitching(false)
  }

  return (
    <aside className={`sidebar${collapsed ? ' collapsed' : ''}`}>
      {/* ── Logo + collapse toggle ── */}
      <div className="sidebarHeader">
        {!collapsed && (
          <div className="sidebarLogo">
            <div className="sidebarLogoIcon">C</div>
            <span className="sidebarLogoText">CRM</span>
          </div>
        )}
        {collapsed && (
          <button className="sidebarLogoBtn" onClick={() => setCollapsed(false)} title="Развернуть">
            <div className="sidebarLogoIcon">C</div>
          </button>
        )}
        {!collapsed && (
          <button className="sidebarCollapseBtn" onClick={() => setCollapsed(true)} title="Свернуть">
            <ChevronLeft size={16} />
          </button>
        )}
      </div>

      {/* ── Profile section ── */}
      {profile && (
        <div className="sidebarProfileWrap">
          <button
            className="sidebarProfileBtn"
            onClick={() => setProfileOpen(o => !o)}
            disabled={switching}
          >
            <div className="sidebarAvatar">{initials}</div>
            {!collapsed && (
              <>
                <div className="sidebarProfileInfo">
                  <span className="sidebarProfileName">{currentName}</span>
                  <span className="sidebarProfileRole">{currentRole}</span>
                </div>
                <ChevronDown size={14} className={`sidebarProfileChevron${profileOpen ? ' open' : ''}`} />
              </>
            )}
          </button>

          {profileOpen && !collapsed && profiles.length > 0 && (
            <div className="sidebarProfileDropdown">
              <div className="sidebarProfileDropdownTitle">Выбор профиля</div>
              {profiles.map((p, i) => {
                const isCurrent = p.companyId === profile.companyId
                return (
                  <button
                    key={p.companyId ?? `basic-${i}`}
                    className={`sidebarProfileDropdownItem${isCurrent ? ' active' : ''}`}
                    onClick={() => !isCurrent && handleSwitch(p.companyId)}
                    disabled={isCurrent || switching}
                  >
                    <div className="sidebarDropdownAvatar">{p.name.slice(0, 2).toUpperCase()}</div>
                    <div className="sidebarDropdownInfo">
                      <span className="sidebarDropdownName">{p.name}</span>
                      <span className="sidebarDropdownRole">{p.role}</span>
                    </div>
                    {isCurrent && <span className="sidebarDropdownCheck">✓</span>}
                  </button>
                )
              })}
            </div>
          )}
        </div>
      )}

      {/* ── Navigation ── */}
      <nav className="sidebarNav">
        {NAV_ITEMS.filter(item => !item.adminOnly || canManageUsers).map(item => (
          <SidebarNavLink key={item.to} item={item} collapsed={collapsed} />
        ))}
      </nav>

      {/* ── Footer: expand + logout ── */}
      <div className="sidebarFooter">
        {collapsed && (
          <button className="sidebarFooterBtn" onClick={() => setCollapsed(false)} title="Развернуть">
            <ChevronRight size={16} />
          </button>
        )}
        <button className="sidebarLogoutBtn" onClick={handleLogout} title="Выйти">
          <LogOut size={16} />
          {!collapsed && <span>Выйти</span>}
        </button>
      </div>
    </aside>
  )
}

function SidebarNavLink({ item, collapsed }: { item: NavItemDef; collapsed: boolean }) {
  const Icon = item.icon
  return (
    <NavLink
      to={item.to}
      end={item.end}
      title={collapsed ? item.label : undefined}
      className={({ isActive }) => `sidebarNavItem${isActive ? ' active' : ''}`}
    >
      <Icon size={18} className="sidebarNavIcon" />
      {!collapsed && <span className="sidebarNavLabel">{item.label}</span>}
    </NavLink>
  )
}
