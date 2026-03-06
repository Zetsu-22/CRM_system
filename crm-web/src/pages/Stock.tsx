import { useEffect, useState } from 'react'
import { PageLayout } from '../components/PageLayout'
import { toast } from '../components/Toast'
import { api } from '../api'
import '../components/common.css'

interface StockBalance {
  id: string
  warehouse: { name: string } | null
  item: { name: string; sku: string; unit: string } | null
  qtyOnHand: number
  qtyReserved: number
}

export function StockPage() {
  const [items, setItems] = useState<StockBalance[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')

  const load = () => {
    setLoading(true)
    api.get<StockBalance[]>('/warehouses/stock')
      .then(setItems)
      .catch(() => toast('error', 'Не удалось загрузить остатки'))
      .finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const filtered = items.filter(r => {
    const q = search.toLowerCase()
    return (
      r.item?.name.toLowerCase().includes(q) ||
      r.item?.sku.toLowerCase().includes(q) ||
      r.warehouse?.name.toLowerCase().includes(q)
    )
  })

  return (
    <PageLayout
      title="Остатки на складе"
      actions={
        <>
          <input
            className="formInput"
            placeholder="Поиск по товару или складу…"
            value={search}
            onChange={e => setSearch(e.target.value)}
            style={{ width: 260 }}
          />
          <button className="btn btn-secondary" onClick={load}>Обновить</button>
        </>
      }
    >
      <div className="tableWrap">
        {loading ? <div className="tableLoading">Загрузка…</div> : filtered.length === 0 ? (
          <div className="tableEmpty">{search ? 'Ничего не найдено' : 'Нет данных'}</div>
        ) : (
          <table className="crmTable">
            <thead><tr>
              <th>Склад</th><th>Артикул</th><th>Наименование</th><th>Ед.</th><th>Кол-во</th><th>Резерв</th><th>Доступно</th>
            </tr></thead>
            <tbody>
              {filtered.map(r => {
                const avail = r.qtyOnHand - r.qtyReserved
                return (
                  <tr key={r.id}>
                    <td>{r.warehouse?.name ?? '—'}</td>
                    <td><code style={{ fontSize: 12 }}>{r.item?.sku ?? '—'}</code></td>
                    <td>{r.item?.name ?? '—'}</td>
                    <td>{r.item?.unit ?? '—'}</td>
                    <td>{r.qtyOnHand}</td>
                    <td style={{ color: r.qtyReserved > 0 ? '#eab308' : undefined }}>{r.qtyReserved}</td>
                    <td>
                      <span className={`badge ${avail > 0 ? 'badge-green' : avail < 0 ? 'badge-red' : 'badge-gray'}`}>
                        {avail}
                      </span>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>
    </PageLayout>
  )
}
