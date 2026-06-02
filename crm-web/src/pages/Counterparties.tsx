import { useEffect, useState } from 'react'
import { Trash2 } from 'lucide-react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import { useOrganizations } from '../OrganizationContext'
import '../components/common.css'

interface Counterparty {
  id: string
  organizationId: string
  name: string
  fullName: string | null
  type: string
  status: string
  inn: string | null
  kpp: string | null
  ogrn: string | null
  paymentTermsDays: number | null
}

const empty = (orgId: string) => ({
  organizationId: orgId,
  name: '', fullName: '', type: 'Customer', status: 'Active', inn: '', kpp: '', ogrn: '', paymentTermsDays: '' as string | number,
})

const typeLabel: Record<string, string> = { Customer: 'Покупатель', Supplier: 'Поставщик', Both: 'Оба' }
const statusBadge = (s: string) => s === 'Active' ? 'badge-green' : 'badge-red'
const statusLabel: Record<string, string> = { Active: 'Активен', Blocked: 'Заблокирован' }

export function CounterpartiesPage() {
  const { profile } = useAuth()
  const { currentOrganization, filterString } = useOrganizations()
  const [items, setItems] = useState<Counterparty[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | null>(null)
  const [selected, setSelected] = useState<Counterparty | null>(null)
  const [form, setForm] = useState(empty(''))
  const [saving, setSaving] = useState(false)

  const hasFullAccess = !!(profile?.isCompanyOwner || profile?.isBasicProfile)
  const canView = !!(hasFullAccess || profile?.isAccessContractors)
  const canEdit = !!(hasFullAccess || profile?.isAccessContractorsEdit)

  const load = () => {
    setLoading(true)
    const qs = filterString ? `?organizationIds=${filterString}` : ''
    api.get<Counterparty[]>(`/counterparties${qs}`)
      .then(setItems)
      .catch(() => toast('error', 'Не удалось загрузить контрагентов'))
      .finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [filterString]) // eslint-disable-line react-hooks/exhaustive-deps

  const openCreate = () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для создания'); return }
    if (!currentOrganization) { toast('error', 'Выберите организацию в шапке'); return }
    setForm(empty(currentOrganization.id)); setModal('create')
  }
  const openEdit = (c: Counterparty) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для редактирования'); return }
    setSelected(c)
    setForm({
      organizationId: c.organizationId,
      name: c.name, fullName: c.fullName ?? '', type: c.type, status: c.status,
      inn: c.inn ?? '', kpp: c.kpp ?? '', ogrn: c.ogrn ?? '',
      paymentTermsDays: c.paymentTermsDays ?? '',
    })
    setModal('edit')
  }
  const openDelete = (c: Counterparty) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSelected(c); setModal('delete')
  }
  const closeModal = () => { setModal(null); setSelected(null) }

  const f = (k: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm(prev => ({ ...prev, [k]: e.target.value }))

  const handleSave = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для сохранения'); return }
    if (!form.name.trim()) { toast('error', 'Название обязательно'); return }
    setSaving(true)
    const body = { ...form, paymentTermsDays: form.paymentTermsDays === '' ? null : Number(form.paymentTermsDays) }
    try {
      if (modal === 'create') {
        await api.post('/counterparties', body)
        toast('success', 'Контрагент создан')
      } else {
        await api.put(`/counterparties/${selected!.id}`, body)
        toast('success', 'Контрагент обновлён')
      }
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  const handleDelete = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSaving(true)
    try {
      await api.delete(`/counterparties/${selected!.id}`)
      toast('success', 'Контрагент удалён')
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  return (
    <PageLayout title="Контрагенты" actions={
      canEdit ? <button className="btn btn-primary" onClick={openCreate}>+ Добавить</button> : null
    }>
      <div className="tableWrap">
        {!canView ? <div className="tableEmpty">Нет доступа к контрагентам</div> : loading ? <div className="tableLoading">Загрузка…</div> : items.length === 0 ? (
          <div className="tableEmpty">Нет контрагентов</div>
        ) : (
          <table className="crmTable">
            <thead><tr>
              <th>Название</th><th>Тип</th><th>Статус</th><th>ИНН</th><th>Срок оплаты</th><th></th>
            </tr></thead>
            <tbody>
              {items.map(c => (
                <tr key={c.id}>
                  <td><strong>{c.name}</strong><div style={{fontSize:12,opacity:.6}}>{c.fullName}</div></td>
                  <td>{typeLabel[c.type] ?? c.type}</td>
                  <td><span className={`badge ${statusBadge(c.status)}`}>{statusLabel[c.status] ?? c.status}</span></td>
                  <td>{c.inn || '—'}</td>
                  <td>{c.paymentTermsDays != null ? `${c.paymentTermsDays} дн.` : '—'}</td>
                  <td>
                    {canEdit && (
                      <div style={{display:'flex',gap:6}}>
                        <button className="btn btn-secondary btn-sm" onClick={() => openEdit(c)}>Изменить</button>
                        <button className="btn btn-danger btn-sm" onClick={() => openDelete(c)}>Удалить</button>
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
        <Modal title={modal === 'create' ? 'Новый контрагент' : 'Редактировать контрагента'} subtitle="Реквизиты и условия" onClose={closeModal} size="md">
          <div className="formGrid">
            <div className="formGroup full"><label className="formLabel">Название *</label>
              <input className="formInput" value={form.name} onChange={f('name')} /></div>
            <div className="formGroup full"><label className="formLabel">Полное наименование</label>
              <input className="formInput" value={form.fullName} onChange={f('fullName')} /></div>
            <div className="formGroup"><label className="formLabel">Тип</label>
              <select className="formSelect" value={form.type} onChange={f('type')}>
                <option value="Customer">Покупатель</option>
                <option value="Supplier">Поставщик</option>
                <option value="Both">Оба</option>
              </select></div>
            <div className="formGroup"><label className="formLabel">Статус</label>
              <select className="formSelect" value={form.status} onChange={f('status')}>
                <option value="Active">Активен</option>
                <option value="Blocked">Заблокирован</option>
              </select></div>
            <div className="formGroup"><label className="formLabel">ИНН</label>
              <input className="formInput" value={form.inn} onChange={f('inn')} /></div>
            <div className="formGroup"><label className="formLabel">КПП</label>
              <input className="formInput" value={form.kpp} onChange={f('kpp')} /></div>
            <div className="formGroup"><label className="formLabel">ОГРН</label>
              <input className="formInput" value={form.ogrn} onChange={f('ogrn')} /></div>
            <div className="formGroup"><label className="formLabel">Срок оплаты (дней)</label>
              <input className="formInput" type="number" value={form.paymentTermsDays} onChange={f('paymentTermsDays')} /></div>
          </div>
          <div className="formActions">
            <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить контрагента" subtitle="Это действие нельзя отменить" onClose={closeModal} size="sm">
          <div className="confirmBox">
            <div className="confirmBoxIcon"><Trash2 size={22} /></div>
            <p>Удалить <strong>{selected.name}</strong>?</p>
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
