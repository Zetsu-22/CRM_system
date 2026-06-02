import { ChevronLeft, X, ShoppingCart, FileText } from 'lucide-react'
import './OrderDetailPanel.css'
import './common.css'

export interface InvoiceLine {
  id: string
  itemId: string
  nameSnapshot: string
  vatRateSnapshot: string
  qty: number
  unitPrice: number
  discountType: string
  discountValue: number
  net: number
  vat: number
  gross: number
}

export interface Invoice {
  id: string
  number: string | null
  date: string
  organizationId: string
  organizationName?: string
  counterpartyId: string
  counterpartyName?: string
  orderId: string | null
  status: string
  totalNet: number
  totalVat: number
  totalGross: number
  paidAmount: number
  comment: string | null
  lines: InvoiceLine[]
}

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Черновик',
  Issued: 'Выставлен',
  Closed: 'Закрыт',
  Cancelled: 'Отменён',
}

const STATUS_BADGE: Record<string, string> = {
  Draft: 'badge-gray',
  Issued: 'badge-blue',
  Closed: 'badge-green',
  Cancelled: 'badge-red',
}

const VAT_PCT: Record<string, number> = { Vat0: 0, Vat5: 5, Vat7: 7, Vat10: 10, Vat22: 22 }

const fmt = (n: number) =>
  n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₽'

const fmtDate = (d: string) => {
  try {
    return new Date(d).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' })
  } catch {
    return d.slice(0, 10)
  }
}

const fmtDateShort = (d: string) => {
  try { return new Date(d).toLocaleDateString('ru-RU') } catch { return d.slice(0, 10) }
}

interface InvoiceDetailPanelProps {
  invoice: Invoice
  canEdit: boolean
  onClose: () => void
  onEdit: () => void
  onDelete: () => void
}

