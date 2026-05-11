import { ChevronLeft, X, ShoppingCart, FileText, ClipboardCheck } from 'lucide-react'
import './OrderDetailPanel.css'
import './common.css'

interface Company { id: string; name: string }
interface Counterparty { id: string; name: string }
interface Item { id: string; name: string; sku: string; unit: string; vatRate: string }

interface UserForOrders {
  id: string
  displayName: string
  isOrderPicker: boolean
  isDriver: boolean
}

export interface OrderLineForm {
  itemId: string
  nameSnapshot: string
  vatRateSnapshot: string
  qty: number
  unitPrice: number
  discountType: string
  discountValue: number
}

export interface OrderFormState {
  number: string
  date: string
  companyId: string
  counterpartyId: string
  comment: string
  bankAccount: string
  contract: string
  deliveryType: string
  isPriority: boolean
  isLongPicking: boolean
  deliveryAddress: string
  pickerId: string
  driverId: string
  lines: OrderLineForm[]
}

const VAT_PCT: Record<string, number> = { Vat0: 0, Vat5: 5, Vat7: 7, Vat10: 10, Vat22: 22 }
const VAT_LABELS: [string, string][] = [['Vat0', '0%'], ['Vat5', '5%'], ['Vat7', '7%'], ['Vat10', '10%'], ['Vat22', '22%']]

const fmt = (n: number) =>
  n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₽'

function calcLine(l: OrderLineForm): { net: number; vat: number; gross: number } {
  const base = l.qty * l.unitPrice
  const net = l.discountType === 'Percent'
    ? base * (1 - l.discountValue / 100)
    : l.discountType === 'Amount'
    ? Math.max(0, base - l.discountValue)
    : base
  const vat = net * (VAT_PCT[l.vatRateSnapshot] ?? 0) / 100
  return { net, vat, gross: net + vat }
}

interface OrderUpsertPanelProps {
  mode: 'create' | 'edit'
  titleNumber?: string | null
  form: OrderFormState
  companies: Company[]
  counterparties: Counterparty[]
  items: Item[]
  users: UserForOrders[]
  saving: boolean
  onClose: () => void
  onSave: () => void
  onChangeForm: (patch: Partial<OrderFormState>) => void
  onSetLine: (index: number, key: string, value: string | number) => void
  onAddLine: () => void
  onRemoveLine: (index: number) => void
}

