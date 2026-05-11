import { useState } from 'react'
import { ChevronLeft, X, ShoppingCart, FileText, ClipboardCheck, CheckCircle2 } from 'lucide-react'
import { api } from '../api'
import { toast } from './Toast'
import './OrderDetailPanel.css'
import './common.css'

export interface OdpOrderLine {
  id: string
  nameSnapshot: string
  vatRateSnapshot: string
  qty: number
  unitPrice: number
  discountType: string
  discountValue: number
  net: number
  vat: number
  gross: number
  availableQty?: number
}

export interface OdpOrder {
  id: string
  number: string | null
  date: string
  company: { id: string; name: string } | null
  counterparty: { id: string; name: string } | null
  status: string
  totalNet: number
  totalVat: number
  totalGross: number
  comment: string | null
  isAssemblyChecklistClosed: boolean
  pickerId?: string | null
  driverId?: string | null
  bankAccount?: string | null
  contract?: string | null
  deliveryAddress?: string | null
  lines: OdpOrderLine[]
}

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Черновик',
  Processing: 'Обрабатывается',
  WaitingForPayment: 'Ожидает оплаты',
  BillConfirmed: 'Счёт подтверждён',
  Manufacturing: 'Изготавливается',
  Picking: 'Собирается',
  TransferredToTransport: 'Передан в ТК',
  DeliveringByTransport: 'Доставляется ТК',
  Delivering: 'Доставляется',
  AwaitingPickup: 'Готов к получению',
  Completed: 'Получен',
  Cancelled: 'Отменён',
}

const STATUS_BADGE: Record<string, string> = {
  Draft: 'badge-gray',
  Processing: 'badge-blue',
  WaitingForPayment: 'badge-orange',
  BillConfirmed: 'badge-green',
  Manufacturing: 'badge-purple',
  Picking: 'badge-teal',
  TransferredToTransport: 'badge-blue',
  DeliveringByTransport: 'badge-blue',
  Delivering: 'badge-green',
  AwaitingPickup: 'badge-orange',
  Completed: 'badge-green',
  Cancelled: 'badge-red',
}

const VAT_PCT: Record<string, number> = { Vat0: 0, Vat5: 5, Vat7: 7, Vat10: 10, Vat22: 22 }
const FINAL_STATUSES = ['Completed', 'Cancelled']
const LOCKED_STATUSES = ['DeliveringByTransport', 'Delivering', 'AwaitingPickup', 'Completed', 'Cancelled']

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

interface OrderDetailPanelProps {
  order: OdpOrder
  users?: Array<{ id: string; displayName: string }>
  canEditOrders: boolean
  canClosePicking: boolean
  onClose: () => void
  onEdit: () => void
  onDelete: () => void
  onTransition: (action: 'confirm' | 'cancel' | 'next-status') => void
  onChecklistClosed: (orderId: string) => void
}