export function InvoiceDetailPanel({
  invoice,
  canEdit,
  onClose,
  onEdit,
  onDelete,
}: InvoiceDetailPanelProps) {
  const isFinal = invoice.status === 'Closed' || invoice.status === 'Cancelled'

  return (
    <div className="odp-overlay">
      {/* ── Header ── */}
      <div className="odp-header">
        <button className="odp-back-btn" onClick={onClose} title="Назад">
          <ChevronLeft size={18} />
        </button>

        <div className="odp-header-title">
          <span className="odp-title">Счёт №{invoice.number ?? '—'}</span>
          <span className="odp-title-date">— {fmtDateShort(invoice.date)}</span>
        </div>

        <span className={`badge ${STATUS_BADGE[invoice.status] ?? 'badge-gray'}`} style={{ flexShrink: 0 }}>
          {STATUS_LABELS[invoice.status] ?? invoice.status}
        </span>

        <div className="odp-header-actions">
          {canEdit && !isFinal && (
            <button className="btn btn-secondary btn-sm" onClick={onEdit}>
              Редактировать
            </button>
          )}
          {canEdit && (
            <button className="btn btn-danger btn-sm" onClick={onDelete}>
              Удалить
            </button>
          )}
        </div>

        <button className="odp-close-btn" onClick={onClose} title="Закрыть">
          <X size={16} />
        </button>
      </div>

      {/* ── Body ── */}
      <div className="odp-body">
        {/* Left: main content */}
        <div className="odp-main">
          <div className="odp-scroll">

            {/* Info grid */}
            <div className="odp-section">
              <div className="odp-section-label">Информация о счёте</div>
              <div className="odp-info-grid">
                <div className="odp-field">
                  <div className="odp-field-label">Организация</div>
                  <div className="odp-field-value">{invoice.organizationName ?? <span className="odp-field-muted">—</span>}</div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Дата</div>
                  <div className="odp-field-value">{fmtDate(invoice.date)}</div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Контрагент</div>
                  <div className="odp-field-value">{invoice.counterpartyName ?? <span className="odp-field-muted">—</span>}</div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Статус</div>
                  <div className="odp-field-value">
                    <span className={`badge ${STATUS_BADGE[invoice.status]}`}>
                      {STATUS_LABELS[invoice.status] ?? invoice.status}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Tabs */}
            <div className="odp-tabs">
              <button className="odp-tab active">
                <ShoppingCart size={13} />
                Позиции ({invoice.lines.length})
              </button>
              <button className="odp-tab" disabled style={{ opacity: .4, cursor: 'not-allowed' }}>
                <FileText size={13} />
                Документы
              </button>
            </div>

            {/* ── Positions tab ── */}
            <div className="odp-tab-content">
              <div className="tableWrap">
                <table className="crmTable">
                  <thead>
                    <tr>
                      <th style={{ width: 32 }}>№</th>
                      <th>Наименование</th>
                      <th style={{ width: 90, textAlign: 'right' }}>Кол-во</th>
                      <th style={{ width: 120, textAlign: 'right' }}>Цена</th>
                      <th style={{ width: 90, textAlign: 'right' }}>Скидка</th>
                      <th style={{ width: 60, textAlign: 'right' }}>НДС</th>
                      <th style={{ width: 120, textAlign: 'right' }}>Сумма</th>
                    </tr>
                  </thead>
                  <tbody>
                    {invoice.lines.map((l, i) => {
                      const disc = l.discountType === 'None' ? '—'
                        : l.discountType === 'Percent' ? `${l.discountValue}%`
                        : fmt(l.discountValue)
                      const vatPct = VAT_PCT[l.vatRateSnapshot] ?? 0
                      return (
                        <tr key={l.id}>
                          <td style={{ opacity: .45, fontSize: 12 }}>{i + 1}</td>
                          <td style={{ fontWeight: 500 }}>{l.nameSnapshot}</td>
                          <td style={{ textAlign: 'right' }}>{l.qty}</td>
                          <td style={{ textAlign: 'right' }}>{fmt(l.unitPrice)}</td>
                          <td style={{ textAlign: 'right', opacity: disc === '—' ? .4 : 1 }}>{disc}</td>
                          <td style={{ textAlign: 'right' }}>{vatPct}%</td>
                          <td style={{ textAlign: 'right' }}><strong>{fmt(l.gross)}</strong></td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>

              {invoice.comment && (
                <div className="odp-note">
                  <div className="odp-note-label">Комментарий</div>
                  <div className="odp-note-text">{invoice.comment}</div>
                </div>
              )}
              {!invoice.comment && (
                <div className="odp-note">
                  <div className="odp-note-label">Комментарий</div>
                  <div className="odp-note-text odp-field-muted">Нет комментария</div>
                </div>
              )}
            </div>

          </div>
        </div>

        {/* ── Right Sidebar ── */}
        <div className="odp-sidebar">
          <div className="odp-sidebar-section">
            <div className="odp-sidebar-title">Оплата</div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Оплачено</div>
              <div className="odp-sidebar-value" style={{ 
                color: invoice.paidAmount >= invoice.totalGross ? 'hsl(142,72%,35%)' : 'hsl(38,92%,50%)',
                fontWeight: 600
              }}>
                {fmt(invoice.paidAmount)}
              </div>
            </div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Осталось</div>
              <div className="odp-sidebar-value">
                {fmt(Math.max(0, invoice.totalGross - invoice.paidAmount))}
              </div>
            </div>
          </div>

          <div className="odp-sidebar-divider" />

          <div className="odp-totals">
            <div className="odp-totals-row">
              <span>Кол-во позиций</span>
              <span>{invoice.lines.length}</span>
            </div>
            <div className="odp-totals-row">
              <span>Сумма без НДС</span>
              <span>{fmt(invoice.totalNet)}</span>
            </div>
            <div className="odp-totals-row">
              <span>Сумма НДС</span>
              <span>{fmt(invoice.totalVat)}</span>
            </div>
            <div className="odp-totals-row odp-totals-final">
              <span>Итого</span>
              <span>{fmt(invoice.totalGross)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
