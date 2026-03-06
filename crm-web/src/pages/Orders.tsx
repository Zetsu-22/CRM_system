import { useEffect, useState } from 'react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import '../components/common.css'

interface Company { id: string; name: string }
interface Counterparty { id: string; name: string }
interface Item { id: string; name: string; sku: string; unit: string; vatRate: string }

interface OrderLine {
  id: string
  itemId: string
  nameSnapshot: string
  vatRateSnapshot: string
  qty: number
  unitPrice: number
  discountType: string
  discountValue: number
  net: number
  vat: number
  gross: number
}

interface Order {
  id: string
  number: string | null
  date: string
  company: Company | null
  counterparty: Counterparty | null
  status: string
  totalNet: number
  totalVat: number
  totalGross: number
  comment: string | null
  lines: OrderLine[]
}

const STATUS_LABELS: Record<string, string> = { Draft: 'Черновик', Confirmed: 'Подтверждён', Fulfilled: 'Выполнен', Closed: 'Закрыт', Cancelled: 'Отменён' }
const STATUS_BADGE: Record<string, string> = { Draft: 'badge-gray', Confirmed: 'badge-green', Fulfilled: 'badge-blue', Closed: 'badge-blue', Cancelled: 'badge-red' }
const VAT_VALUES: Record<string, number> = { Vat0: 0, Vat5: 5, Vat7: 7, Vat10: 10, Vat22: 22 }

const fmt = (n: number) => n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₽'

const emptyLine = () => ({ itemId: '', nameSnapshot: '', vatRateSnapshot: 'Vat0', qty: 1, unitPrice: 0, discountType: 'None', discountValue: 0 })

