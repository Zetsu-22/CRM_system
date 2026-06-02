import { useState } from 'react'
import { Search, X, ChevronDown, ChevronUp } from 'lucide-react'
import type { OrderSearchParams } from '../api/hooks/useOrders'

// Статусы для выбора в фильтре
const STATUS_OPTIONS: Array<{ value: string; label: string }> = [
  { value: 'Draft',                label: 'Черновик' },
  { value: 'Processing',           label: 'Обрабатывается' },
  { value: 'WaitingForPayment',    label: 'Ожидает оплаты' },
  { value: 'BillConfirmed',        label: 'Счёт подтверждён' },
  { value: 'Manufacturing',        label: 'Изготавливается' },
  { value: 'Picking',              label: 'Собирается' },
  { value: 'TransferredToTransport', label: 'Передан в ТК' },
  { value: 'DeliveringByTransport', label: 'Доставляется ТК' },
  { value: 'Delivering',           label: 'Доставляется' },
  { value: 'AwaitingPickup',       label: 'Готов к получению' },
  { value: 'Completed',            label: 'Получен' },
  { value: 'Cancelled',            label: 'Отменён' },
]

interface Props {
  params: OrderSearchParams
  onChange: (p: OrderSearchParams) => void
}

export function OrderFiltersPanel({ params, onChange }: Props) {
  const [expanded, setExpanded] = useState(false)

  const hasActiveFilters =
    !!params.search ||
    (params.statuses?.length ?? 0) > 0 ||
    !!params.counterpartyId ||
    !!params.dateFrom || !!params.dateTo ||
    params.totalFrom != null || params.totalTo != null

  const clearAll = () => onChange({})

  const toggleStatus = (val: string) => {
    const cur = params.statuses ?? []
    onChange({
      ...params,
      statuses: cur.includes(val) ? cur.filter(s => s !== val) : [...cur, val],
    })
  }

  return (
    <div style={{ marginBottom: 12 }}>
      {/* Строка поиска + кнопка расширенных фильтров */}
      <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
        <div style={{ position: 'relative', flex: 1, maxWidth: 360 }}>
          <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'hsl(var(--muted-foreground))' }} />
          <input
            type="text"
            placeholder="Поиск по номеру, контрагенту, комментарию..."
            value={params.search ?? ''}
            onChange={e => onChange({ ...params, search: e.target.value || undefined })}
            style={{
              width: '100%', padding: '7px 10px 7px 32px',
              border: '1px solid hsl(var(--border))', borderRadius: 8,
              fontSize: 13, background: 'hsl(var(--background))',
              color: 'hsl(var(--foreground))',
            }}
          />
        </div>
        <button
          className={`btn btn-secondary btn-sm`}
          onClick={() => setExpanded(e => !e)}
          style={{ display: 'flex', alignItems: 'center', gap: 4 }}
        >
          Фильтры {hasActiveFilters && <span style={{ background: 'hsl(var(--primary))', color: '#fff', borderRadius: '50%', width: 16, height: 16, fontSize: 10, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>{countActiveFilters(params)}</span>}
          {expanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
        </button>
        {hasActiveFilters && (
          <button className="btn btn-secondary btn-sm" onClick={clearAll} title="Сбросить все фильтры">
            <X size={14} />
          </button>
        )}
      </div>

      {/* Расширенные фильтры */}
      {expanded && (
        <div style={{
          marginTop: 8, padding: '12px 16px',
          border: '1px solid hsl(var(--border))', borderRadius: 8,
          background: 'hsl(var(--card))',
          display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: 12,
        }}>
          {/* Статусы */}
          <div>
            <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: '0.07em', textTransform: 'uppercase', color: 'hsl(var(--muted-foreground))', marginBottom: 6 }}>
              Статус
            </div>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
              {STATUS_OPTIONS.map(opt => (
                <button
                  key={opt.value}
                  onClick={() => toggleStatus(opt.value)}
                  style={{
                    padding: '3px 8px', fontSize: 12, borderRadius: 12,
                    border: '1px solid hsl(var(--border))', cursor: 'pointer',
                    background: params.statuses?.includes(opt.value) ? 'hsl(var(--primary))' : 'transparent',
                    color: params.statuses?.includes(opt.value) ? '#fff' : 'hsl(var(--foreground))',
                    transition: 'all 0.12s',
                  }}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          {/* Диапазон дат */}
          <div>
            <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: '0.07em', textTransform: 'uppercase', color: 'hsl(var(--muted-foreground))', marginBottom: 6 }}>
              Дата заказа
            </div>
            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
              <input
                type="date"
                value={params.dateFrom ?? ''}
                onChange={e => onChange({ ...params, dateFrom: e.target.value || undefined })}
                style={{ flex: 1, padding: '5px 8px', border: '1px solid hsl(var(--border))', borderRadius: 6, fontSize: 12, background: 'hsl(var(--background))', color: 'hsl(var(--foreground))' }}
              />
              <span style={{ color: 'hsl(var(--muted-foreground))', fontSize: 12 }}>—</span>
              <input
                type="date"
                value={params.dateTo ?? ''}
                onChange={e => onChange({ ...params, dateTo: e.target.value || undefined })}
                style={{ flex: 1, padding: '5px 8px', border: '1px solid hsl(var(--border))', borderRadius: 6, fontSize: 12, background: 'hsl(var(--background))', color: 'hsl(var(--foreground))' }}
              />
            </div>
          </div>

          {/* Диапазон суммы */}
          <div>
            <div style={{ fontSize: 11, fontWeight: 700, letterSpacing: '0.07em', textTransform: 'uppercase', color: 'hsl(var(--muted-foreground))', marginBottom: 6 }}>
              Сумма итого (₽)
            </div>
            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
              <input
                type="number"
                placeholder="От"
                value={params.totalFrom ?? ''}
                onChange={e => onChange({ ...params, totalFrom: e.target.value ? Number(e.target.value) : undefined })}
                style={{ flex: 1, padding: '5px 8px', border: '1px solid hsl(var(--border))', borderRadius: 6, fontSize: 12, background: 'hsl(var(--background))', color: 'hsl(var(--foreground))' }}
              />
              <span style={{ color: 'hsl(var(--muted-foreground))', fontSize: 12 }}>—</span>
              <input
                type="number"
                placeholder="До"
                value={params.totalTo ?? ''}
                onChange={e => onChange({ ...params, totalTo: e.target.value ? Number(e.target.value) : undefined })}
                style={{ flex: 1, padding: '5px 8px', border: '1px solid hsl(var(--border))', borderRadius: 6, fontSize: 12, background: 'hsl(var(--background))', color: 'hsl(var(--foreground))' }}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function countActiveFilters(p: OrderSearchParams): number {
  let n = 0
  if (p.search) n++
  if (p.statuses?.length) n++
  if (p.counterpartyId) n++
  if (p.dateFrom || p.dateTo) n++
  if (p.totalFrom != null || p.totalTo != null) n++
  return n
}
