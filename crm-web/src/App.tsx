import { useEffect, useMemo, useState } from 'react'
import './App.css'

function App() {
  const modules = useMemo(
    () => [
      { key: 'companies', title: 'Организации' },
      { key: 'counterparties', title: 'Контрагенты' },
      { key: 'items', title: 'Номенклатура' },
      { key: 'warehouses', title: 'Склады' },
      { key: 'stock', title: 'Остатки' },
      { key: 'orders', title: 'Заказы' },
      { key: 'invoices', title: 'Счета' },
      { key: 'upds', title: 'УПД' },
      { key: 'payments', title: 'Платежи' },
      { key: 'reports', title: 'Отчеты' },
    ],
    [],
  )

  const [apiStatus, setApiStatus] = useState<'loading' | 'ok' | 'error'>('loading')

  useEffect(() => {
    let cancelled = false

    async function ping() {
      try {
        const res = await fetch('/api/v1/health')
        if (!res.ok) throw new Error('Health check failed')
        if (!cancelled) setApiStatus('ok')
      } catch {
        if (!cancelled) setApiStatus('error')
      }
    }

    ping()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="appShell">
      <header className="topBar">
        <div className="topBarTitle">CRM System</div>
        <div className="topBarRight">
          <div className={`statusPill status-${apiStatus}`}>
            API: {apiStatus === 'loading' ? 'checking…' : apiStatus}
          </div>
        </div>
      </header>

      <main className="content">
        <h1 className="pageTitle">Панель управления</h1>
        <p className="pageSubtitle">
          Это базовая стартовая страница. Дальше сюда добавим маршрутизацию и реальные таблицы.
        </p>

        <section className="grid">
          {modules.map((m) => (
            <button
              key={m.key}
              className="tile"
              type="button"
              onClick={() => alert(`Раздел: ${m.title} (пока без маршрутов)`)}
            >
              <div className="tileTitle">{m.title}</div>
              <div className="tileDesc">Скоро будет</div>
            </button>
          ))}
        </section>
      </main>
    </div>
  )
}

export default App
