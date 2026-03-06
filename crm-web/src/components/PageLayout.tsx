import type { ReactNode } from 'react'
import './PageLayout.css'

interface Props {
  title: string
  actions?: ReactNode
  children: ReactNode
}

export function PageLayout({ title, actions, children }: Props) {
  return (
    <div className="pageLayout">
      <div className="pageHead">
        <h1 className="pageH1">{title}</h1>
        {actions && <div className="pageActions">{actions}</div>}
      </div>
      {children}
    </div>
  )
}
