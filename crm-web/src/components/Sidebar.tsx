import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../AuthContext'
import './Sidebar.css'

export function Sidebar() {
  const { profile, logout, canManageUsers, switchProfile } = useAuth()
  const navigate = useNavigate()
  const [profileOpen, setProfileOpen] = useState(false)
  const [switching, setSwitching] = useState(false)

  const owner = profile?.isCompanyOwner ?? false
  const profiles = (profile?.availableProfiles && profile.availableProfiles.length > 0)
    ? profile.availableProfiles
    : (profile
      ? [
        {
          companyId: null,
          name: profile.fullName ?? profile.email ?? 'Личный профиль',
          role: 'Личный',
          isBasicProfile: true,
          isCompanyOwner: false,
        },
        ...(profile.companyId
          ? [{
            companyId: profile.companyId,
            name: profile.companyName ?? 'Компания',
            role: profile.isCompanyOwner ? 'Владелец' : 'Сотрудник',
            isBasicProfile: false,
            isCompanyOwner: profile.isCompanyOwner,
          }]
          : []),
      ]
      : [])

  function handleLogout() {
    logout()
    navigate('/login')
  }

  async function handleSwitch(companyId: string | null) {
    setSwitching(true)
    setProfileOpen(false)
    await switchProfile(companyId)
    setSwitching(false)
  }

  // Текущий профиль для отображения
  const currentName = profile?.companyName ?? profile?.fullName ?? profile?.email ?? ''
  const currentRole = owner ? 'Владелец' : profile?.isBasicProfile
    ? (profile.companyId ? 'Администратор' : 'Личный профиль')
    : 'Сотрудник'

  return (
    <aside className="sidebar">
      <div className="sidebarLogo">CRM</div>

      {/* ── Profile Switcher ── */}
      {profile && profiles.length > 0 && (
        <div className="profileSwitcher">
          <button
            className="profileSwitcherBtn"
            onClick={() => setProfileOpen(o => !o)}
            disabled={switching}
          >
            <div className="profileSwitcherAvatar">
              {(profile?.companyName ?? profile?.fullName ?? 'U').charAt(0).toUpperCase()}
            </div>
            <div className="profileSwitcherInfo">
              <span className="profileSwitcherName">{currentName}</span>
              <span className="profileSwitcherRole">{currentRole}</span>
            </div>
            <span className="profileSwitcherChevron">{profileOpen ? '▲' : '▼'}</span>
          </button>

          {profileOpen && (
            <div className="profileDropdown">
              <div className="profileDropdownTitle">Ваши профили</div>
              {profiles.map((p, i) => {
                const isCurrent = p.companyId === profile?.companyId
                return (
                  <button
                    key={p.companyId ?? `basic-${i}`}
                    className={'profileDropdownItem' + (isCurrent ? ' active' : '')}
                    onClick={() => !isCurrent && handleSwitch(p.companyId)}
                    disabled={isCurrent || switching}
                  >
                    <div className="profileDropdownAvatar">
                      {p.name.charAt(0).toUpperCase()}
                    </div>
                    <div className="profileDropdownInfo">
                      <span className="profileDropdownName">{p.name}</span>
                      <span className="profileDropdownRole">{p.role}</span>
                    </div>
                    {isCurrent && <span className="profileDropdownCheck">✓</span>}
                  </button>
                )
              })}
            </div>
          )}
        </div>
      )}

      <nav className="sidebarNav">
        <SidebarLink to="/" icon="⊞" label="Дашборд" end />
        <SidebarLink to="/companies"      icon="🏢" label="Компания" />
        <SidebarLink to="/organizations"  icon="🏛️" label="Организации" />
        <SidebarLink to="/counterparties" icon="👥" label="Контрагенты" />
        <SidebarLink to="/items"          icon="📦" label="Номенклатура" />
        <SidebarLink to="/warehouses"     icon="🏭" label="Склады" />
        <SidebarLink to="/orders"         icon="📋" label="Заказы" />
        <SidebarLink to="/invoices"       icon="🧾" label="Счета" />
        <SidebarLink to="/upds"           icon="📄" label="УПД" />

        {canManageUsers && (
          <SidebarLink to="/users" icon="👤" label="Пользователи" />
        )}
      </nav>

      {/* ── Footer: user + logout ── */}
      <div className="sidebarFooter">
        <div className="sidebarUser">
          <span className="sidebarUserName">{profile?.fullName ?? profile?.email}</span>
          <span className="sidebarUserRole">{currentRole}</span>
        </div>
        <button className="sidebarLogout" onClick={handleLogout} title="Выйти">⏻</button>
      </div>
    </aside>
  )
}

function SidebarLink({ to, icon, label, end }: { to: string; icon: string; label: string; end?: boolean }) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) => 'sidebarLink' + (isActive ? ' active' : '')}
    >
      <span className="sidebarIcon">{icon}</span>
      <span className="sidebarLabel">{label}</span>
    </NavLink>
  )
}
