import { useEffect, useState } from 'react'
import './Toast.css'

export type ToastType = 'success' | 'error' | 'info'

export interface ToastMessage {
  id: number
  type: ToastType
  text: string
}

let _addToast: ((type: ToastType, text: string) => void) | null = null

export function toast(type: ToastType, text: string) {
  _addToast?.(type, text)
}

export function ToastContainer() {
  const [items, setItems] = useState<ToastMessage[]>([])

  useEffect(() => {
    _addToast = (type, text) => {
      const id = Date.now()
      setItems(prev => [...prev, { id, type, text }])
      setTimeout(() => {
        setItems(prev => prev.filter(t => t.id !== id))
      }, 3500)
    }
    return () => { _addToast = null }
  }, [])

  if (!items.length) return null

  return (
    <div className="toastContainer">
      {items.map(t => (
        <div key={t.id} className={`toastItem toast-${t.type}`}>
          <span className="toastIcon">
            {t.type === 'success' ? '✓' : t.type === 'error' ? '✕' : 'i'}
          </span>
          <span className="toastText">{t.text}</span>
          <button
            className="toastClose"
            onClick={() => setItems(prev => prev.filter(x => x.id !== t.id))}
          >×</button>
        </div>
      ))}
    </div>
  )
}
