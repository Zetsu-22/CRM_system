import { useEffect, useState } from 'react'
import { Trash2 } from 'lucide-react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import '../components/common.css'

interface Warehouse {
  id: string
  name: string
  address: string | null
  isDefault: boolean
}

const empty = () => ({ name: '', address: '', isDefault: false })

export function WarehousesPage() {
  const { profile } = useAuth()
  const [items, setItems] = useState<Warehouse[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | null>(null)
  const [selected, setSelected] = useState<Warehouse | null>(null)
  const [form, setForm] = useState(empty())
  const [saving, setSaving] = useState(false)

  const hasFullAccess = !!(profile?.isCompanyOwner || profile?.isBasicProfile)
  const canView = !!(hasFullAccess || profile?.isAccessWarehouses)
  const canEdit = !!(hasFullAccess || profile?.isAccessWarehousesEdit)

  const load = () => {
    setLoading(true)
    api.get<Warehouse[]>('/warehouses')
      .then(setItems)
      .catch(() => toast('error', 'Не удалось загрузить склады'))
      .finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const openCreate = () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для создания склада'); return }
    setForm(empty()); setModal('create')
  }
  const openEdit = (c: Warehouse) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для редактирования склада'); return }
    setSelected(c)
    setForm({ name: c.name, address: c.address ?? '', isDefault: c.isDefault })
    setModal('edit')
  }
  const openDelete = (c: Warehouse) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления склада'); return }
    setSelected(c); setModal('delete')
  }
  const closeModal = () => { setModal(null); setSelected(null) }

  const f = (k: string) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(prev => ({ ...prev, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value }))

  const handleSave = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для сохранения'); return }
    if (!form.name.trim()) { toast('error', 'Название обязательно'); return }
    setSaving(true)
    try {
      if (modal === 'create') {
        await api.post('/warehouses', form)
        toast('success', 'Склад создан')
      } else {
        await api.put(`/warehouses/${selected!.id}`, form)
        toast('success', 'Склад обновлён')
      }
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  const handleDelete = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSaving(true)
    try {
      await api.delete(`/warehouses/${selected!.id}`)
      toast('success', 'Склад удалён')
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  return (
    <PageLayout title="Склады" actions={
      canEdit ? <button className="btn btn-primary" onClick={openCreate}>+ Добавить</button> : null
    }>
      <div className="tableWrap">
        {!canView ? <div className="tableEmpty">Нет доступа к складам</div> : loading ? <div className="tableLoading">Загрузка…</div> : items.length === 0 ? (
          <div className="tableEmpty">Нет складов</div>
        ) : (
          <table className="crmTable">
            <thead><tr>
              <th>Название</th><th>Адрес</th><th>По умолчанию</th><th></th>
            </tr></thead>
            <tbody>
              {items.map(c => (
                <tr key={c.id}>
                  <td><strong>{c.name}</strong></td>
                  <td>{c.address || '—'}</td>
                  <td>{c.isDefault ? <span className="badge badge-green">Да</span> : <span className="badge badge-gray">Нет</span>}</td>
                  <td onClick={e => e.stopPropagation()}>
                    {canEdit && (
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => openEdit(c)}>Изм.</button>
                        <button className="btn btn-danger btn-sm" onClick={() => openDelete(c)}>Удал.</button>
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
        <Modal title={modal === 'create' ? 'Новый склад' : 'Редактировать склад'} subtitle="Место хранения товаров" onClose={closeModal} size="sm">
          <div className="formGrid cols1">
            <div className="formGroup"><label className="formLabel">Название *</label>
              <input className="formInput" value={form.name} onChange={f('name')} /></div>
            <div className="formGroup"><label className="formLabel">Адрес</label>
              <input className="formInput" value={form.address} onChange={f('address')} /></div>
            <div className="formGroup">
              <div className="formCheckboxRow">
                <label htmlFor="isDefault">Склад по умолчанию</label>
                <input type="checkbox" id="isDefault" checked={form.isDefault} onChange={f('isDefault')} />
              </div>
            </div>
          </div>
          <div className="formActions">
            <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить склад" subtitle="Это действие нельзя отменить" onClose={closeModal} size="sm">
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
