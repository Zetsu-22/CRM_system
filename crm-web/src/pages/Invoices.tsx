import { useEffect, useState } from 'react'
import { Trash2, FileText } from 'lucide-react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import '../components/common.css'

interface Company { id: string; name: string }
interface Counterparty { id: string; name: string }
interface Item { id: string; name: string; vatRate: string }

interface InvoiceLine {
  id: string; itemId: string; nameSnapshot: string; vatRateSnapshot: string
  qty: number; unitPrice: number; net: number; vat: number; gross: number
}
interface Invoice {
  id: string; number: string | null; date: string
  companyId: string; counterpartyId: string; orderId: string | null
  status: string; totalNet: number; totalVat: number; totalGross: number
  paidAmount: number; comment: string | null; lines: InvoiceLine[]
}

const STATUS_LABELS: Record<string, string> = { Draft: 'Черновик', Issued: 'Выставлен', Closed: 'Закрыт', Cancelled: 'Отменён' }
const STATUS_BADGE: Record<string, string> = { Draft: 'badge-gray', Issued: 'badge-blue', Closed: 'badge-green', Cancelled: 'badge-red' }
const VAT_VALUES: Record<string, number> = { Vat0: 0, Vat5: 5, Vat7: 7, Vat10: 10, Vat22: 22 }
const fmt = (n: number) => n.toLocaleString('ru-RU', { minimumFractionDigits: 2 }) + ' ₽'
const emptyLine = () => ({ itemId: '', nameSnapshot: '', vatRateSnapshot: 'Vat0', qty: 1, unitPrice: 0, discountType: 'None', discountValue: 0 })

