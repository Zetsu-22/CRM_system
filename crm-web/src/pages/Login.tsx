import { useState, type FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../AuthContext'
import type { UserProfile } from '../AuthContext'
import '../App.css'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail]       = useState('')
  const [password, setPassword] = useState('')
  const [showPwd, setShowPwd]   = useState(false)
  const [error, setError]       = useState<string | null>(null)
  const [loading, setLoading]   = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await fetch('/api/v1/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      const data = await res.json()
      if (!res.ok) { setError(data?.error ?? `Ошибка ${res.status}`); return }
      login(data.token as string, data.profile as UserProfile)
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

        <h1 className="authHeading">Вход в систему</h1>
        <p className="authSubtitle">Введите данные своего аккаунта</p>

        <form onSubmit={handleSubmit} className="authForm">
          <div className="authField">
            <label htmlFor="email">Email</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect width="20" height="16" x="2" y="4" rx="2"/><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
                </svg>
              </span>
              <input
                id="email"
                className="authInput"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="admin@company.ru"
              />
            </div>
          </div>

          <div className="authField">
            <label htmlFor="password">Пароль</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect width="18" height="11" x="3" y="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </span>
              <input
                id="password"
                className="authInput hasEye"
                type={showPwd ? 'text' : 'password'}
                autoComplete="current-password"
                required
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="••••••••"
              />
              <button type="button" className="authInputEye" onClick={() => setShowPwd(v => !v)} tabIndex={-1}>
                {showPwd ? (
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M9.88 9.88a3 3 0 1 0 4.24 4.24"/><path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68"/><path d="M6.61 6.61A13.526 13.526 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61"/><line x1="2" x2="22" y1="2" y2="22"/>
                  </svg>
                ) : (
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/>
                  </svg>
                )}
              </button>
            </div>
          </div>

          {error && <div className="authError">{error}</div>}

          <button type="submit" className="authBtn" disabled={loading}>
            {loading ? 'Вход...' : 'Войти'}
          </button>
        </form>

        <p className="authFooter">
          Нет аккаунта?{' '}
          <Link to="/register">Зарегистрироваться</Link>
        </p>
      </div>

      {/* ── Right: decorative panel ── */}
      <div className="authRight">
        <div className="authRightPattern" />
        <div className="authRightContent">
          <div className="authRightIllustration">
            <svg viewBox="0 0 120 120" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="10" y="20" width="100" height="80" rx="8" fill="white" fillOpacity="0.12"/>
              <rect x="20" y="35" width="40" height="6" rx="3" fill="white" fillOpacity="0.5"/>
              <rect x="20" y="47" width="60" height="4" rx="2" fill="white" fillOpacity="0.3"/>
              <rect x="20" y="57" width="50" height="4" rx="2" fill="white" fillOpacity="0.3"/>
              <rect x="20" y="70" width="30" height="18" rx="4" fill="white" fillOpacity="0.2"/>
              <rect x="58" y="70" width="30" height="18" rx="4" fill="white" fillOpacity="0.2"/>
              <circle cx="95" cy="35" r="16" fill="white" fillOpacity="0.15"/>
              <path d="M88 35l4 4 8-8" stroke="white" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </div>
          <h2>Управляйте бизнесом эффективно</h2>
          <p>Заказы, склад, контрагенты и финансы — всё в одном месте.</p>
        </div>
      </div>
    </div>
  )
}
