import { useEffect, useState } from 'react'
import { Trash2 } from 'lucide-react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import '../components/common.css'

interface Item {
  id: string
  type: string
  sku: string
  barcode: string | null
  name: string
  description: string | null
  unit: string
  vatRate: string
  isStockItem: boolean
}

const empty = () => ({ type: 'Product', sku: '', barcode: '', name: '', description: '', unit: 'шт', vatRate: 'Vat0', isStockItem: true })

const typeLabel: Record<string, string> = { Product: 'Товар', Service: 'Услуга' }
const vatLabel: Record<string, string> = { Vat0: '0%', Vat5: '5%', Vat7: '7%', Vat10: '10%', Vat22: '22%' }
const vatOptions = Object.entries(vatLabel)

export function ItemsPage() {
  const { profile } = useAuth()
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | null>(null)
  const [selected, setSelected] = useState<Item | null>(null)
  const [form, setForm] = useState(empty())
  const [saving, setSaving] = useState(false)

  const hasFullAccess = !!(profile?.isCompanyOwner || profile?.isBasicProfile)
  const canView = !!(hasFullAccess || profile?.isAccessNomenclature)
  const canEdit = !!(hasFullAccess || profile?.isAccessNomenclatureEdit)

  const load = () => {
    setLoading(true)
    api.get<Item[]>('/items')
      .then(setItems)
      .catch(() => toast('error', 'Не удалось загрузить номенклатуру'))
      .finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [])

  const openCreate = () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для создания'); return }
    setForm(empty()); setModal('create')
  }
  const openEdit = (c: Item) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для редактирования'); return }
    setSelected(c)
    setForm({ type: c.type, sku: c.sku, barcode: c.barcode ?? '', name: c.name,
      description: c.description ?? '', unit: c.unit, vatRate: c.vatRate, isStockItem: c.isStockItem })
    setModal('edit')
  }
  const openDelete = (c: Item) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSelected(c); setModal('delete')
  }
  const closeModal = () => { setModal(null); setSelected(null) }

  const f = (k: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm(prev => ({ ...prev, [k]: e.target.type === 'checkbox' ? (e.target as HTMLInputElement).checked : e.target.value }))

  const handleSave = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для сохранения'); return }
    if (!form.name.trim()) { toast('error', 'Название обязательно'); return }
    setSaving(true)
    try {
      if (modal === 'create') {
        await api.post('/items', form)
        toast('success', 'Номенклатура создана')
      } else {
        await api.put(`/items/${selected!.id}`, form)
        toast('success', 'Номенклатура обновлена')
      }
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  const handleDelete = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSaving(true)
    try {
      await api.delete(`/items/${selected!.id}`)
      toast('success', 'Удалено')
      load(); closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
    finally { setSaving(false) }
  }

  return (
    <PageLayout title="Номенклатура" actions={canEdit ? <button className="btn btn-primary" onClick={openCreate}>+ Добавить</button> : null}>
      <div className="tableWrap">
        {!canView ? <div className="tableEmpty">Нет доступа к номенклатуре</div> : loading ? <div className="tableLoading">Загрузка…</div> : items.length === 0 ? (
          <div className="tableEmpty">Нет позиций</div>
        ) : (
          <table className="crmTable">
            <thead><tr>
              <th>Артикул</th><th>Название</th><th>Тип</th><th>Ед. изм.</th><th>НДС</th><th>Склад</th><th></th>
            </tr></thead>
            <tbody>
              {items.map(c => (
                <tr key={c.id}>
                  <td><code style={{ fontSize: 12 }}>{c.sku}</code></td>
                  <td><strong>{c.name}</strong><div style={{ fontSize: 12, opacity: .6 }}>{c.description}</div></td>
                  <td>{typeLabel[c.type] ?? c.type}</td>
                  <td>{c.unit}</td>
                  <td>{vatLabel[c.vatRate] ?? c.vatRate}</td>
                  <td>{c.isStockItem ? <span className="badge badge-blue">Да</span> : <span className="badge badge-gray">Нет</span>}</td>
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
        <Modal title={modal === 'create' ? 'Новая номенклатура' : 'Редактировать номенклатуру'} subtitle="Товар или услуга" onClose={closeModal} size="md">
          <div className="formGrid">
            <div className="formGroup"><label className="formLabel">Тип</label>
              <select className="formSelect" value={form.type} onChange={f('type')}>
                <option value="Product">Товар</option>
                <option value="Service">Услуга</option>
              </select></div>
            {modal === 'edit' && (
              <div className="formGroup"><label className="formLabel">Артикул</label>
                <input className="formInput" value={form.sku} readOnly style={{ opacity: 0.6, cursor: 'not-allowed' }} /></div>
            )}
            <div className="formGroup full"><label className="formLabel">Название *</label>
              <input className="formInput" value={form.name} onChange={f('name')} /></div>
            <div className="formGroup full"><label className="formLabel">Описание</label>
              <input className="formInput" value={form.description} onChange={f('description')} /></div>
            <div className="formGroup"><label className="formLabel">Единица измерения</label>
              <input className="formInput" value={form.unit} onChange={f('unit')} /></div>
            <div className="formGroup"><label className="formLabel">Ставка НДС</label>
              <select className="formSelect" value={form.vatRate} onChange={f('vatRate')}>
                {vatOptions.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
              </select></div>
            <div className="formGroup"><label className="formLabel">Штрихкод</label>
              <input className="formInput" value={form.barcode} onChange={f('barcode')} /></div>
            <div className="formGroup full">
              <div className="formCheckboxRow">
                <label htmlFor="isStockItem">Складской учёт</label>
                <input type="checkbox" id="isStockItem" checked={form.isStockItem} onChange={f('isStockItem')} />
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
        <Modal title="Удалить номенклатуру" subtitle="Это действие нельзя отменить" onClose={closeModal} size="sm">
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
