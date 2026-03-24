import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import { useAuth } from '../AuthContext'
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
  const { hasCompany, profile } = useAuth()
  const navigate = useNavigate()
  const [items, setItems] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'edit' | null>(null)
  const [selected, setSelected] = useState<Company | null>(null)
  const [form, setForm] = useState(empty())
  const [saving, setSaving] = useState(false)
  const [seeding, setSeeding] = useState(false)

  const load = () => {
    setLoading(true)
    api.get<Company[]>('/companies')
      .then(setItems)
      .catch(() => toast('error', 'Не удалось загрузить компании'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const openEdit = (c: Company) => {
    setSelected(c)
    setForm({ name: c.name, inn: c.inn ?? '', kpp: c.kpp ?? '', ogrn: c.ogrn ?? '',
      legalAddress: c.legalAddress ?? '', actualAddress: c.actualAddress ?? '',
      bankName: c.bankName ?? '', bik: c.bik ?? '', accountNumber: c.accountNumber ?? '',
      corrAccount: c.corrAccount ?? '', vatPayer: c.vatPayer })
    setModal('edit')
  }
  const closeModal = () => { setModal(null); setSelected(null) }

  const handleSave = async () => {
    if (!form.name.trim()) { toast('error', 'Название обязательно'); return }
    setSaving(true)
    try {
      await api.put(`/companies/${selected!.id}`, form)
      toast('success', 'Компания обновлена')
      load(); closeModal()
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка сохранения')
    } finally { setSaving(false) }
  }

  const f = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(prev => ({ ...prev, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value }))

  const handleSeed = async () => {
    if (!(import.meta as any).env?.DEV) return
    if (seeding) return
    const ok = window.confirm('Создать тестовые данные в БД для компании?')
    if (!ok) return
    setSeeding(true)
    try {
      await api.post('/dev/seed-test-company', {
        companyName: profile?.companyName ?? 'Тестовая компния',
        force: false,
      })
      toast('success', 'Тестовые данные созданы')
    } catch (e: any) {
      toast('error', e.message ?? 'Не удалось создать тестовые данные')
    } finally {
      setSeeding(false)
    }
  }

  const actions = hasCompany
    ? (
      (import.meta as any).env?.DEV
        ? (
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <button className="btn btn-secondary" onClick={handleSeed} disabled={seeding}>
              {seeding ? 'Заполнение…' : 'Заполнить тестовыми данными'}
            </button>
          </div>
        )
        : null
    )
    : (
      <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
        <button className="btn btn-primary" onClick={() => navigate('/setup-company')}>Создать компанию</button>
        {(import.meta as any).env?.DEV && (
          <button className="btn btn-secondary" onClick={handleSeed} disabled={seeding}>
            {seeding ? 'Заполнение…' : 'Заполнить тестовыми данными'}
          </button>
        )}
      </div>
    )

  return (
    <PageLayout title="Компания" actions={actions}>
      <div className="tableWrap">
        {loading ? <div className="tableLoading">Загрузка…</div> : items.length === 0 ? (
          <div className="tableEmpty">Компания не создана</div>
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
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modal === 'edit' && (
        <Modal title="Редактировать компанию" subtitle="Реквизиты и банковские данные" onClose={closeModal} size="lg">
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
            <div className="formGroup full">
              <div className="formCheckboxRow">
                <label htmlFor="vatPayer">Плательщик НДС</label>
                <input type="checkbox" id="vatPayer" checked={form.vatPayer} onChange={f('vatPayer')} />
              </div>
            </div>
          </div>
          <div className="formActions">
            <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}
    </PageLayout>
  )
}