export function InvoicesPage() {
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [counterparties, setCounterparties] = useState<Counterparty[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | 'view' | null>(null)
  const [selected, setSelected] = useState<Invoice | null>(null)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState({ number: '', date: new Date().toISOString().slice(0, 10), companyId: '', counterpartyId: '', orderId: '', status: 'Draft', comment: '', lines: [emptyLine()] })

  const load = () => {
    setLoading(true)
    Promise.all([api.get<Invoice[]>('/invoices'), api.get<Company[]>('/companies'), api.get<Counterparty[]>('/counterparties'), api.get<Item[]>('/items')])
      .then(([inv, c, cp, it]) => { setInvoices(inv); setCompanies(c); setCounterparties(cp); setItems(it) })
      .catch(() => toast('error', 'Ошибка загрузки')).finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const openCreate = () => { setForm({ number: '', date: new Date().toISOString().slice(0, 10), companyId: companies[0]?.id ?? '', counterpartyId: counterparties[0]?.id ?? '', orderId: '', status: 'Draft', comment: '', lines: [emptyLine()] }); setModal('create') }
  const openEdit = (inv: Invoice) => { setSelected(inv); setForm({ number: inv.number ?? '', date: inv.date.slice(0, 10), companyId: inv.companyId, counterpartyId: inv.counterpartyId, orderId: inv.orderId ?? '', status: inv.status, comment: inv.comment ?? '', lines: inv.lines.map(l => ({ itemId: l.itemId, nameSnapshot: l.nameSnapshot, vatRateSnapshot: l.vatRateSnapshot, qty: l.qty, unitPrice: l.unitPrice, discountType: 'None', discountValue: 0 })) }); setModal('edit') }
  const openView = (inv: Invoice) => { setSelected(inv); setModal('view') }
  const openDelete = (inv: Invoice) => { setSelected(inv); setModal('delete') }
  const closeModal = () => { setModal(null); setSelected(null) }

  const setLine = (i: number, k: string, v: string | number) => setForm(prev => {
    const lines = [...prev.lines]; lines[i] = { ...lines[i], [k]: v }
    if (k === 'itemId') { const item = items.find(x => x.id === v); if (item) lines[i] = { ...lines[i], nameSnapshot: item.name, vatRateSnapshot: item.vatRate } }
    return { ...prev, lines }
  })
  const addLine = () => setForm(prev => ({ ...prev, lines: [...prev.lines, emptyLine()] }))
  const removeLine = (i: number) => setForm(prev => ({ ...prev, lines: prev.lines.filter((_, idx) => idx !== i) }))

  const buildBody = () => ({ number: form.number || null, date: form.date, companyId: form.companyId, counterpartyId: form.counterpartyId, orderId: form.orderId || null, status: form.status, comment: form.comment || null, lines: form.lines.map(l => ({ itemId: l.itemId, nameSnapshot: l.nameSnapshot, vatRateSnapshot: l.vatRateSnapshot, qty: Number(l.qty), unitPrice: Number(l.unitPrice), discountType: l.discountType, discountValue: Number(l.discountValue) })) })

  const handleSave = async () => {
    if (!form.companyId || !form.counterpartyId) { toast('error', 'Выберите организацию и контрагента'); return }
    if (form.lines.some(l => !l.itemId)) { toast('error', 'Выберите товар во всех строках'); return }
    setSaving(true)
    try {
      modal === 'create' ? await api.post('/invoices', buildBody()) : await api.put(`/invoices/${selected!.id}`, buildBody())
      toast('success', modal === 'create' ? 'Счёт создан' : 'Счёт обновлён'); load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') } finally { setSaving(false) }
  }

  const handleDelete = async () => {
    setSaving(true)
    try { await api.delete(`/invoices/${selected!.id}`); toast('success', 'Счёт удалён'); load(); closeModal() }
    catch (e: any) { toast('error', e.message ?? 'Ошибка') } finally { setSaving(false) }
  }

  return (
    <PageLayout title="Счета" actions={<button className="btn btn-primary" onClick={openCreate}>+ Создать счёт</button>}>
      <div className="tableWrap">
        {loading ? <div className="tableLoading">Загрузка…</div> : invoices.length === 0 ? <div className="tableEmpty">Нет счетов</div> : (
          <table className="crmTable">
            <thead><tr><th>№</th><th>Дата</th><th>Статус</th><th>Сумма</th><th>НДС</th><th>Итого</th><th>Оплачено</th><th></th></tr></thead>
            <tbody>
              {invoices.map(inv => (
                <tr key={inv.id} style={{ cursor: 'pointer' }} onClick={() => openView(inv)}>
                  <td>{inv.number || '—'}</td>
                  <td>{inv.date.slice(0, 10)}</td>
                  <td><span className={`badge ${STATUS_BADGE[inv.status] ?? 'badge-gray'}`}>{STATUS_LABELS[inv.status] ?? inv.status}</span></td>
                  <td>{fmt(inv.totalNet)}</td>
                  <td>{fmt(inv.totalVat)}</td>
                  <td><strong>{fmt(inv.totalGross)}</strong></td>
                  <td style={{ color: inv.paidAmount >= inv.totalGross ? '#2ac36a' : '#eab308' }}>{fmt(inv.paidAmount)}</td>
                  <td onClick={e => e.stopPropagation()}><div style={{ display: 'flex', gap: 6 }}>
                    <button className="btn btn-secondary btn-sm" onClick={() => openEdit(inv)}>Изм.</button>
                    <button className="btn btn-danger btn-sm" onClick={() => openDelete(inv)}>Удал.</button>
                  </div></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {(modal === 'create' || modal === 'edit') && (
        <Modal title={modal === 'create' ? 'Новый счёт' : 'Редактировать счёт'} subtitle="Счёт на оплату покупателю" onClose={closeModal} size="xl">
          <div className="formGrid">
            <div className="formGroup"><label className="formLabel">Номер</label><input className="formInput" value={form.number} onChange={e => setForm(p => ({ ...p, number: e.target.value }))} placeholder="Авто" /></div>
            <div className="formGroup"><label className="formLabel">Дата</label><input className="formInput" type="date" value={form.date} onChange={e => setForm(p => ({ ...p, date: e.target.value }))} /></div>
            <div className="formGroup"><label className="formLabel">Организация *</label>
              <select className="formSelect" value={form.companyId} onChange={e => setForm(p => ({ ...p, companyId: e.target.value }))}>
                <option value="">— выберите —</option>{companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Контрагент *</label>
              <select className="formSelect" value={form.counterpartyId} onChange={e => setForm(p => ({ ...p, counterpartyId: e.target.value }))}>
                <option value="">— выберите —</option>{counterparties.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Статус</label>
              <select className="formSelect" value={form.status} onChange={e => setForm(p => ({ ...p, status: e.target.value }))}>
                {Object.entries(STATUS_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Комментарий</label><input className="formInput" value={form.comment} onChange={e => setForm(p => ({ ...p, comment: e.target.value }))} /></div>
          </div>
          <div className="formSection">
            <div className="formSectionTitle" style={{ justifyContent: 'space-between' }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: 7 }}><FileText size={14} />Строки счёта</span>
              <button className="btn btn-secondary btn-sm" onClick={addLine}>+ Строка</button>
            </div>
            <div className="tableWrap">
              <table className="crmTable">
                <thead><tr><th>Товар</th><th>Название</th><th>Кол-во</th><th>Цена</th><th>НДС</th><th></th></tr></thead>
                <tbody>
                  {form.lines.map((l, i) => (
                    <tr key={i}>
                      <td><select className="formSelect" style={{ minWidth: 160 }} value={l.itemId} onChange={e => setLine(i, 'itemId', e.target.value)}>
                        <option value="">— товар —</option>{items.map(it => <option key={it.id} value={it.id}>{it.name}</option>)}
                      </select></td>
                      <td><input className="formInput" value={l.nameSnapshot} onChange={e => setLine(i, 'nameSnapshot', e.target.value)} /></td>
                      <td><input className="formInput" type="number" style={{ width: 80 }} value={l.qty} onChange={e => setLine(i, 'qty', e.target.value)} /></td>
                      <td><input className="formInput" type="number" style={{ width: 100 }} value={l.unitPrice} onChange={e => setLine(i, 'unitPrice', e.target.value)} /></td>
                      <td><select className="formSelect" style={{ width: 80 }} value={l.vatRateSnapshot} onChange={e => setLine(i, 'vatRateSnapshot', e.target.value)}>
                        {Object.entries(VAT_VALUES).map(([k, v]) => <option key={k} value={k}>{v}%</option>)}
                      </select></td>
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
        <Modal title={`Счёт ${selected.number ?? '—'}`} subtitle={`${STATUS_LABELS[selected.status]}`} onClose={closeModal} size="lg">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px 24px', marginBottom: 16, fontSize: 14 }}>
            <div><span style={{ opacity: .6 }}>Дата:</span> {selected.date.slice(0, 10)}</div>
            <div><span style={{ opacity: .6 }}>Статус:</span> <span className={`badge ${STATUS_BADGE[selected.status]}`}>{STATUS_LABELS[selected.status]}</span></div>
          </div>
          <div className="tableWrap">
            <table className="crmTable">
              <thead><tr><th>Наименование</th><th>Кол-во</th><th>Цена</th><th>НДС</th><th>Сумма</th></tr></thead>
              <tbody>{selected.lines.map(l => <tr key={l.id}><td>{l.nameSnapshot}</td><td>{l.qty}</td><td>{fmt(l.unitPrice)}</td><td>{fmt(l.vat)}</td><td>{fmt(l.gross)}</td></tr>)}</tbody>
            </table>
          </div>
          <div style={{ textAlign: 'right', marginTop: 12, display: 'flex', gap: 20, justifyContent: 'flex-end', fontSize: 14 }}>
            <span style={{ opacity: .7 }}>Без НДС: {fmt(selected.totalNet)}</span>
            <span style={{ opacity: .7 }}>НДС: {fmt(selected.totalVat)}</span>
            <strong>Итого: {fmt(selected.totalGross)}</strong>
          </div>
          <div className="formActions"><button className="btn btn-secondary" onClick={closeModal}>Закрыть</button></div>
        </Modal>
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить счёт" subtitle="Это действие нельзя отменить" onClose={closeModal} size="sm">
          <div className="confirmBox">
            <div className="confirmBoxIcon"><Trash2 size={22} /></div>
            <p>Удалить счёт <strong>{selected.number ?? selected.id.slice(0, 8)}</strong>?</p>
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
