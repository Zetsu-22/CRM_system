import { useState, type FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../AuthContext'
import type { UserProfile } from '../AuthContext'
import '../App.css'

export function RegisterPage() {
  const { login } = useAuth()
  const navigate  = useNavigate()
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', password: '', confirm: '' })
  const [showPwd, setShowPwd]       = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [error, setError]           = useState<string | null>(null)
  const [loading, setLoading]       = useState(false)

  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [k]: e.target.value }))

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (form.password.length < 6) { setError('Пароль должен быть не короче 6 символов'); return }
    if (form.password !== form.confirm) { setError('Пароли не совпадают'); return }
    setLoading(true)
    try {
      const res = await fetch('/api/v1/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: form.email, password: form.password, fullName: form.fullName || null, phone: form.phone || null }),
      })
      const data = await res.json()
      if (!res.ok) { setError(data?.error ?? `Ошибка ${res.status}`); return }
      login(data.token as string, (data.refreshToken as string | undefined) ?? null, data.profile as UserProfile)
      navigate('/', { replace: true })
    } catch {
      setError('Ошибка соединения с сервером')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="authShell">
      {/* ── Left: form ── */}
      <div className="authLeft">
        <div className="authLogoRow">
          <div className="authLogoIcon">C</div>
          <span className="authLogoText">CRM System</span>
        </div>

        <h1 className="authHeading">Создать аккаунт</h1>
        <p className="authSubtitle">Заполните данные для регистрации</p>

        <form onSubmit={handleSubmit} className="authForm">
          {/* Имя */}
          <div className="authField">
            <label htmlFor="fullName">Имя</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="8" r="4"/><path d="M20 21a8 8 0 1 0-16 0"/>
                </svg>
              </span>
              <input id="fullName" className="authInput" value={form.fullName}
                onChange={set('fullName')} placeholder="Иван Иванов" />
            </div>
          </div>

          {/* Email */}
          <div className="authField">
            <label htmlFor="email">Email *</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect width="20" height="16" x="2" y="4" rx="2"/><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
                </svg>
              </span>
              <input id="email" className="authInput" type="email" autoComplete="email" required
                value={form.email} onChange={set('email')} placeholder="admin@company.ru" />
            </div>
          </div>

          {/* Телефон */}
          <div className="authField">
            <label htmlFor="phone">Телефон</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 12 19.79 19.79 0 0 1 1.61 3.4 2 2 0 0 1 3.6 1.22h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 8.79a16 16 0 0 0 6.29 6.29l.96-.96a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/>
                </svg>
              </span>
              <input id="phone" className="authInput" type="tel"
                value={form.phone} onChange={set('phone')} placeholder="+7 900 000-00-00" />
            </div>
          </div>

          {/* Пароль */}
          <div className="authField">
            <label htmlFor="password">Пароль *</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect width="18" height="11" x="3" y="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </span>
              <input id="password" className="authInput hasEye" type={showPwd ? 'text' : 'password'}
                autoComplete="new-password" required value={form.password} onChange={set('password')} placeholder="Минимум 6 символов" />
              <button type="button" className="authInputEye" onClick={() => setShowPwd(v => !v)} tabIndex={-1}>
                {showPwd ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            </div>
          </div>

          {/* Повтор пароля */}
          <div className="authField">
            <label htmlFor="confirm">Повторите пароль *</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect width="18" height="11" x="3" y="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </span>
              <input id="confirm" className="authInput hasEye" type={showConfirm ? 'text' : 'password'}
                autoComplete="new-password" required value={form.confirm} onChange={set('confirm')} placeholder="••••••••" />
              <button type="button" className="authInputEye" onClick={() => setShowConfirm(v => !v)} tabIndex={-1}>
                {showConfirm ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            </div>
          </div>

          {error && <div className="authError">{error}</div>}

          <button type="submit" className="authBtn" disabled={loading}>
            {loading ? 'Регистрация...' : 'Создать аккаунт'}
          </button>
        </form>

        <p className="authFooter">
          Уже есть аккаунт?{' '}
          <Link to="/login">Войти</Link>
        </p>
      </div>

      {/* ── Right: decorative panel ── */}
      <div className="authRight">
        <div className="authRightPattern" />
        <div className="authRightContent">
          <div className="authRightIllustration">
            <svg viewBox="0 0 120 120" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="60" cy="45" r="22" fill="white" fillOpacity="0.15"/>
              <circle cx="60" cy="45" r="13" fill="white" fillOpacity="0.2"/>
              <path d="M51 45l6 6 12-12" stroke="white" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
              <rect x="18" y="78" width="84" height="28" rx="6" fill="white" fillOpacity="0.1"/>
              <rect x="26" y="86" width="32" height="4" rx="2" fill="white" fillOpacity="0.4"/>
              <rect x="26" y="95" width="48" height="3" rx="1.5" fill="white" fillOpacity="0.25"/>
              <rect x="80" y="83" width="14" height="14" rx="3" fill="white" fillOpacity="0.2"/>
            </svg>
          </div>
          <h2>Начните бесплатно</h2>
          <p>Зарегистрируйтесь, создайте компанию и пригласите команду.</p>
        </div>
      </div>
    </div>
  )
}

function EyeIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/>
    </svg>
  )
}

function EyeOffIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9.88 9.88a3 3 0 1 0 4.24 4.24"/><path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68"/><path d="M6.61 6.61A13.526 13.526 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61"/><line x1="2" x2="22" y1="2" y2="22"/>
    </svg>
  )
}
