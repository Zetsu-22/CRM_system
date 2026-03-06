import { NavLink } from 'react-router-dom'
import './Sidebar.css'

const NAV = [
  { to: '/',              label: 'Дашборд',      icon: '⊞' },
  { to: '/companies',     label: 'Организации',  icon: '🏢' },
  { to: '/counterparties',label: 'Контрагенты',  icon: '👥' },
  { to: '/items',         label: 'Номенклатура', icon: '📦' },
  { to: '/warehouses',    label: 'Склады',        icon: '🏭' },
  { to: '/stock',         label: 'Остатки',       icon: '📊' },
  { to: '/orders',        label: 'Заказы',        icon: '📋' },
  { to: '/invoices',      label: 'Счета',         icon: '🧾' },
  { to: '/upds',          label: 'УПД',           icon: '📄' },
  { to: '/payments',      label: 'Платежи',       icon: '💳' },
]

export function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="sidebarLogo">CRM</div>
      <nav className="sidebarNav">
        {NAV.map(n => (
          <NavLink
            key={n.to}
            to={n.to}
            end={n.to === '/'}
            className={({ isActive }) => 'sidebarLink' + (isActive ? ' active' : '')}
          >
            <span className="sidebarIcon">{n.icon}</span>
            <span className="sidebarLabel">{n.label}</span>
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