export function OrderDetailPanel({
  order,
  users,
  canEditOrders,
  canClosePicking,
  onClose,
  onEdit,
  onDelete,
  onTransition,
  onChecklistClosed,
}: OrderDetailPanelProps) {
  const [activeTab, setActiveTab] = useState<'positions' | 'documents' | 'assembly'>('positions')

  const shortageLines = order.lines.filter(l => (l.availableQty ?? Number.POSITIVE_INFINITY) < l.qty)
  const hasShortage = shortageLines.length > 0

  const [checkedLines, setCheckedLines] = useState<Set<string>>(
    order.isAssemblyChecklistClosed ? new Set(shortageLines.map(l => l.id)) : new Set()
  )
  const [closingChecklist, setClosingChecklist] = useState(false)

  const checkedCount = shortageLines.filter(l => checkedLines.has(l.id)).length
  const allLocalChecked = shortageLines.length > 0 && shortageLines.every(l => checkedLines.has(l.id))

  const toggleLine = (lineId: string) => {
    if (!canClosePicking || order.isAssemblyChecklistClosed) return
    setCheckedLines(prev => {
      const next = new Set(prev)
      if (next.has(lineId)) next.delete(lineId)
      else next.add(lineId)
      return next
    })
  }

  const handleCloseChecklist = async () => {
    if (!canClosePicking) { toast('error', 'Недостаточно прав для закрытия чеклиста'); return }
    setClosingChecklist(true)
    try {
      await api.post(`/orders/${order.id}/assembly-checklist/close`, {})
      toast('success', 'Чеклист сборки закрыт')
      onChecklistClosed(order.id)
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка закрытия чеклиста')
    } finally {
      setClosingChecklist(false)
    }
  }

  const isFinal = FINAL_STATUSES.includes(order.status)
  const isLocked = LOCKED_STATUSES.includes(order.status)
  const isDraft = order.status === 'Draft'
  const nextLabel = order.status === 'Picking' ? '→ Готов к получению'
    : order.status === 'Manufacturing' ? '→ Собирается'
    : order.status === 'Processing' ? '→ Ожидает оплаты'
    : 'Следующий статус →'

  const pickerName = order.pickerId ? (users?.find(u => u.id === order.pickerId)?.displayName ?? null) : null
  const driverName = order.driverId ? (users?.find(u => u.id === order.driverId)?.displayName ?? null) : null

  return (
    <div className="odp-overlay">
      {/* ── Header ── */}
      <div className="odp-header">
        <button className="odp-back-btn" onClick={onClose} title="Назад">
          <ChevronLeft size={18} />
        </button>

        <div className="odp-header-title">
          <span className="odp-title">Заказ №{order.number ?? '—'}</span>
          <span className="odp-title-date">— {fmtDateShort(order.date)}</span>
        </div>

        <span className={`badge ${STATUS_BADGE[order.status] ?? 'badge-gray'}`} style={{ flexShrink: 0 }}>
          {STATUS_LABELS[order.status] ?? order.status}
        </span>

        <div className="odp-header-actions">
          {canEditOrders && !isFinal && (
            <button className="btn btn-secondary btn-sm" onClick={() => onTransition('cancel')}>
              ✕ Отменить
            </button>
          )}
          {canEditOrders && isDraft && (
            <button className="btn btn-primary btn-sm" onClick={() => onTransition('confirm')}>
              Подтвердить
            </button>
          )}
          {canEditOrders && !isFinal && !isDraft && (
            <button className="btn btn-primary btn-sm" onClick={() => onTransition('next-status')}>
              {nextLabel}
            </button>
          )}
          {canEditOrders && !isLocked && (
            <button className="btn btn-secondary btn-sm" onClick={onEdit} style={{ marginLeft: 6 }}>
              Редактировать
            </button>
          )}
          {canEditOrders && (
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
              <div className="odp-section-label">Коротко о заказе</div>
              <div className="odp-info-grid">
                <div className="odp-field">
                  <div className="odp-field-label">Организация</div>
                  <div className="odp-field-value">{order.company?.name ?? <span className="odp-field-muted">—</span>}</div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Дата</div>
                  <div className="odp-field-value">{fmtDate(order.date)}</div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Режим НДС</div>
                  <div className="odp-field-value odp-field-muted">В сумме</div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Контрагент</div>
                  <div className="odp-field-value">{order.counterparty?.name ?? <span className="odp-field-muted">—</span>}</div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Банковский счёт</div>
                  <div className="odp-field-value">
                    {order.bankAccount ? order.bankAccount : <span className="odp-field-muted">—</span>}
                  </div>
                </div>
                <div className="odp-field">
                  <div className="odp-field-label">Договор</div>
                  <div className="odp-field-value">
                    {order.contract ? order.contract : <span className="odp-field-muted">—</span>}
                  </div>
                </div>
              </div>
            </div>

            {/* Tabs */}
            <div className="odp-tabs">
              <button
                className={`odp-tab${activeTab === 'positions' ? ' active' : ''}`}
                onClick={() => setActiveTab('positions')}
              >
                <ShoppingCart size={13} />
                Позиции ({order.lines.length})
              </button>
              <button
                className={`odp-tab${activeTab === 'documents' ? ' active' : ''}`}
                onClick={() => setActiveTab('documents')}
              >
                <FileText size={13} />
                Документы
              </button>
              {hasShortage && (
                <button
                  className={`odp-tab${activeTab === 'assembly' ? ' active' : ''}`}
                  onClick={() => setActiveTab('assembly')}
                >
                  <ClipboardCheck size={13} />
                  Сборка
                  {order.isAssemblyChecklistClosed && (
                    <CheckCircle2 size={13} style={{ color: 'hsl(142,72%,35%)' }} />
                  )}
                </button>
              )}
            </div>

            {/* ── Positions tab ── */}
            {activeTab === 'positions' && (
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
                      {order.lines.map((l, i) => {
                        const disc = l.discountType === 'None' ? '—'
                          : l.discountType === 'Percent' ? `${l.discountValue}%`
                          : fmt(l.discountValue)
                        const vatPct = VAT_PCT[l.vatRateSnapshot] ?? 0
                        return (
                          <tr key={l.id}>
                            <td style={{ opacity: .45, fontSize: 12 }}>{i + 1}</td>
                            <td style={{ fontWeight: 500 }}>{l.nameSnapshot}</td>
                            <td style={{ textAlign: 'right' }}>{l.qty} шт.</td>
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

                {order.comment && (
                  <div className="odp-note">
                    <div className="odp-note-label">Заметка (служебная)</div>
                    <div className="odp-note-text">{order.comment}</div>
                  </div>
                )}
                {!order.comment && (
                  <div className="odp-note">
                    <div className="odp-note-label">Заметка (служебная)</div>
                    <div className="odp-note-text odp-field-muted">Нет заметок</div>
                  </div>
                )}
              </div>
            )}

            {/* ── Documents tab ── */}
            {activeTab === 'documents' && (
              <div className="odp-tab-content odp-empty-tab">
                <FileText size={36} style={{ opacity: .2 }} />
                <span>Документы не прикреплены</span>
              </div>
            )}

            {/* ── Assembly tab ── */}
            {activeTab === 'assembly' && (
              <div className="odp-tab-content">
                <div className="odp-assembly-header">
                  <div>
                    <div className="odp-assembly-title">Список сборки</div>
                    <div className="odp-assembly-sub">
                      Собрано: {order.isAssemblyChecklistClosed ? shortageLines.length : checkedCount} из {shortageLines.length}
                    </div>
                  </div>
                  {(order.isAssemblyChecklistClosed || allLocalChecked) && (
                    <span className="odp-assembly-done">
                      <CheckCircle2 size={14} />
                      Все товары собраны
                    </span>
                  )}
                </div>

                <div className="odp-assembly-list">
                  {shortageLines.map(l => {
                    const checked = order.isAssemblyChecklistClosed || checkedLines.has(l.id)
                    return (
                      <label
                        key={l.id}
                        className={`odp-assembly-item${checked ? ' checked' : ''}${!canClosePicking || order.isAssemblyChecklistClosed ? ' readonly' : ''}`}
                      >
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={() => toggleLine(l.id)}
                          disabled={!canClosePicking || order.isAssemblyChecklistClosed}
                        />
                        <span className="odp-assembly-name">
                          {l.nameSnapshot} — {l.qty} шт.
                        </span>
                      </label>
                    )
                  })}
                </div>

                {canClosePicking && !order.isAssemblyChecklistClosed && (
                  <div style={{ marginTop: 20 }}>
                    <button
                      className="btn btn-primary"
                      onClick={handleCloseChecklist}
                      disabled={closingChecklist}
                    >
                      {closingChecklist ? 'Сохранение…' : 'Закрыть чеклист сборки'}
                    </button>
                  </div>
                )}

                {!canClosePicking && !order.isAssemblyChecklistClosed && (
                  <div style={{ marginTop: 16, fontSize: 13, color: 'hsl(var(--muted-foreground))' }}>
                    Закрыть чеклист может только сборщик (в статусе Собирается) или менеджер заказов.
                  </div>
                )}
              </div>
            )}

          </div>
        </div>

        {/* ── Right Sidebar ── */}
        <div className="odp-sidebar">
          <div className="odp-sidebar-section">
            <div className="odp-sidebar-title">Детали</div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Тип доставки</div>
              <div className="odp-sidebar-value odp-sidebar-muted">Самовывоз</div>
            </div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Адрес доставки</div>
              <div className={order.deliveryAddress ? 'odp-sidebar-value' : 'odp-sidebar-value odp-sidebar-muted'}>
                {order.deliveryAddress ? order.deliveryAddress : '—'}
              </div>
            </div>
            <div className="odp-sidebar-checkbox-group" style={{ marginTop: 4 }}>
              <label className="odp-sidebar-checkbox">
                <input type="checkbox" disabled defaultChecked={false} />
                Приоритетный
              </label>
              <label className="odp-sidebar-checkbox">
                <input type="checkbox" disabled defaultChecked={false} />
                Длинная сборка
              </label>
            </div>
          </div>

          <div className="odp-sidebar-section">
            <div className="odp-sidebar-title">Исполнители</div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Сборщик</div>
              <div className={pickerName ? 'odp-sidebar-value' : 'odp-sidebar-value odp-sidebar-muted'}>
                {pickerName ?? 'Не назначен'}
              </div>
            </div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Водитель</div>
              <div className={driverName ? 'odp-sidebar-value' : 'odp-sidebar-value odp-sidebar-muted'}>
                {driverName ?? 'Не назначен'}
              </div>
            </div>
          </div>

          <div className="odp-sidebar-divider" />

          <div className="odp-totals">
            <div className="odp-totals-row">
              <span>Кол-во позиций</span>
              <span>{order.lines.length}</span>
            </div>
            <div className="odp-totals-row">
              <span>Промежуточный итог</span>
              <span>{fmt(order.totalNet)}</span>
            </div>
            <div className="odp-totals-row">
              <span>Сумма НДС</span>
              <span>{fmt(order.totalVat)}</span>
            </div>
            <div className="odp-totals-row odp-totals-final">
              <span>Итого</span>
              <span>{fmt(order.totalGross)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
