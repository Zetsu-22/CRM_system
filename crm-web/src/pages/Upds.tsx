import { useEffect, useState } from 'react'
import { Trash2, ClipboardList } from 'lucide-react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import '../components/common.css'

interface Company { id: string; name: string }
interface Counterparty { id: string; name: string }
interface Item { id: string; name: string; vatRate: string }

interface UpdLine {
  id: string; itemId: string; nameSnapshot: string; vatRateSnapshot: string
  qty: number; unitPrice: number; net: number; vat: number; gross: number
}
interface Upd {
  id: string; number: string | null; date: string
  companyId: string; counterpartyId: string; orderId: string | null; invoiceId: string | null
  function: string; status: string; shippingDate: string | null
  totalNet: number; totalVat: number; totalGross: number
  comment: string | null; lines: UpdLine[]
}

const FUNC_LABELS: Record<string, string> = { ShippingAndInvoice: 'Отгрузка и счёт', ShippingOnly: 'Только отгрузка', InvoiceOnly: 'Только счёт' }
const STATUS_LABELS: Record<string, string> = { Draft: 'Черновик', Registered: 'Зарегистрирован', Completed: 'Завершён', Cancelled: 'Отменён' }
const STATUS_BADGE: Record<string, string> = { Draft: 'badge-gray', Registered: 'badge-blue', Completed: 'badge-green', Cancelled: 'badge-red' }
const VAT_VALUES: Record<string, number> = { Vat0: 0, Vat5: 5, Vat7: 7, Vat10: 10, Vat22: 22 }
const fmt = (n: number) => n.toLocaleString('ru-RU', { minimumFractionDigits: 2 }) + ' ₽'
const emptyLine = () => ({ itemId: '', nameSnapshot: '', vatRateSnapshot: 'Vat0', qty: 1, unitPrice: 0, discountType: 'None', discountValue: 0, warehouseId: null })

