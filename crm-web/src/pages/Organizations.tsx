import { useEffect, useState } from 'react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import '../components/common.css'

interface Organization {
  id: string
  companyId: string
  name: string
  address: string | null
  isDefault: boolean
}

const emptyForm = () => ({ name: '', address: '', isDefault: false })

export function OrganizationsPage() {
  const [items, setItems] = useState<Organization[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | null>(null)
  const [selected, setSelected] = useState<Organization | null>(null)
  const [form, setForm] = useState(emptyForm())
  const [saving, setSaving] = useState(false)

  const load = () => {
    setLoading(true)
    api.get<Organization[]>('/organizations')
      .then(setItems)
      .catch(() => toast('error', 'Не удалось загрузить организации'))
      .finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const openCreate = () => { setForm(emptyForm()); setModal('create') }
  const openEdit = (o: Organization) => {
    setSelected(o)
    setForm({ name: o.name, address: o.address ?? '', isDefault: o.isDefault })
    setModal('edit')
  }
  const openDelete = (o: Organization) => { setSelected(o); setModal('delete') }

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.name.trim()) return
    setSaving(true)
    try {
      const body = { name: form.name.trim(), address: form.address.trim() || null, isDefault: form.isDefault }
      if (modal === 'edit' && selected) {
        await api.put(`/organizations/${selected.id}`, body)
        toast('success', 'Организация обновлена')
      } else {
        await api.post('/organizations', body)
        toast('success', 'Организация создана')
      }
      setModal(null)
      load()
    } catch {
      toast('error', 'Ошибка при сохранении')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!selected) return
    setSaving(true)
    try {
      await api.delete(`/organizations/${selected.id}`)
      toast('success', 'Организация удалена')
      setModal(null)
      load()
    } catch {
      toast('error', 'Ошибка при удалении')
    } finally {
      setSaving(false)
    }
  }

  return (
    <PageLayout
      title="Организации"
      actions={<button className="btn btn-primary" onClick={openCreate}>+ Добавить</button>}
    >
      {loading && <div className="tableLoading">Загрузка...</div>}

      {!loading && items.length === 0 && (
        <div className="tableWrap">
          <div className="tableEmpty">
            Организаций нет. Организации — это юридические лица или подразделения внутри компании.
          </div>
        </div>
      )}

      {!loading && items.length > 0 && (
        <div className="tableWrap">
          <table className="crmTable">
            <thead>
              <tr>
                <th>Название</th>
                <th>Адрес</th>
                <th>По умолчанию</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(org => (
                <tr key={org.id}>
                  <td>{org.name}</td>
                  <td>{org.address ?? '—'}</td>
                  <td>{org.isDefault ? <span className="badge badge-green">✓ Да</span> : '—'}</td>
                  <td style={{ display: 'flex', gap: 8 }}>
                    <button className="btn btn-sm btn-secondary" onClick={() => openEdit(org)}>Изменить</button>
                    <button className="btn btn-sm btn-danger" onClick={() => openDelete(org)}>Удалить</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {(modal === 'create' || modal === 'edit') && (
        <Modal
          title={modal === 'edit' ? 'Редактировать организацию' : 'Новая организация'}
          onClose={() => setModal(null)}
          width={480}
        >
          <form onSubmit={handleSave}>
            <div className="formGrid cols1">
              <div className="formGroup">
                <label className="formLabel">Название *</label>
                <input
                  className="formInput"
                  value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                  required
                  autoFocus
                />
              </div>
              <div className="formGroup">
                <label className="formLabel">Адрес</label>
                <input
                  className="formInput"
                  value={form.address}
                  onChange={e => setForm(f => ({ ...f, address: e.target.value }))}
                />
              </div>
              <div className="formGroup" style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                <input
                  type="checkbox"
                  id="isDefault"
                  checked={form.isDefault}
                  onChange={e => setForm(f => ({ ...f, isDefault: e.target.checked }))}
                />
                <label htmlFor="isDefault" style={{ cursor: 'pointer', fontSize: 14 }}>По умолчанию</label>
              </div>
            </div>
            <div className="formActions">
              <button type="button" className="btn btn-secondary" onClick={() => setModal(null)}>Отмена</button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Сохранение...' : 'Сохранить'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить организацию" onClose={() => setModal(null)} width={400}>
          <div className="confirmBox">
            <p>Удалить организацию <strong>{selected.name}</strong>?</p>
            <div className="confirmActions">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Отмена</button>
              <button className="btn btn-danger" onClick={handleDelete} disabled={saving}>
                {saving ? 'Удаление...' : 'Удалить'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </PageLayout>
  )
}
