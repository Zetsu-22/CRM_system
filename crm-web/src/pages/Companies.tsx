import { useEffect, useState } from 'react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import '../components/common.css'

interface Company {
  id: string
  name: string
  inn: string | null
  kpp: string | null
  ogrn: string | null
  legalAddress: string | null
  actualAddress: string | null
  bankName: string | null
  bik: string | null
  accountNumber: string | null
  corrAccount: string | null
  vatPayer: boolean
}

const empty = (): Omit<Company, 'id'> => ({
  name: '', inn: '', kpp: '', ogrn: '', legalAddress: '', actualAddress: '',
  bankName: '', bik: '', accountNumber: '', corrAccount: '', vatPayer: false,
})

export function CompaniesPage() {
  const [items, setItems] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | null>(null)
  const [selected, setSelected] = useState<Company | null>(null)
  const [form, setForm] = useState(empty())
  const [saving, setSaving] = useState(false)

  const load = () => {
    setLoading(true)
    api.get<Company[]>('/companies')
      .then(setItems)
      .catch(() => toast('error', 'Не удалось загрузить организации'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const openCreate = () => { setForm(empty()); setModal('create') }
  const openEdit = (c: Company) => {
    setSelected(c)
    setForm({ name: c.name, inn: c.inn ?? '', kpp: c.kpp ?? '', ogrn: c.ogrn ?? '',
      legalAddress: c.legalAddress ?? '', actualAddress: c.actualAddress ?? '',
      bankName: c.bankName ?? '', bik: c.bik ?? '', accountNumber: c.accountNumber ?? '',
      corrAccount: c.corrAccount ?? '', vatPayer: c.vatPayer })
    setModal('edit')
  }
  const openDelete = (c: Company) => { setSelected(c); setModal('delete') }
  const closeModal = () => { setModal(null); setSelected(null) }

  const handleSave = async () => {
    if (!form.name.trim()) { toast('error', 'Название обязательно'); return }
    setSaving(true)
    try {
      if (modal === 'create') {
        await api.post('/companies', form)
        toast('success', 'Организация создана')
      } else {
        await api.put(`/companies/${selected!.id}`, form)
        toast('success', 'Организация обновлена')
      }
      load(); closeModal()
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка сохранения')
    } finally { setSaving(false) }
  }

  const handleDelete = async () => {
    if (!selected) return
    setSaving(true)
    try {
      await api.delete(`/companies/${selected.id}`)
      toast('success', 'Организация удалена')
      load(); closeModal()
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка удаления')
    } finally { setSaving(false) }
  }

  const f = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(prev => ({ ...prev, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value }))

  return (
    <PageLayout title="Организации" actions={
      <button className="btn btn-primary" onClick={openCreate}>+ Добавить</button>
    }>
      <div className="tableWrap">
        {loading ? <div className="tableLoading">Загрузка…</div> : items.length === 0 ? (
          <div className="tableEmpty">Нет организаций</div>
        ) : (
          <table className="crmTable">
            <thead><tr>
              <th>Название</th><th>ИНН</th><th>КПП</th><th>НДС</th><th>Адрес</th><th></th>
            </tr></thead>
            <tbody>
              {items.map(c => (
                <tr key={c.id}>
                  <td><strong>{c.name}</strong></td>
                  <td>{c.inn || '—'}</td>
                  <td>{c.kpp || '—'}</td>
                  <td>{c.vatPayer ? <span className="badge badge-green">Да</span> : <span className="badge badge-gray">Нет</span>}</td>
                  <td style={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{c.legalAddress || '—'}</td>
                  <td>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button className="btn btn-secondary btn-sm" onClick={() => openEdit(c)}>Изменить</button>
                      <button className="btn btn-danger btn-sm" onClick={() => openDelete(c)}>Удалить</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {(modal === 'create' || modal === 'edit') && (
        <Modal title={modal === 'create' ? 'Новая организация' : 'Редактировать организацию'} onClose={closeModal} width={640}>
          <div className="formGrid">
            <div className="formGroup full"><label className="formLabel">Название *</label>
              <input className="formInput" value={form.name} onChange={f('name')} /></div>
            <div className="formGroup"><label className="formLabel">ИНН</label>
              <input className="formInput" value={form.inn ?? ''} onChange={f('inn')} /></div>
            <div className="formGroup"><label className="formLabel">КПП</label>
              <input className="formInput" value={form.kpp ?? ''} onChange={f('kpp')} /></div>
            <div className="formGroup"><label className="formLabel">ОГРН</label>
              <input className="formInput" value={form.ogrn ?? ''} onChange={f('ogrn')} /></div>
            <div className="formGroup"><label className="formLabel">Банк</label>
              <input className="formInput" value={form.bankName ?? ''} onChange={f('bankName')} /></div>
            <div className="formGroup"><label className="formLabel">БИК</label>
              <input className="formInput" value={form.bik ?? ''} onChange={f('bik')} /></div>
            <div className="formGroup"><label className="formLabel">Р/с</label>
              <input className="formInput" value={form.accountNumber ?? ''} onChange={f('accountNumber')} /></div>
            <div className="formGroup"><label className="formLabel">К/с</label>
              <input className="formInput" value={form.corrAccount ?? ''} onChange={f('corrAccount')} /></div>
            <div className="formGroup full"><label className="formLabel">Юридический адрес</label>
              <input className="formInput" value={form.legalAddress ?? ''} onChange={f('legalAddress')} /></div>
            <div className="formGroup full"><label className="formLabel">Фактический адрес</label>
              <input className="formInput" value={form.actualAddress ?? ''} onChange={f('actualAddress')} /></div>
            <div className="formGroup full" style={{ flexDirection: 'row', alignItems: 'center', gap: 10 }}>
              <input type="checkbox" id="vatPayer" checked={form.vatPayer} onChange={f('vatPayer')} />
              <label htmlFor="vatPayer" className="formLabel" style={{ textTransform: 'none', cursor: 'pointer' }}>Плательщик НДС</label>
            </div>
          </div>
          <div className="formActions">
            <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить организацию?" onClose={closeModal} width={400}>
          <div className="confirmBox">
            <p>Вы уверены, что хотите удалить <strong>{selected.name}</strong>?</p>
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