export function UpdsPage() {
  const { profile } = useAuth()
  const [upds, setUpds] = useState<Upd[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [counterparties, setCounterparties] = useState<Counterparty[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | 'view' | null>(null)
  const [selected, setSelected] = useState<Upd | null>(null)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState({
    number: '', date: new Date().toISOString().slice(0, 10),
    companyId: '', counterpartyId: '', orderId: '', invoiceId: '',
    function: 'ShippingAndInvoice', status: 'Draft', shippingDate: '', comment: '',
    lines: [emptyLine()]
  })

  const hasFullAccess = !!(profile?.isCompanyOwner || profile?.isBasicProfile)
  const canView = !!(hasFullAccess || profile?.isAccessFinancialJournal)
  const canEdit = !!(hasFullAccess || profile?.isAccessFinancialJournalEdit)

  const load = () => {
    setLoading(true)
    Promise.all([api.get<Upd[]>('/upds'), api.get<Company[]>('/companies'), api.get<Counterparty[]>('/counterparties'), api.get<Item[]>('/items')])
      .then(([u, c, cp, it]) => { setUpds(u); setCompanies(c); setCounterparties(cp); setItems(it) })
      .catch(() => toast('error', 'Ошибка загрузки')).finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const openCreate = () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для создания УПД'); return }
    setForm({ number: '', date: new Date().toISOString().slice(0, 10), companyId: companies[0]?.id ?? '', counterpartyId: counterparties[0]?.id ?? '', orderId: '', invoiceId: '', function: 'ShippingAndInvoice', status: 'Draft', shippingDate: '', comment: '', lines: [emptyLine()] })
    setModal('create')
  }
  const openEdit = (u: Upd) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для редактирования УПД'); return }
    setSelected(u)
    setForm({ number: u.number ?? '', date: u.date.slice(0, 10), companyId: u.companyId, counterpartyId: u.counterpartyId, orderId: u.orderId ?? '', invoiceId: u.invoiceId ?? '', function: u.function, status: u.status, shippingDate: u.shippingDate?.slice(0, 10) ?? '', comment: u.comment ?? '', lines: u.lines.map(l => ({ itemId: l.itemId, nameSnapshot: l.nameSnapshot, vatRateSnapshot: l.vatRateSnapshot, qty: l.qty, unitPrice: l.unitPrice, discountType: 'None', discountValue: 0, warehouseId: null })) })
    setModal('edit')
  }
  const openView = (u: Upd) => { setSelected(u); setModal('view') }
  const openDelete = (u: Upd) => { setSelected(u); setModal('delete') }
  const closeModal = () => { setModal(null); setSelected(null) }

  const setLine = (i: number, k: string, v: string | number) => setForm(prev => {
    const lines = [...prev.lines]; lines[i] = { ...lines[i], [k]: v }
    if (k === 'itemId') { const item = items.find(x => x.id === v); if (item) lines[i] = { ...lines[i], nameSnapshot: item.name, vatRateSnapshot: item.vatRate } }
    return { ...prev, lines }
  })

  const buildBody = () => ({
    number: form.number || null, date: form.date, companyId: form.companyId, counterpartyId: form.counterpartyId,
    orderId: form.orderId || null, invoiceId: form.invoiceId || null,
    function: form.function, status: form.status, shippingDate: form.shippingDate || null,
    comment: form.comment || null,
    lines: form.lines.map(l => ({ itemId: l.itemId, nameSnapshot: l.nameSnapshot, vatRateSnapshot: l.vatRateSnapshot, qty: Number(l.qty), unitPrice: Number(l.unitPrice), discountType: l.discountType, discountValue: Number(l.discountValue), warehouseId: null }))
  })

  const handleSave = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для сохранения'); return }
    if (!form.companyId || !form.counterpartyId) { toast('error', 'Выберите организацию и контрагента'); return }
    if (form.lines.some(l => !l.itemId)) { toast('error', 'Выберите товар во всех строках'); return }
    setSaving(true)
    try {
      modal === 'create' ? await api.post('/upds', buildBody()) : await api.put(`/upds/${selected!.id}`, buildBody())
      toast('success', modal === 'create' ? 'УПД создан' : 'УПД обновлён'); load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') } finally { setSaving(false) }
  }

  const handleDelete = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSaving(true)
    try { await api.delete(`/upds/${selected!.id}`); toast('success', 'УПД удалён'); load(); closeModal() }
    catch (e: any) { toast('error', e.message ?? 'Ошибка') } finally { setSaving(false) }
  }

  return (
    <PageLayout title="УПД" actions={canEdit ? <button className="btn btn-primary" onClick={openCreate}>+ Создать УПД</button> : null}>
      <div className="tableWrap">
        {!canView ? <div className="tableEmpty">Нет доступа к УПД</div> : loading ? <div className="tableLoading">Загрузка…</div> : upds.length === 0 ? <div className="tableEmpty">Нет УПД</div> : (
          <table className="crmTable">
            <thead><tr><th>№</th><th>Дата</th><th>Функция</th><th>Статус</th><th>Без НДС</th><th>НДС</th><th>Итого</th><th></th></tr></thead>
            <tbody>
              {upds.map(u => (
                <tr key={u.id} style={{ cursor: 'pointer' }} onClick={() => openView(u)}>
                  <td>{u.number || '—'}</td>
                  <td>{u.date.slice(0, 10)}</td>
                  <td>{FUNC_LABELS[u.function] ?? u.function}</td>
                  <td><span className={`badge ${STATUS_BADGE[u.status] ?? 'badge-gray'}`}>{STATUS_LABELS[u.status] ?? u.status}</span></td>
                  <td>{fmt(u.totalNet)}</td>
                  <td>{fmt(u.totalVat)}</td>
                  <td><strong>{fmt(u.totalGross)}</strong></td>
                  <td onClick={e => e.stopPropagation()}>
                    {canEdit && (
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => openEdit(u)}>Изм.</button>
                        <button className="btn btn-danger btn-sm" onClick={() => openDelete(u)}>Удал.</button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {(modal === 'create' || modal === 'edit') && (
        <Modal title={modal === 'create' ? 'Новый УПД' : 'Редактировать УПД'} subtitle="Универсальный передаточный документ" onClose={closeModal} size="xl">
          <div className="formGrid">
            <div className="formGroup"><label className="formLabel">Номер</label><input className="formInput" value={form.number} onChange={e => setForm(p => ({ ...p, number: e.target.value }))} placeholder="Авто" /></div>
            <div className="formGroup"><label className="formLabel">Дата *</label><input className="formInput" type="date" value={form.date} onChange={e => setForm(p => ({ ...p, date: e.target.value }))} /></div>
            <div className="formGroup"><label className="formLabel">Организация *</label>
              <select className="formSelect" value={form.companyId} onChange={e => setForm(p => ({ ...p, companyId: e.target.value }))}>
                <option value="">— выберите —</option>{companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Контрагент *</label>
              <select className="formSelect" value={form.counterpartyId} onChange={e => setForm(p => ({ ...p, counterpartyId: e.target.value }))}>
                <option value="">— выберите —</option>{counterparties.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Функция</label>
              <select className="formSelect" value={form.function} onChange={e => setForm(p => ({ ...p, function: e.target.value }))}>
                {Object.entries(FUNC_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Статус</label>
              <select className="formSelect" value={form.status} onChange={e => setForm(p => ({ ...p, status: e.target.value }))}>
                {Object.entries(STATUS_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Дата отгрузки</label><input className="formInput" type="date" value={form.shippingDate} onChange={e => setForm(p => ({ ...p, shippingDate: e.target.value }))} /></div>
            <div className="formGroup"><label className="formLabel">Комментарий</label><input className="formInput" value={form.comment} onChange={e => setForm(p => ({ ...p, comment: e.target.value }))} /></div>
          </div>
          <div className="formSection">
            <div className="formSectionTitle" style={{ justifyContent: 'space-between' }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: 7 }}><ClipboardList size={14} />Строки УПД</span>
              <button className="btn btn-secondary btn-sm" onClick={() => setForm(p => ({ ...p, lines: [...p.lines, emptyLine()] }))}>+ Строка</button>
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
                      <td><button className="btn btn-danger btn-sm" onClick={() => setForm(p => ({ ...p, lines: p.lines.filter((_, idx) => idx !== i) }))} disabled={form.lines.length === 1}>✕</button></td>
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
        <Modal title={`УПД ${selected.number ?? '—'}`} subtitle={`${FUNC_LABELS[selected.function]} • ${STATUS_LABELS[selected.status]}`} onClose={closeModal} size="lg">
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px 24px', marginBottom: 16, fontSize: 14 }}>
            <div><span style={{ opacity: .6 }}>Дата:</span> {selected.date.slice(0, 10)}</div>
            <div><span style={{ opacity: .6 }}>Статус:</span> <span className={`badge ${STATUS_BADGE[selected.status]}`}>{STATUS_LABELS[selected.status]}</span></div>
            <div><span style={{ opacity: .6 }}>Функция:</span> {FUNC_LABELS[selected.function]}</div>
            {selected.shippingDate && <div><span style={{ opacity: .6 }}>Отгрузка:</span> {selected.shippingDate.slice(0, 10)}</div>}
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
        <Modal title="Удалить УПД" subtitle="Это действие нельзя отменить" onClose={closeModal} size="sm">
          <div className="confirmBox">
            <div className="confirmBoxIcon"><Trash2 size={22} /></div>
            <p>Удалить УПД <strong>{selected.number ?? selected.id.slice(0, 8)}</strong>?</p>
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
