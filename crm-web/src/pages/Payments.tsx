import { useEffect, useState } from 'react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import '../components/common.css'

interface Company { id: string; name: string }
interface Counterparty { id: string; name: string }
interface Payment {
  id: string; type: string; companyId: string; counterpartyId: string | null
  date: string; amount: number; paymentMethod: string; bankReference: string | null
  externalId: string | null; status: string
}

const TYPE_LABELS: Record<string, string> = { Incoming: 'Входящий', Outgoing: 'Исходящий' }
const TYPE_BADGE: Record<string, string> = { Incoming: 'badge-green', Outgoing: 'badge-yellow' }
const METHOD_LABELS: Record<string, string> = { Bank: 'Банк', Cash: 'Наличные', Card: 'Карта' }
const STATUS_LABELS: Record<string, string> = { Draft: 'Черновик', Posted: 'Проведён', Cancelled: 'Отменён' }
const STATUS_BADGE: Record<string, string> = { Draft: 'badge-gray', Posted: 'badge-green', Cancelled: 'badge-red' }
const fmt = (n: number) => n.toLocaleString('ru-RU', { minimumFractionDigits: 2 }) + ' ₽'

const empty = () => ({ type: 'Incoming', companyId: '', counterpartyId: '', date: new Date().toISOString().slice(0, 10), amount: '', paymentMethod: 'Bank', bankReference: '', externalId: '', status: 'Draft' })

