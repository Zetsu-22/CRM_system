import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { PageLayout } from '../components/PageLayout'
import '../components/common.css'

interface Stats {
  companies: number
  counterparties: number
  items: number
  orders: number
  invoices: number
  payments: number
}

const TILES = [
  { key: 'companies',      label: 'Организации',  icon: '🏢', to: '/companies' },
  { key: 'counterparties', label: 'Контрагенты',  icon: '👥', to: '/counterparties' },
  { key: 'items',          label: 'Номенклатура', icon: '📦', to: '/items' },
  { key: 'orders',         label: 'Заказы',        icon: '📋', to: '/orders' },
  { key: 'invoices',       label: 'Счета',         icon: '🧾', to: '/invoices' },
  { key: 'payments',       label: 'Платежи',       icon: '💳', to: '/payments' },
] as const

export function DashboardPage() {
  const navigate = useNavigate()
  const [stats, setStats] = useState<Partial<Stats>>({})
  const [apiOk, setApiOk] = useState<boolean | null>(null)

  useEffect(() => {
    fetch('/api/v1/health')
      .then(r => r.ok ? setApiOk(true) : setApiOk(false))
      .catch(() => setApiOk(false))

    Promise.allSettled([
      api.get<any[]>('/companies'),
      api.get<any[]>('/counterparties'),
      api.get<any[]>('/items'),
      api.get<any[]>('/orders'),
      api.get<any[]>('/invoices'),
      api.get<any[]>('/payments'),
    ]).then(([c, cp, it, o, inv, p]) => {
      setStats({
        companies:      c.status === 'fulfilled'   ? c.value.length   : 0,
        counterparties: cp.status === 'fulfilled'  ? cp.value.length  : 0,
        items:          it.status === 'fulfilled'  ? it.value.length  : 0,
        orders:         o.status === 'fulfilled'   ? o.value.length   : 0,
        invoices:       inv.status === 'fulfilled' ? inv.value.length : 0,
        payments:       p.status === 'fulfilled'   ? p.value.length   : 0,
      })
    })
  }, [])

  return (
    <PageLayout title="Панель управления">
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 24, padding: '12px 16px', background: 'var(--panel)', border: '1px solid var(--border)', borderRadius: 10, width: 'fit-content' }}>
        <span style={{ fontSize: 13, opacity: .7 }}>Статус API:</span>
        {apiOk === null
          ? <span className="badge badge-gray">Проверка…</span>
          : apiOk
            ? <span className="badge badge-green">Подключено</span>
            : <span className="badge badge-red">Нет связи</span>}
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 12 }}>
        {TILES.map(t => (
          <button
            key={t.key}
            onClick={() => navigate(t.to)}
            style={{
              textAlign: 'left', padding: '18px 16px', borderRadius: 14,
              border: '1px solid var(--border)', background: 'var(--panel)',
              cursor: 'pointer', transition: 'all 0.12s',
            }}
            onMouseEnter={e => {
              (e.currentTarget as HTMLElement).style.borderColor = 'rgba(120,148,255,0.5)'
              ;(e.currentTarget as HTMLElement).style.background = 'rgba(27,32,50,0.9)'
            }}
            onMouseLeave={e => {
              (e.currentTarget as HTMLElement).style.borderColor = 'var(--border)'
              ;(e.currentTarget as HTMLElement).style.background = 'var(--panel)'
            }}
          >
            <div style={{ fontSize: 28, marginBottom: 8 }}>{t.icon}</div>
            <div style={{ fontWeight: 600, fontSize: 15 }}>{t.label}</div>
            <div style={{ marginTop: 6, fontSize: 24, fontWeight: 700, color: '#7894ff' }}>
              {stats[t.key as keyof Stats] ?? '—'}
            </div>
          </button>
        ))}
      </div>
    </PageLayout>
  )
}