export function OrdersPage() {
  const [orders, setOrders] = useState<Order[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [counterparties, setCounterparties] = useState<Counterparty[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | 'view' | null>(null)
  const [selected, setSelected] = useState<Order | null>(null)
  const [saving, setSaving] = useState(false)

  const [form, setForm] = useState({
    number: '', date: new Date().toISOString().slice(0, 10),
    companyId: '', counterpartyId: '', comment: '',
    lines: [emptyLine()]
  })

  const load = () => {
    setLoading(true)
    Promise.all([
      api.get<Order[]>('/orders'),
      api.get<Company[]>('/companies'),
      api.get<Counterparty[]>('/counterparties'),
      api.get<Item[]>('/items'),
    ]).then(([o, c, cp, it]) => {
      setOrders(o); setCompanies(c); setCounterparties(cp); setItems(it)
    }).catch(() => toast('error', 'Ошибка загрузки данных'))
      .finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const openCreate = () => {
    setForm({ number: '', date: new Date().toISOString().slice(0, 10), companyId: companies[0]?.id ?? '', counterpartyId: counterparties[0]?.id ?? '', comment: '', lines: [emptyLine()] })
    setModal('create')
  }

  const openEdit = (o: Order) => {
    if (o.status !== 'Draft') { toast('error', 'Редактирование доступно только для черновиков'); return }
    setSelected(o)
    setForm({
      number: o.number ?? '', date: o.date.slice(0, 10),
      companyId: o.company?.id ?? '', counterpartyId: o.counterparty?.id ?? '',
      comment: o.comment ?? '',
      lines: o.lines.map(l => ({ itemId: l.itemId, nameSnapshot: l.nameSnapshot, vatRateSnapshot: l.vatRateSnapshot, qty: l.qty, unitPrice: l.unitPrice, discountType: l.discountType, discountValue: l.discountValue }))
    })
    setModal('edit')
  }

  const openView = (o: Order) => { setSelected(o); setModal('view') }
  const openDelete = (o: Order) => { setSelected(o); setModal('delete') }
  const closeModal = () => { setModal(null); setSelected(null) }

  const setLine = (i: number, k: string, v: string | number) =>
    setForm(prev => {
      const lines = [...prev.lines]
      lines[i] = { ...lines[i], [k]: v }
      if (k === 'itemId') {
        const item = items.find(x => x.id === v)
        if (item) lines[i] = { ...lines[i], nameSnapshot: item.name, vatRateSnapshot: item.vatRate }
      }
      return { ...prev, lines }
    })

  const addLine = () => setForm(prev => ({ ...prev, lines: [...prev.lines, emptyLine()] }))
  const removeLine = (i: number) => setForm(prev => ({ ...prev, lines: prev.lines.filter((_, idx) => idx !== i) }))

  const buildBody = () => ({
    number: form.number || null,
    date: form.date,
    companyId: form.companyId,
    counterpartyId: form.counterpartyId,
    comment: form.comment || null,
    lines: form.lines.map(l => ({
      itemId: l.itemId,
      nameSnapshot: l.nameSnapshot,
      vatRateSnapshot: l.vatRateSnapshot,
      qty: Number(l.qty),
      unitPrice: Number(l.unitPrice),
      discountType: l.discountType,
      discountValue: Number(l.discountValue),
      warehouseId: null
    }))
  })

  const transition = async (id: string, action: 'confirm' | 'fulfill' | 'close' | 'cancel') => {
    const labels: Record<string, string> = { confirm: 'подтверждён', fulfill: 'выполнен', close: 'закрыт', cancel: 'отменён' }
    try {
      await api.post(`/orders/${id}/${action}`, {})
      toast('success', `Заказ ${labels[action]}`)
      load()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
  }

  const handleSave = async () => {
    if (!form.companyId) { toast('error', 'Выберите организацию'); return }
    if (!form.counterpartyId) { toast('error', 'Выберите контрагента'); return }
    if (form.lines.some(l => !l.itemId)) { toast('error', 'Выберите товар во всех строках'); return }
    setSaving(true)
    try {
      if (modal === 'create') {
        await api.post('/orders', buildBody())
        toast('success', 'Заказ создан')
      } else {
        await api.put(`/orders/${selected!.id}`, buildBody())
        toast('success', 'Заказ обновлён')
      }
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  const handleDelete = async () => {
    setSaving(true)
    try {
      await api.delete(`/orders/${selected!.id}`)
      toast('success', 'Заказ удалён')
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  return (
    <PageLayout title="Заказы" actions={
      <button className="btn btn-primary" onClick={openCreate}>+ Создать заказ</button>
    }>
      <div className="tableWrap">
        {loading ? <div className="tableLoading">Загрузка…</div> : orders.length === 0 ? (
          <div className="tableEmpty">Нет заказов</div>
        ) : (
          <table className="crmTable">
            <thead><tr>
              <th>№</th><th>Дата</th><th>Организация</th><th>Контрагент</th><th>Статус</th>
              <th>Сумма</th><th>НДС</th><th>Итого</th><th></th>
            </tr></thead>
            <tbody>
              {orders.map(o => (
                <tr key={o.id} style={{ cursor: 'pointer' }} onClick={() => openView(o)}>
                  <td>{o.number || '—'}</td>
                  <td>{o.date.slice(0, 10)}</td>
                  <td>{o.company?.name ?? '—'}</td>
                  <td>{o.counterparty?.name ?? '—'}</td>
                  <td><span className={`badge ${STATUS_BADGE[o.status] ?? 'badge-gray'}`}>{STATUS_LABELS[o.status] ?? o.status}</span></td>
                  <td>{fmt(o.totalNet)}</td>
                  <td>{fmt(o.totalVat)}</td>
                  <td><strong>{fmt(o.totalGross)}</strong></td>
                  <td onClick={e => e.stopPropagation()}>
                    <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                      {o.status === 'Draft' && <>
                        <button className="btn btn-secondary btn-sm" onClick={() => openEdit(o)}>Изм.</button>
                        <button className="btn btn-primary btn-sm" onClick={() => transition(o.id, 'confirm')}>Подтв.</button>
                        <button className="btn btn-danger btn-sm" onClick={() => transition(o.id, 'cancel')}>Откл.</button>
                      </>}
                      {o.status === 'Confirmed' && <>
                        <button className="btn btn-primary btn-sm" onClick={() => transition(o.id, 'fulfill')}>Выполнить</button>
                        <button className="btn btn-secondary btn-sm" onClick={() => transition(o.id, 'close')}>Закрыть</button>
                        <button className="btn btn-danger btn-sm" onClick={() => transition(o.id, 'cancel')}>Откл.</button>
                      </>}
                      {o.status === 'Fulfilled' && <>
                        <button className="btn btn-secondary btn-sm" onClick={() => transition(o.id, 'close')}>Закрыть</button>
                      </>}
                      {(o.status === 'Closed' || o.status === 'Cancelled') && null}
                      <button className="btn btn-danger btn-sm" onClick={() => openDelete(o)}>Удал.</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {(modal === 'create' || modal === 'edit') && (
        <Modal title={modal === 'create' ? 'Новый заказ' : 'Редактировать заказ'} onClose={closeModal} width={860}>
          <div className="formGrid">
            <div className="formGroup"><label className="formLabel">Номер</label>
              <input className="formInput" value={form.number} onChange={e => setForm(p => ({ ...p, number: e.target.value }))} placeholder="Авто" /></div>
            <div className="formGroup"><label className="formLabel">Дата</label>
              <input className="formInput" type="date" value={form.date} onChange={e => setForm(p => ({ ...p, date: e.target.value }))} /></div>
            <div className="formGroup"><label className="formLabel">Организация *</label>
              <select className="formSelect" value={form.companyId} onChange={e => setForm(p => ({ ...p, companyId: e.target.value }))}>
                <option value="">— выберите —</option>
                {companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Контрагент *</label>
              <select className="formSelect" value={form.counterpartyId} onChange={e => setForm(p => ({ ...p, counterpartyId: e.target.value }))}>
                <option value="">— выберите —</option>
                {counterparties.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Комментарий</label>
              <input className="formInput" value={form.comment} onChange={e => setForm(p => ({ ...p, comment: e.target.value }))} /></div>
          </div>

          <div style={{ marginTop: 20 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 10 }}>
              <span style={{ fontWeight: 600 }}>Строки заказа</span>
              <button className="btn btn-secondary btn-sm" onClick={addLine}>+ Строка</button>
            </div>
            <div className="tableWrap">
              <table className="crmTable">
                <thead><tr><th>Товар</th><th>Название</th><th>Кол-во</th><th>Цена</th><th>НДС</th><th></th></tr></thead>
                <tbody>
                  {form.lines.map((l, i) => (
                    <tr key={i}>
                      <td>
                        <select className="formSelect" style={{ minWidth: 160 }} value={l.itemId} onChange={e => setLine(i, 'itemId', e.target.value)}>
                          <option value="">— товар —</option>
                          {items.map(it => <option key={it.id} value={it.id}>{it.name}</option>)}
                        </select>
                      </td>
                      <td><input className="formInput" value={l.nameSnapshot} onChange={e => setLine(i, 'nameSnapshot', e.target.value)} /></td>
                      <td><input className="formInput" type="number" style={{ width: 80 }} value={l.qty} onChange={e => setLine(i, 'qty', e.target.value)} /></td>
                      <td><input className="formInput" type="number" style={{ width: 100 }} value={l.unitPrice} onChange={e => setLine(i, 'unitPrice', e.target.value)} /></td>
                      <td>
                        <select className="formSelect" style={{ width: 80 }} value={l.vatRateSnapshot} onChange={e => setLine(i, 'vatRateSnapshot', e.target.value)}>
                          {Object.entries(VAT_VALUES).map(([k, v]) => <option key={k} value={k}>{v}%</option>)}
                        </select>
                      </td>
                      <td><button className="btn btn-danger btn-sm" onClick={() => removeLine(i)} disabled={form.lines.length === 1}>✕</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="formActions">
            <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}

      {modal === 'view' && selected && (
        <Modal title={`Заказ ${selected.number ?? '—'}`} onClose={closeModal} width={700}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px 24px', marginBottom: 16, fontSize: 14 }}>
            <div><span style={{ opacity: .6 }}>Дата:</span> {selected.date.slice(0, 10)}</div>
            <div><span style={{ opacity: .6 }}>Статус:</span> <span className={`badge ${STATUS_BADGE[selected.status]}`}>{STATUS_LABELS[selected.status]}</span></div>
            <div><span style={{ opacity: .6 }}>Организация:</span> {selected.company?.name}</div>
            <div><span style={{ opacity: .6 }}>Контрагент:</span> {selected.counterparty?.name}</div>
            {selected.comment && <div style={{ gridColumn: '1/-1' }}><span style={{ opacity: .6 }}>Комментарий:</span> {selected.comment}</div>}
          </div>
          <div className="tableWrap">
            <table className="crmTable">
              <thead><tr><th>Наименование</th><th>Кол-во</th><th>Цена</th><th>НДС</th><th>Сумма</th></tr></thead>
              <tbody>
                {selected.lines.map(l => (
                  <tr key={l.id}>
                    <td>{l.nameSnapshot}</td>
                    <td>{l.qty}</td>
                    <td>{fmt(l.unitPrice)}</td>
                    <td>{fmt(l.vat)}</td>
                    <td>{fmt(l.gross)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div style={{ textAlign: 'right', marginTop: 12, display: 'flex', gap: 20, justifyContent: 'flex-end', fontSize: 14 }}>
            <span style={{ opacity: .7 }}>Без НДС: {fmt(selected.totalNet)}</span>
            <span style={{ opacity: .7 }}>НДС: {fmt(selected.totalVat)}</span>
            <strong>Итого: {fmt(selected.totalGross)}</strong>
          </div>
          <div className="formActions">
            {selected.status === 'Draft' && <button className="btn btn-secondary" onClick={() => { closeModal(); setTimeout(() => openEdit(selected!), 50) }}>Редактировать</button>}
            {selected.status === 'Draft' && <button className="btn btn-primary" onClick={() => { transition(selected!.id, 'confirm'); closeModal() }}>Подтвердить</button>}
            {selected.status === 'Confirmed' && <button className="btn btn-primary" onClick={() => { transition(selected!.id, 'fulfill'); closeModal() }}>Выполнить</button>}
            {(selected.status === 'Confirmed' || selected.status === 'Fulfilled') && <button className="btn btn-secondary" onClick={() => { transition(selected!.id, 'close'); closeModal() }}>Закрыть</button>}
            {(selected.status === 'Draft' || selected.status === 'Confirmed') && <button className="btn btn-danger" onClick={() => { transition(selected!.id, 'cancel'); closeModal() }}>Отменить</button>}
            <button className="btn btn-secondary" onClick={closeModal}>Закрыть</button>
          </div>
        </Modal>
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить заказ?" onClose={closeModal} width={400}>
          <div className="confirmBox">
            <p>Удалить заказ <strong>{selected.number ?? selected.id.slice(0, 8)}</strong>?</p>
            <div className="confirmActions">
              <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
              <button className="btn btn-danger" onClick={handleDelete} disabled={saving}>{saving ? 'Удаление…' : 'Удалить'}</button>
            </div>
          </div>
        </Modal>
      )}
    </PageLayout>
  )
}
