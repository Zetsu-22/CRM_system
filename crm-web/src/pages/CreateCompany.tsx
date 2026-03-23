import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../AuthContext'
import type { UserProfile } from '../AuthContext'
import '../App.css'

export function CreateCompanyPage() {
  const { token, login, profile, logout } = useAuth()
  const navigate  = useNavigate()
  const [name, setName]     = useState('')
  const [error, setError]   = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) { setError('Введите название компании'); return }
    setError(null)
    setLoading(true)
    try {
      const res = await fetch('/api/v1/auth/setup-company', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({ companyName: name.trim() }),
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

        {/* Step indicator */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 28 }}>
          <StepDot done label="1" />
          <div style={{ flex: 1, height: 2, background: 'rgba(79,110,247,0.5)', borderRadius: 1 }} />
          <StepDot active label="2" />
        </div>

        <h1 className="authHeading">Создайте компанию</h1>
        <p className="authSubtitle" style={{ marginBottom: 28 }}>
          Привет{profile?.fullName ? `, ${profile.fullName.split(' ')[0]}` : ''}! Последний шаг — назовите вашу компанию.
        </p>

        <form onSubmit={handleSubmit} className="authForm">
          <div className="authField">
            <label htmlFor="companyName">Название компании *</label>
            <div className="authInputWrap">
              <span className="authInputIcon">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect width="16" height="20" x="4" y="2" rx="2" ry="2"/>
                  <path d="M9 22v-4h6v4"/><path d="M8 6h.01M16 6h.01M12 6h.01M12 10h.01M8 10h.01M16 10h.01M12 14h.01M8 14h.01M16 14h.01"/>
                </svg>
              </span>
              <input
                id="companyName"
                className="authInput"
                autoFocus
                required
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="ООО Ромашка"
              />
            </div>
          </div>

          {error && <div className="authError">{error}</div>}

          <button type="submit" className="authBtn" disabled={loading || !name.trim()}>
            {loading ? 'Создание...' : 'Войти в систему →'}
          </button>
        </form>

        <p className="authFooter" style={{ marginTop: 20 }}>
          <button
            style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: 13 }}
            onClick={() => { logout(); navigate('/login', { replace: true }) }}
          >
            Выйти из аккаунта
          </button>
        </p>
      </div>

      {/* ── Right: decorative panel ── */}
      <div className="authRight">
        <div className="authRightPattern" />
        <div className="authRightContent">
          <div className="authRightIllustration">
            <svg viewBox="0 0 120 120" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="15" y="30" width="90" height="60" rx="8" fill="white" fillOpacity="0.12"/>
              <rect x="25" y="42" width="20" height="20" rx="4" fill="white" fillOpacity="0.25"/>
              <rect x="53" y="44" width="36" height="5" rx="2.5" fill="white" fillOpacity="0.5"/>
              <rect x="53" y="54" width="24" height="4" rx="2" fill="white" fillOpacity="0.3"/>
              <rect x="25" y="70" width="70" height="3" rx="1.5" fill="white" fillOpacity="0.15"/>
              <rect x="25" y="78" width="50" height="3" rx="1.5" fill="white" fillOpacity="0.15"/>
              <circle cx="88" cy="25" r="14" fill="white" fillOpacity="0.15"/>
              <path d="M82 25l4 4 8-8" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </div>
          <h2>Ваша компания, ваши правила</h2>
          <p>Вы станете владельцем компании с полным доступом ко всем функциям системы.</p>
        </div>
      </div>
    </div>
  )
}

function StepDot({ label, done, active }: { label: string; done?: boolean; active?: boolean }) {
  const bg = done ? '#4f6ef7' : active ? '#4f6ef7' : 'rgba(255,255,255,0.1)'
  const color = (done || active) ? '#fff' : 'var(--text-muted)'
  return (
    <div style={{
      width: 28, height: 28, borderRadius: '50%',
      background: bg, color, display: 'flex',
      alignItems: 'center', justifyContent: 'center',
      fontSize: 13, fontWeight: 700, flexShrink: 0,
      border: active ? '2px solid rgba(79,110,247,0.4)' : 'none',
    }}>
      {done ? (
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
          <polyline points="20 6 9 17 4 12"/>
        </svg>
      ) : label}
    </div>
  )
}
