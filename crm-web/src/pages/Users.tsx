import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { UserCog, Shield, Truck, Package, Users } from 'lucide-react'
import { Modal } from '../components/Modal'
import { PageLayout } from '../components/PageLayout'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import { toast } from '../components/Toast'
import '../components/common.css'

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
  isDriver: boolean
  isAccessContractors: boolean
  isAccessContractorsEdit: boolean
  isAccessContractorDiscountEdit: boolean
  isAccessNomenclature: boolean
  isAccessNomenclatureEdit: boolean
  isAccessFinancialJournal: boolean
  isAccessWarehouses: boolean
}

type PermKey = keyof Omit<UserDto, 'id' | 'email' | 'fullName' | 'phone' | 'isActive' | 'companyId' | 'isCompanyOwner'>

const ROLE_SECTION: { key: PermKey; label: string; desc: string }[] = [
  { key: 'isBasicProfile',   label: 'Администратор', desc: 'Управление пользователями компании' },
  { key: 'isAccessOrdersEdit', label: 'Менеджер заказов', desc: 'Создавать, редактировать, менять статусы заказов' },
  { key: 'isOrderPicker',    label: 'Сборщик',       desc: 'Просмотр заказов + закрытие чеклиста сборки' },
  { key: 'isExpeditor',      label: 'Экспедитор',    desc: 'Работа с доставкой и статусами отгрузки' },
  { key: 'isDriver',         label: 'Водитель',      desc: 'Только просмотр заказов (без редактирования)' },
]

const MODULE_SECTION: { key: PermKey; label: string }[] = [
  { key: 'isAccessContractors',            label: 'Контрагенты (просм.)' },
  { key: 'isAccessContractorsEdit',        label: 'Контрагенты (ред.)' },
  { key: 'isAccessContractorDiscountEdit', label: 'Скидки контрагентов (ред.)' },
  { key: 'isAccessNomenclature',           label: 'Номенклатура (просм.)' },
  { key: 'isAccessNomenclatureEdit',       label: 'Номенклатура (ред.)' },
  { key: 'isAccessFinancialJournal',       label: 'Финансовый журнал (счета, УПД, платежи)' },
  { key: 'isAccessWarehouses',             label: 'Склады и остатки' },
]

const ALL_PERM_KEYS: PermKey[] = [...ROLE_SECTION.map(r => r.key), ...MODULE_SECTION.map(m => m.key)]

function getRoleBadge(u: UserDto) {
  if (u.isCompanyOwner) return <span className="badge badge-blue">Владелец</span>
  if (u.isBasicProfile) return <span className="badge badge-purple">Администратор</span>
  if (u.isDriver)       return <span className="badge badge-gray"><Truck size={10} style={{ display:'inline', marginRight:3 }} />Водитель</span>
  if (u.isOrderPicker)  return <span className="badge badge-teal"><Package size={10} style={{ display:'inline', marginRight:3 }} />Сборщик</span>
  if (u.isExpeditor)    return <span className="badge badge-blue"><Truck size={10} style={{ display:'inline', marginRight:3 }} />Экспедитор</span>
  if (u.isAccessOrdersEdit) return <span className="badge badge-teal">Менеджер</span>
  return <span className="badge badge-gray">Сотрудник</span>
}

const emptyPerms = (): Record<PermKey, boolean> => ({
  isBasicProfile: false, isAccessOrdersEdit: false, isOrderPicker: false,
  isExpeditor: false, isDriver: false,
  isAccessContractors: false, isAccessContractorsEdit: false, isAccessContractorDiscountEdit: false,
  isAccessNomenclature: false, isAccessNomenclatureEdit: false,
  isAccessFinancialJournal: false, isAccessWarehouses: false,
})

