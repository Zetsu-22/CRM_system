import type { ReactNode } from 'react'
import { X } from 'lucide-react'
import './Modal.css'

type ModalSize = 'sm' | 'md' | 'lg' | 'xl'

const SIZE_MAP: Record<ModalSize, number> = {
  sm: 440,
  md: 580,
  lg: 800,
  xl: 980,
}

interface ModalProps {
  title: string
  subtitle?: string
  onClose: () => void
  children: ReactNode
  size?: ModalSize
  width?: number
}

export function Modal({ title, subtitle, onClose, children, size = 'md', width }: ModalProps) {
  const maxWidth = width ?? SIZE_MAP[size]
  return (
    <div className="modalOverlay" onClick={onClose} role="presentation">
      <div
        className="modalBox"
        style={{ maxWidth }}
        onClick={e => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-label={title}
      >
        <div className="modalHeader">
          <div className="modalHeaderInfo">
            <span className="modalTitle">{title}</span>
            {subtitle && <span className="modalSubtitle">{subtitle}</span>}
          </div>
          <button className="modalClose" onClick={onClose} aria-label="Закрыть">
            <X size={18} />
          </button>
        </div>
        <div className="modalBody">{children}</div>
      </div>
    </div>
  )
}
