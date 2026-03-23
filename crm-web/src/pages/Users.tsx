import { useEffect, useState, type FormEvent } from 'react'
import type { CSSProperties } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import '../components/common.css'
import '../components/Modal.css'

interface UserDto {
  id: string
  email: string
  fullName: string | null
  phone: string | null
  isActive: boolean
  companyId: string | null
  isCompanyOwner: boolean
  isBasicProfile: boolean
  isAccessOrdersEdit: boolean
  isOrderPicker: boolean
  isExpeditor: boolean
  isAccessContractors: boolean
  isAccessContractorsEdit: boolean
  isAccessContractorDiscountEdit: boolean
  isAccessNomenclature: boolean
  isAccessNomenclatureEdit: boolean
  isAccessFinancialJournal: boolean
  isAccessWarehouses: boolean
}

const PERM_LABELS: { key: keyof UserDto; label: string }[] = [
  { key: 'isBasicProfile',                 label: 'Администратор' },
  { key: 'isAccessOrdersEdit',             label: 'Заказы (ред.)' },
  { key: 'isOrderPicker',                  label: 'Сборщик' },
  { key: 'isExpeditor',                    label: 'Экспедитор' },
  { key: 'isAccessContractors',            label: 'Контрагенты (просм.)' },
  { key: 'isAccessContractorsEdit',        label: 'Контрагенты (ред.)' },
  { key: 'isAccessContractorDiscountEdit', label: 'Скидки (ред.)' },
  { key: 'isAccessNomenclature',           label: 'Номенклатура (просм.)' },
  { key: 'isAccessNomenclatureEdit',       label: 'Номенклатура (ред.)' },
  { key: 'isAccessFinancialJournal',       label: 'Финансы' },
  { key: 'isAccessWarehouses',             label: 'Склады' },
]

const muted: CSSProperties = { color: 'var(--text-muted)' }