export function UsersPage() {
  const { canManageUsers } = useAuth()
  const navigate = useNavigate()
  const [users, setUsers]     = useState<UserDto[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal]     = useState<'create' | 'edit' | 'resetPwd' | null>(null)
  const [selected, setSelected] = useState<UserDto | null>(null)

  useEffect(() => {
    if (!canManageUsers) { navigate('/'); return }
    load()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canManageUsers])

  async function load() {
    setLoading(true)
    try { setUsers(await api.get<UserDto[]>('/users')) }
    catch (e: unknown) { toast('error', e instanceof Error ? e.message : 'Ошибка загрузки') }
    finally { setLoading(false) }
  }

  async function toggleActive(u: UserDto) {
    try {
      await api.put(`/users/${u.id}/${u.isActive ? 'deactivate' : 'activate'}`, {})
      toast('success', u.isActive ? 'Пользователь заблокирован' : 'Пользователь активирован')
      load()
    } catch (e: unknown) { toast('error', e instanceof Error ? e.message : 'Ошибка') }
  }

  const openEdit = (u: UserDto) => { setSelected(u); setModal('edit') }
  const openReset = (u: UserDto) => { setSelected(u); setModal('resetPwd') }
  const closeModal = () => { setModal(null); setSelected(null) }

  return (
    <PageLayout
      title="Сотрудники"
      actions={<button className="btn btn-primary" onClick={() => setModal('create')}><Users size={14} style={{ marginRight: 6 }} />Добавить сотрудника</button>}
    >
      <div className="tableWrap">
        {loading ? <div className="tableLoading">Загрузка…</div> : users.length === 0 ? (
          <div className="tableEmpty">Нет сотрудников</div>
        ) : (
          <table className="crmTable">
            <thead>
              <tr>
                <th>Сотрудник</th>
                <th>Роль</th>
                <th>Права доступа</th>
                <th>Статус</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id}>
                  <td>
                    <div style={{ fontWeight: 600 }}>{u.fullName ?? u.email}</div>
                    {u.fullName && <div style={{ fontSize: 12, opacity: .6 }}>{u.email}</div>}
                    {u.phone    && <div style={{ fontSize: 12, opacity: .6 }}>{u.phone}</div>}
                  </td>
                  <td>{getRoleBadge(u)}</td>
                  <td>
                    {u.isCompanyOwner ? (
                      <span style={{ opacity: .5, fontSize: 13 }}>Все права</span>
                    ) : (
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
                        {MODULE_SECTION.filter(p => u[p.key]).map(p => (
                          <span key={p.key} className="badge badge-teal" style={{ fontSize: 11 }}>{p.label}</span>
                        ))}
                        {!MODULE_SECTION.some(p => u[p.key]) && (
                          <span style={{ opacity: .4, fontSize: 13 }}>—</span>
                        )}
                      </div>
                    )}
                  </td>
                  <td>
                    <span className={`badge ${u.isActive ? 'badge-green' : 'badge-red'}`}>
                      {u.isActive ? 'Активен' : 'Заблокирован'}
                    </span>
                  </td>
                  <td onClick={e => e.stopPropagation()}>
                    {!u.isCompanyOwner && (
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => openEdit(u)}><UserCog size={13} /></button>
                        <button className="btn btn-secondary btn-sm" onClick={() => openReset(u)}>Пароль</button>
                        <button
                          className={`btn btn-sm ${u.isActive ? 'btn-danger' : 'btn-secondary'}`}
                          onClick={() => toggleActive(u)}
                        >{u.isActive ? 'Блок.' : 'Разблок.'}</button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modal === 'create' && (
        <CreateUserModal onClose={closeModal} onCreated={() => { closeModal(); load() }} />
      )}
      {modal === 'edit' && selected && (
        <EditPermsModal user={selected} onClose={closeModal} onSaved={() => { closeModal(); load() }} />
      )}
      {modal === 'resetPwd' && selected && (
        <ResetPasswordModal user={selected} onClose={closeModal} />
      )}
    </PageLayout>
  )
}

// ── Create User Modal ─────────────────────────────────────────────────────────
function CreateUserModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({ email: '', password: '', fullName: '', phone: '', ...emptyPerms() })
  const [saving, setSaving] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      await api.post('/users', form)
      toast('success', 'Сотрудник добавлен')
      onCreated()
    } catch (e: unknown) { toast('error', e instanceof Error ? e.message : 'Ошибка') }
    finally { setSaving(false) }
  }

  const toggle = (k: PermKey) => setForm(f => ({ ...f, [k]: !f[k] }))

  return (
    <Modal title="Новый сотрудник" subtitle="Доступ к CRM системе" onClose={onClose} size="lg">
      <form onSubmit={submit}>
        <div className="formGrid">
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

        <div className="formSection">
          <div className="formSectionTitle"><Shield size={14} />Роли сотрудника</div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
            {ROLE_SECTION.map(r => (
              <div key={r.key} className="formCheckboxRow" onClick={() => toggle(r.key)} style={{ cursor: 'pointer', borderRadius: 8, padding: '8px 10px', background: form[r.key] ? 'var(--primary-light, #eff6ff)' : 'var(--bg-page)' }}>
                <div>
                  <div style={{ fontWeight: 500, fontSize: 13 }}>{r.label}</div>
                  <div style={{ fontSize: 11, opacity: .6, marginTop: 1 }}>{r.desc}</div>
                </div>
                <input type="checkbox" checked={!!form[r.key]} onChange={() => toggle(r.key)} onClick={e => e.stopPropagation()} />
              </div>
            ))}
          </div>
        </div>

        <div className="formSection">
          <div className="formSectionTitle"><UserCog size={14} />Доступ к модулям</div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
            {MODULE_SECTION.map(m => (
              <div key={m.key} className="formCheckboxRow">
                <label style={{ fontSize: 13 }}>{m.label}</label>
                <input type="checkbox" checked={!!form[m.key]} onChange={() => toggle(m.key)} />
              </div>
            ))}
          </div>
        </div>

        <div className="formActions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Создание…' : 'Создать'}</button>
        </div>
      </form>
    </Modal>
  )
}

