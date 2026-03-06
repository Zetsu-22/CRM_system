import type { ReactNode } from 'react'
import './Modal.css'

interface ModalProps {
  title: string
  onClose: () => void
  children: ReactNode
  width?: number
}

export function Modal({ title, onClose, children, width = 520 }: ModalProps) {
  return (
    <div className="modalOverlay" onClick={onClose}>
      <div
        className="modalBox"
        style={{ maxWidth: width }}
        onClick={e => e.stopPropagation()}
      >
        <div className="modalHeader">
          <span className="modalTitle">{title}</span>
          <button className="modalClose" onClick={onClose}>×</button>
        </div>
        <div className="modalBody">{children}</div>
      </div>
    </div>
  )
}