export function PaymentsPage() {
  const { profile } = useAuth()
  const [payments, setPayments] = useState<Payment[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [counterparties, setCounterparties] = useState<Counterparty[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | null>(null)
  const [selected, setSelected] = useState<Payment | null>(null)
  const [form, setForm] = useState(empty())
  const [saving, setSaving] = useState(false)

  const hasFullAccess = !!(profile?.isCompanyOwner || profile?.isBasicProfile)
  const canView = !!(hasFullAccess || profile?.isAccessFinancialJournal)
  const canEdit = !!(hasFullAccess || profile?.isAccessFinancialJournalEdit)

  const load = () => {
    setLoading(true)
    Promise.all([api.get<Payment[]>('/payments'), api.get<Company[]>('/companies'), api.get<Counterparty[]>('/counterparties')])
      .then(([p, c, cp]) => { setPayments(p); setCompanies(c); setCounterparties(cp) })
      .catch(() => toast('error', 'Ошибка загрузки')).finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const openCreate = () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для создания платежа'); return }
    setForm({ ...empty(), companyId: companies[0]?.id ?? '', counterpartyId: counterparties[0]?.id ?? '' });
    setModal('create')
  }
  const openEdit = (p: Payment) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для редактирования платежа'); return }
    setSelected(p)
    setForm({ type: p.type, companyId: p.companyId, counterpartyId: p.counterpartyId ?? '', date: p.date.slice(0, 10), amount: String(p.amount), paymentMethod: p.paymentMethod, bankReference: p.bankReference ?? '', externalId: p.externalId ?? '', status: p.status })
    setModal('edit')
  }
  const openDelete = (p: Payment) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления платежа'); return }
    setSelected(p); setModal('delete')
  }
  const closeModal = () => { setModal(null); setSelected(null) }

  const f = (k: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm(prev => ({ ...prev, [k]: e.target.value }))

  const buildBody = () => ({
    type: form.type, companyId: form.companyId,
    counterpartyId: form.counterpartyId || null,
    date: form.date, amount: Number(form.amount),
    paymentMethod: form.paymentMethod,
    bankReference: form.bankReference || null,
    externalId: form.externalId || null,
    status: form.status
  })

  const handleSave = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для сохранения'); return }
    if (!form.companyId) { toast('error', 'Выберите организацию'); return }
    if (!form.date) { toast('error', 'Выберите дату'); return }
    if (!form.amount) { toast('error', 'Введите сумму'); return }
    setSaving(true)
    try {
      modal === 'create' ? await api.post('/payments', buildBody()) : await api.put(`/payments/${selected!.id}`, buildBody())
      toast('success', modal === 'create' ? 'Платёж создан' : 'Платёж обновлён'); load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') } finally { setSaving(false) }
  }

  const handleDelete = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSaving(true)
    try { await api.delete(`/payments/${selected!.id}`); toast('success', 'Платёж удалён'); load(); closeModal() }
    catch (e: any) { toast('error', e.message ?? 'Ошибка') } finally { setSaving(false) }
  }

  const companyName = (id: string) => companies.find(c => c.id === id)?.name ?? '—'
  const cpName = (id: string | null) => id ? (counterparties.find(c => c.id === id)?.name ?? '—') : '—'

  return (
    <PageLayout title="Платежи" actions={canEdit ? <button className="btn btn-primary" onClick={openCreate}>+ Добавить</button> : null}>
      <div className="tableWrap">
        {!canView ? <div className="tableEmpty">Нет доступа к платежам</div> : loading ? <div className="tableLoading">Загрузка…</div> : payments.length === 0 ? <div className="tableEmpty">Нет платежей</div> : (
          <table className="crmTable">
            <thead><tr><th>Дата</th><th>Тип</th><th>Организация</th><th>Контрагент</th><th>Метод</th><th>Сумма</th><th>Статус</th><th></th></tr></thead>
            <tbody>
              {payments.map(p => (
                <tr key={p.id}>
                  <td>{p.date.slice(0, 10)}</td>
                  <td><span className={`badge ${TYPE_BADGE[p.type] ?? 'badge-gray'}`}>{TYPE_LABELS[p.type] ?? p.type}</span></td>
                  <td>{companyName(p.companyId)}</td>
                  <td>{cpName(p.counterpartyId)}</td>
                  <td>{METHOD_LABELS[p.paymentMethod] ?? p.paymentMethod}</td>
                  <td><strong>{fmt(p.amount)}</strong></td>
                  <td><span className={`badge ${STATUS_BADGE[p.status] ?? 'badge-gray'}`}>{STATUS_LABELS[p.status] ?? p.status}</span></td>
                  <td onClick={e => e.stopPropagation()}>
                    {canEdit && (
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => openEdit(p)}>Изм.</button>
                        <button className="btn btn-danger btn-sm" onClick={() => openDelete(p)}>Удал.</button>
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
        <Modal title={modal === 'create' ? 'Новый платёж' : 'Редактировать платёж'} onClose={closeModal} width={580}>
          <div className="formGrid">
            <div className="formGroup"><label className="formLabel">Тип</label>
              <select className="formSelect" value={form.type} onChange={f('type')}>
                {Object.entries(TYPE_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Дата</label>
              <input className="formInput" type="date" value={form.date} onChange={f('date')} /></div>
            <div className="formGroup"><label className="formLabel">Организация *</label>
              <select className="formSelect" value={form.companyId} onChange={f('companyId')}>
                <option value="">— выберите —</option>{companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Контрагент</label>
              <select className="formSelect" value={form.counterpartyId} onChange={f('counterpartyId')}>
                <option value="">— не указан —</option>{counterparties.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Сумма *</label>
              <input className="formInput" type="number" value={form.amount} onChange={f('amount')} /></div>
            <div className="formGroup"><label className="formLabel">Метод оплаты</label>
              <select className="formSelect" value={form.paymentMethod} onChange={f('paymentMethod')}>
                {Object.entries(METHOD_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Статус</label>
              <select className="formSelect" value={form.status} onChange={f('status')}>
                {Object.entries(STATUS_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Номер п/п</label>
              <input className="formInput" value={form.bankReference} onChange={f('bankReference')} /></div>
            <div className="formGroup full"><label className="formLabel">Внешний ID</label>
              <input className="formInput" value={form.externalId} onChange={f('externalId')} /></div>
          </div>
          <div className="formActions">
            <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить платёж?" onClose={closeModal} width={400}>
          <div className="confirmBox">
            <p>Удалить платёж от <strong>{selected.date.slice(0, 10)}</strong> на <strong>{fmt(selected.amount)}</strong>?</p>
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