// ── Edit Permissions Modal ────────────────────────────────────────────────────
function EditPermsModal({ user, onClose, onSaved }: { user: UserDto; onClose: () => void; onSaved: () => void }) {
  const initPerms = () => {
    const p: Record<PermKey, boolean> = emptyPerms()
    ALL_PERM_KEYS.forEach(k => { p[k] = !!user[k] })
    return p
  }
  const [perms, setPerms] = useState<Record<PermKey, boolean>>(initPerms)
  const [saving, setSaving] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      await api.put(`/users/${user.id}/permissions`, perms)
      toast('success', 'Права обновлены')
      onSaved()
    } catch (e: unknown) { toast('error', e instanceof Error ? e.message : 'Ошибка') }
    finally { setSaving(false) }
  }

  const toggle = (k: PermKey) => setPerms(v => ({ ...v, [k]: !v[k] }))

  return (
    <Modal title={user.fullName ?? user.email} subtitle="Настройка прав доступа" onClose={onClose} size="lg">
      <form onSubmit={submit}>
        <div className="formSection">
          <div className="formSectionTitle"><Shield size={14} />Роли</div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
            {ROLE_SECTION.map(r => (
              <div key={r.key} className="formCheckboxRow" onClick={() => toggle(r.key)} style={{ cursor: 'pointer', borderRadius: 8, padding: '8px 10px', background: perms[r.key] ? 'var(--primary-light, #eff6ff)' : 'var(--bg-page)' }}>
                <div>
                  <div style={{ fontWeight: 500, fontSize: 13 }}>{r.label}</div>
                  <div style={{ fontSize: 11, opacity: .6, marginTop: 1 }}>{r.desc}</div>
                </div>
                <input type="checkbox" checked={perms[r.key]} onChange={() => toggle(r.key)} onClick={e => e.stopPropagation()} />
              </div>
            ))}
          </div>
        </div>

        <div className="formSection">
          <div className="formSectionTitle"><UserCog size={14} />Доступ к модулям</div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
            {MODULE_SECTION.map(m => (
              <div key={m.key} className="formCheckboxRow">
                <label style={{ fontSize: 13 }}>{m.label}</label>
                <input type="checkbox" checked={perms[m.key]} onChange={() => toggle(m.key)} />
              </div>
            ))}
          </div>
        </div>

        <div className="formActions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
        </div>
      </form>
    </Modal>
  )
}

// ── Reset Password Modal ──────────────────────────────────────────────────────
function ResetPasswordModal({ user, onClose }: { user: UserDto; onClose: () => void }) {
  const [pwd, setPwd] = useState('')
  const [saving, setSaving] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (pwd.length < 6) { toast('error', 'Пароль минимум 6 символов'); return }
    setSaving(true)
    try {
      await api.put(`/users/${user.id}/reset-password`, { newPassword: pwd })
      toast('success', 'Пароль изменён')
      onClose()
    } catch (e: unknown) { toast('error', e instanceof Error ? e.message : 'Ошибка') }
    finally { setSaving(false) }
  }

  return (
    <Modal title="Сброс пароля" subtitle={user.fullName ?? user.email} onClose={onClose} size="sm">
      <form onSubmit={submit}>
        <div className="formGroup">
          <label className="formLabel">Новый пароль</label>
          <input className="formInput" type="password" value={pwd} required minLength={6}
            onChange={e => setPwd(e.target.value)} autoFocus />
        </div>
        <div className="formActions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Сохранение…' : 'Изменить'}</button>
        </div>
      </form>
    </Modal>
  )
}