export function OrderUpsertPanel({
  mode,
  titleNumber,
  form,
  companies,
  counterparties,
  items,
  users,
  saving,
  onClose,
  onSave,
  onChangeForm,
  onSetLine,
  onAddLine,
  onRemoveLine,
}: OrderUpsertPanelProps) {
  const pageTitle = mode === 'create'
    ? 'Новый заказ'
    : `Заказ №${titleNumber ?? form.number ?? '—'}`

  const totals = form.lines.reduce(
    (acc, l) => {
      const { net, vat, gross } = calcLine(l)
      return { net: acc.net + net, vat: acc.vat + vat, gross: acc.gross + gross }
    },
    { net: 0, vat: 0, gross: 0 }
  )

  const totalQty = form.lines.reduce((s, l) => s + Number(l.qty), 0)

  return (
    <div className="odp-overlay">
      {/* ── Header ── */}
      <div className="odp-header">
        <button className="odp-back-btn" onClick={onClose} title="Назад">
          <ChevronLeft size={18} />
        </button>
        <div className="odp-header-title">
          <span className="odp-title">{pageTitle}</span>
        </div>
        <div className="odp-header-actions">
          <button className="btn btn-secondary btn-sm" onClick={onClose}>Отмена</button>
          <button className="btn btn-primary btn-sm" onClick={onSave} disabled={saving}>
            {saving ? 'Сохранение…' : 'Сохранить'}
          </button>
        </div>
        <button className="odp-close-btn" onClick={onClose} title="Закрыть">
          <X size={16} />
        </button>
      </div>

      {/* ── Body ── */}
      <div className="odp-body">
        {/* ── Main ── */}
        <div className="odp-main">
          <div className="odp-scroll">

            {/* Info grid */}
            <div className="odp-section">
              <div className="odp-section-label">Коротко о заказе</div>
              <div className="odp-info-grid">

                <div className="odp-field">
                  <div className="odp-field-label">Организация</div>
                  <div className="odp-field-value">
                    <select className="input" value={form.companyId}
                      onChange={e => onChangeForm({ companyId: e.target.value })}>
                      <option value="">—</option>
                      {companies.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                    </select>
                  </div>
                </div>

                <div className="odp-field">
                  <div className="odp-field-label">Дата</div>
                  <div className="odp-field-value">
                    <input className="input" type="date" value={form.date}
                      onChange={e => onChangeForm({ date: e.target.value })} />
                  </div>
                </div>

                <div className="odp-field">
                  <div className="odp-field-label">Режим НДС</div>
                  <div className="odp-field-value">
                    <select className="input" defaultValue="included" disabled>
                      <option value="included">В сумме</option>
                    </select>
                  </div>
                </div>

                <div className="odp-field">
                  <div className="odp-field-label">Контрагент</div>
                  <div className="odp-field-value">
                    <select className="input" value={form.counterpartyId}
                      onChange={e => onChangeForm({ counterpartyId: e.target.value })}>
                      <option value="">—</option>
                      {counterparties.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                    </select>
                  </div>
                </div>

                <div className="odp-field">
                  <div className="odp-field-label">Номер</div>
                  <div className="odp-field-value">
                    <input className="input" value={form.number} placeholder="(необязательно)"
                      onChange={e => onChangeForm({ number: e.target.value })} />
                  </div>
                </div>

                <div className="odp-field">
                  <div className="odp-field-label">Банковский счёт</div>
                  <div className="odp-field-value">
                    <input className="input" value={form.bankAccount}
                      onChange={e => onChangeForm({ bankAccount: e.target.value })}
                      placeholder="—" />
                  </div>
                </div>

                <div className="odp-field">
                  <div className="odp-field-label">Договор</div>
                  <div className="odp-field-value">
                    <input className="input" value={form.contract}
                      onChange={e => onChangeForm({ contract: e.target.value })}
                      placeholder="—" />
                  </div>
                </div>

              </div>
            </div>

            {/* Tabs */}
            <div className="odp-tabs">
              <button className="odp-tab active" type="button">
                <ShoppingCart size={13} />
                Позиции ({form.lines.length})
              </button>
              <button className="odp-tab" type="button" disabled style={{ opacity: .4, cursor: 'not-allowed' }}>
                <FileText size={13} />
                Документы
              </button>
              <button className="odp-tab" type="button" disabled style={{ opacity: .4, cursor: 'not-allowed' }}>
                <ClipboardCheck size={13} />
                Сборка
              </button>
            </div>

            {/* Positions tab */}
            <div className="odp-tab-content">
              <div className="tableWrap">
                <table className="crmTable">
                  <thead>
                    <tr>
                      <th style={{ width: 28 }}>№</th>
                      <th>Наименование</th>
                      <th style={{ width: 80, textAlign: 'right' }}>Кол-во</th>
                      <th style={{ width: 110, textAlign: 'right' }}>Цена</th>
                      <th style={{ width: 130 }}>Скидка</th>
                      <th style={{ width: 80 }}>НДС</th>
                      <th style={{ width: 110, textAlign: 'right' }}>Сумма</th>
                      <th style={{ width: 36 }} />
                    </tr>
                  </thead>
                  <tbody>
                    {form.lines.map((l, i) => {
                      const { gross } = calcLine(l)
                      const vatPct = VAT_PCT[l.vatRateSnapshot] ?? 0
                      return (
                        <tr key={i}>
                          <td style={{ opacity: .4, fontSize: 12 }}>{i + 1}</td>
                          <td>
                            <select className="input" value={l.itemId}
                              onChange={e => onSetLine(i, 'itemId', e.target.value)}>
                              <option value="">— выберите —</option>
                              {items.map(it => <option key={it.id} value={it.id}>{it.name}</option>)}
                            </select>
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            <input className="input" type="number" min={0} value={l.qty}
                              style={{ textAlign: 'right' }}
                              onChange={e => onSetLine(i, 'qty', Number(e.target.value))} />
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            <input className="input" type="number" min={0} value={l.unitPrice}
                              style={{ textAlign: 'right' }}
                              onChange={e => onSetLine(i, 'unitPrice', Number(e.target.value))} />
                          </td>
                          <td>
                            <div style={{ display: 'flex', gap: 4 }}>
                              <select className="input" value={l.discountType}
                                style={{ width: 60 }}
                                onChange={e => onSetLine(i, 'discountType', e.target.value)}>
                                <option value="None">—</option>
                                <option value="Percent">%</option>
                                <option value="Amount">₽</option>
                              </select>
                              {l.discountType !== 'None' && (
                                <input className="input" type="number" min={0}
                                  value={l.discountValue}
                                  style={{ textAlign: 'right' }}
                                  onChange={e => onSetLine(i, 'discountValue', Number(e.target.value))} />
                              )}
                            </div>
                          </td>
                          <td>
                            <select className="input" value={l.vatRateSnapshot}
                              onChange={e => onSetLine(i, 'vatRateSnapshot', e.target.value)}>
                              {VAT_LABELS.map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                            </select>
                            <span style={{ fontSize: 11, opacity: .5, marginLeft: 3 }}>{vatPct}%</span>
                          </td>
                          <td style={{ textAlign: 'right', fontWeight: 600, whiteSpace: 'nowrap' }}>
                            {fmt(gross)}
                          </td>
                          <td>
                            <button className="btn btn-danger btn-sm" type="button"
                              onClick={() => onRemoveLine(i)}>✕</button>
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>

              <div style={{ marginTop: 10 }}>
                <button className="btn btn-secondary" type="button" onClick={onAddLine}>
                  + Добавить строку
                </button>
              </div>

              {/* Comment */}
              <div className="odp-note" style={{ marginTop: 20 }}>
                <div className="odp-note-label">Заметка (служебная)</div>
                <textarea
                  className="input"
                  rows={3}
                  value={form.comment}
                  onChange={e => onChangeForm({ comment: e.target.value })}
                  placeholder="Служебная заметка по заказу"
                  style={{ resize: 'vertical', width: '100%', marginTop: 4 }}
                />
              </div>
            </div>

          </div>
        </div>

        {/* ── Sidebar ── */}
        <div className="odp-sidebar">

          {/* Детали */}
          <div className="odp-sidebar-section">
            <div className="odp-sidebar-title">Детали</div>

            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Тип доставки</div>
              <select className="input" value={form.deliveryType}
                onChange={e => onChangeForm({ deliveryType: e.target.value })}>
                <option value="selfPickup">Самовывоз</option>
                <option value="companyDelivery">Доставка магазином</option>
                <option value="transportCompany">Доставка ТК</option>
              </select>
            </div>

            {form.deliveryType !== 'selfPickup' && (
              <div className="odp-sidebar-field">
                <div className="odp-sidebar-label">Адрес доставки</div>
                <input className="input" value={form.deliveryAddress} placeholder="Введите адрес"
                  onChange={e => onChangeForm({ deliveryAddress: e.target.value })} />
              </div>
            )}

            <div className="odp-sidebar-checkbox-group" style={{ marginTop: 8 }}>
              <label className="odp-sidebar-checkbox">
                <input type="checkbox" checked={form.isPriority}
                  onChange={e => onChangeForm({ isPriority: e.target.checked })} />
                Приоритетный
              </label>
              <label className="odp-sidebar-checkbox">
                <input type="checkbox" checked={form.isLongPicking}
                  onChange={e => onChangeForm({ isLongPicking: e.target.checked })} />
                Длинная сборка
              </label>
            </div>
          </div>

          {/* Исполнители */}
          <div className="odp-sidebar-section">
            <div className="odp-sidebar-title">Исполнители</div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Сборщик</div>
              <select className="input" value={form.pickerId}
                onChange={e => onChangeForm({ pickerId: e.target.value })}>
                <option value="">Не назначен</option>
                {users.filter(u => u.isOrderPicker).map(u => (
                  <option key={u.id} value={u.id}>{u.displayName}</option>
                ))}
              </select>
            </div>
            <div className="odp-sidebar-field">
              <div className="odp-sidebar-label">Водитель</div>
              <select className="input" value={form.driverId}
                onChange={e => onChangeForm({ driverId: e.target.value })}>
                <option value="">Не назначен</option>
                {users.filter(u => u.isDriver).map(u => (
                  <option key={u.id} value={u.id}>{u.displayName}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="odp-sidebar-divider" />

          {/* Итого — live */}
          <div className="odp-totals">
            <div className="odp-totals-row">
              <span>Кол-во позиций</span>
              <span>{form.lines.length}</span>
            </div>
            <div className="odp-totals-row">
              <span>Кол-во товаров</span>
              <span>{totalQty}</span>
            </div>
            <div className="odp-totals-row">
              <span>Промежуточный итог</span>
              <span>{fmt(totals.net)}</span>
            </div>
            <div className="odp-totals-row">
              <span>Сумма НДС</span>
              <span>{fmt(totals.vat)}</span>
            </div>
            <div className="odp-totals-row odp-totals-final">
              <span>Итого</span>
              <span>{fmt(totals.gross)}</span>
            </div>
          </div>

        </div>
      </div>
    </div>
  )
}