export function UsersPage() {
  const { canManageUsers } = useAuth()
  const navigate = useNavigate()
  const [users, setUsers]       = useState<UserDto[]>([])
  const [loading, setLoading]   = useState(true)
  const [errMsg, setErrMsg]     = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [editPerms, setEditPerms]   = useState<UserDto | null>(null)

  useEffect(() => {
    if (!canManageUsers) { navigate('/'); return }
    load()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canManageUsers])

  async function load() {
    setLoading(true)
    try {
      setUsers(await api.get<UserDto[]>('/users'))
    } catch (e: unknown) { setErrMsg(e instanceof Error ? e.message : 'Ошибка') }
    finally { setLoading(false) }
  }

  async function toggleActive(u: UserDto) {
    try {
      await api.put(`/users/${u.id}/${u.isActive ? 'deactivate' : 'activate'}`, {})
      load()
    } catch (e: unknown) { setErrMsg(e instanceof Error ? e.message : 'Ошибка') }
  }

  return (
    <div className="pageLayout">
      <div className="pageHead">
        <h1 className="pageH1">Пользователи</h1>
        <div className="pageActions">
          <button className="btn btn-primary" onClick={() => setShowCreate(true)}>+ Добавить</button>
        </div>
      </div>

      {errMsg && <div style={{ color: '#ef4444', marginBottom: 12, fontSize: 14 }}>{errMsg}</div>}

      {loading ? (
        <p style={muted}>Загрузка...</p>
      ) : (
        <div className="tableWrap">
          <table className="crmTable">
            <thead>
              <tr>
                <th>Пользователь</th>
                <th>Роль</th>
                <th>Права</th>
                <th>Статус</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id}>
                  <td>
                    <div style={{ fontWeight: 600 }}>{u.fullName ?? u.email}</div>
                    {u.fullName && <div style={{ ...muted, fontSize: 12 }}>{u.email}</div>}
                    {u.phone   && <div style={{ ...muted, fontSize: 12 }}>{u.phone}</div>}
                  </td>
                  <td>
                    {u.isCompanyOwner
                      ? <span className="badge badge-blue">Владелец</span>
                      : u.isBasicProfile
                        ? <span className="badge badge-purple">Администратор</span>
                        : <span className="badge badge-gray">Сотрудник</span>}
                  </td>
                  <td>
                    {u.isCompanyOwner ? (
                      <span style={muted}>Все права</span>
                    ) : (
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
                        {PERM_LABELS.filter(p => u[p.key] === true).map(p => (
                          <span key={p.key} className="badge badge-teal" style={{ fontSize: 11 }}>{p.label}</span>
                        ))}
                        {PERM_LABELS.every(p => !u[p.key]) && <span style={muted}>Нет прав</span>}
                      </div>
                    )}
                  </td>
                  <td>
                    <span className={`badge ${u.isActive ? 'badge-green' : 'badge-red'}`}>
                      {u.isActive ? 'Активен' : 'Заблокирован'}
                    </span>
                  </td>
                  <td>
                    {!u.isCompanyOwner && (
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => setEditPerms({ ...u })}>Права</button>
                        <button
                          className={`btn btn-sm ${u.isActive ? 'btn-danger' : 'btn-secondary'}`}
                          onClick={() => toggleActive(u)}
                        >
                          {u.isActive ? 'Блок.' : 'Разблок.'}
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
              {users.length === 0 && (
                <tr>
                  <td colSpan={5} className="tableEmpty">Нет пользователей</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {showCreate && (
        <CreateUserModal
          onClose={() => setShowCreate(false)}
          onCreated={() => { setShowCreate(false); load() }}
        />
      )}
      {editPerms && (
        <EditPermsModal
          user={editPerms}
          onClose={() => setEditPerms(null)}
          onSaved={() => { setEditPerms(null); load() }}
        />
      )}
    </div>
  )
}

// ── Create User Modal ─────────────────────────────────────────────────────────
function CreateUserModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({
    email: '', password: '', fullName: '', phone: '',
    isBasicProfile: false,
    isAccessOrdersEdit: false, isOrderPicker: false, isExpeditor: false,
    isAccessContractors: false, isAccessContractorsEdit: false, isAccessContractorDiscountEdit: false,
    isAccessNomenclature: false, isAccessNomenclatureEdit: false,
    isAccessFinancialJournal: false, isAccessWarehouses: false,
  })
  const [loading, setLoading] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setLoading(true); setErr(null)
    try {
      await api.post('/users', form)
      onCreated()
    } catch (e: unknown) { setErr(e instanceof Error ? e.message : 'Ошибка') }
    finally { setLoading(false) }
  }

  return (
    <div className="modalOverlay" onClick={onClose}>
      <div className="modalBox" style={{ maxWidth: 520 }} onClick={e => e.stopPropagation()}>
        <div className="modalHeader">
          <span className="modalTitle">Новый пользователь</span>
          <button className="modalClose" onClick={onClose}>✕</button>
        </div>
        <div className="modalBody">
          <form onSubmit={submit} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="formGroup">
              <label className="formLabel">Email *</label>
              <input className="formInput" required type="email" value={form.email}
                onChange={e => setForm(f => ({ ...f, email: e.target.value }))} />
            </div>
            <div className="formGroup">
              <label className="formLabel">Пароль *</label>
              <input className="formInput" required type="password" value={form.password}
                onChange={e => setForm(f => ({ ...f, password: e.target.value }))} />
            </div>
            <div className="formGrid">
              <div className="formGroup">
                <label className="formLabel">Имя</label>
                <input className="formInput" value={form.fullName}
                  onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))} />
              </div>
              <div className="formGroup">
                <label className="formLabel">Телефон</label>
                <input className="formInput" value={form.phone}
                  onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} />
              </div>
            </div>

            <div style={{ marginTop: 4 }}>
              <div style={{ fontWeight: 600, marginBottom: 10, fontSize: 13, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.4px' }}>Права доступа</div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
                {PERM_LABELS.map(p => (
                  <label key={p.key} style={{ display: 'flex', alignItems: 'center', gap: 7, fontSize: 13, cursor: 'pointer' }}>
                    <input type="checkbox" checked={!!form[p.key as keyof typeof form]}
                      onChange={() => setForm(f => ({ ...f, [p.key]: !f[p.key as keyof typeof f] }))} />
                    {p.label}
                  </label>
                ))}
              </div>
            </div>

            {err && <div style={{ color: '#ef4444', fontSize: 13 }}>{err}</div>}
            <div className="formActions">
              <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Создание...' : 'Создать'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

// ── Edit Permissions Modal ────────────────────────────────────────────────────
function EditPermsModal({ user, onClose, onSaved }: { user: UserDto; onClose: () => void; onSaved: () => void }) {
  const [perms, setPerms] = useState({
    isBasicProfile: user.isBasicProfile,
    isAccessOrdersEdit: user.isAccessOrdersEdit,
    isOrderPicker: user.isOrderPicker,
    isExpeditor: user.isExpeditor,
    isAccessContractors: user.isAccessContractors,
    isAccessContractorsEdit: user.isAccessContractorsEdit,
    isAccessContractorDiscountEdit: user.isAccessContractorDiscountEdit,
    isAccessNomenclature: user.isAccessNomenclature,
    isAccessNomenclatureEdit: user.isAccessNomenclatureEdit,
    isAccessFinancialJournal: user.isAccessFinancialJournal,
    isAccessWarehouses: user.isAccessWarehouses,
  })
  const [loading, setLoading] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setLoading(true); setErr(null)
    try {
      await api.put(`/users/${user.id}/permissions`, perms)
      onSaved()
    } catch (e: unknown) { setErr(e instanceof Error ? e.message : 'Ошибка') }
    finally { setLoading(false) }
  }

  return (
    <div className="modalOverlay" onClick={onClose}>
      <div className="modalBox" style={{ maxWidth: 460 }} onClick={e => e.stopPropagation()}>
        <div className="modalHeader">
          <span className="modalTitle">Права: {user.fullName ?? user.email}</span>
          <button className="modalClose" onClick={onClose}>✕</button>
        </div>
        <div className="modalBody">
          <form onSubmit={submit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 16 }}>
              {PERM_LABELS.map(p => (
                <label key={p.key} style={{ display: 'flex', alignItems: 'center', gap: 7, fontSize: 14, cursor: 'pointer' }}>
                  <input type="checkbox"
                    checked={!!perms[p.key as keyof typeof perms]}
                    onChange={() => setPerms(v => ({ ...v, [p.key]: !v[p.key as keyof typeof v] }))} />
                  {p.label}
                </label>
              ))}
            </div>
            {err && <div style={{ color: '#ef4444', fontSize: 13, marginBottom: 12 }}>{err}</div>}
            <div className="formActions">
              <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Сохранение...' : 'Сохранить'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
