import { useState, useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Trash2 } from 'lucide-react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { OrderDetailPanel } from '../components/OrderDetailPanel'
import { OrderUpsertPanel } from '../components/OrderUpsertPanel'
import { toast } from '../components/Toast'
import { api } from '../api'
import {
  useOrders,
  useOrder,
  useCreateOrder,
  useUpdateOrder,
  useDeleteOrder,
  useOrderTransition,
} from '../api/hooks/useOrders'
import { useCounterparties } from '../api/hooks/useCounterparties'
import { useItems } from '../api/hooks/useItems'
import { useAuth } from '../AuthContext'
import { useOrganizations, type OrgItem } from '../OrganizationContext'
import { QK } from '../api/queryKeys'
import '../components/common.css'

// ── Локальные типы (для форм и внутреннего использования) ──────────────────

interface Counterparty { id: string; name: string }
interface Item { id: string; name: string; sku: string; unit: string; vatRate: string }
interface UserForOrders { id: string; displayName: string; isOrderPicker: boolean; isDriver: boolean }

interface OrderLine {
  id: string; itemId: string; nameSnapshot: string; vatRateSnapshot: string
  qty: number; unitPrice: number; discountType: string; discountValue: number
  net: number; vat: number; gross: number; availableQty?: number
}

interface Order {
  id: string; number: string | null; date: string
  organization: OrgItem | null; counterparty: Counterparty | null
  status: string; totalNet: number; totalVat: number; totalGross: number
  comment: string | null; isAssemblyChecklistClosed: boolean
  pickingChecklistJson?: string | null; lines: OrderLine[]
  rowVersion?: number; pickerId?: string | null; driverId?: string | null
  bankAccount?: string | null; contract?: string | null; deliveryAddress?: string | null
}

type ApiOrder = {
  id: string; number: string | null; date: string
  companyId: string; companyName: string | null
  organizationId: string; organizationName: string | null
  counterpartyId: string; counterpartyName: string | null
  status: string; comment: string | null
  pickerId: string | null; driverId: string | null
  bankAccount: string | null; contract: string | null; deliveryAddress: string | null
  totalNet: number; totalVat: number; totalGross: number
  isAssemblyChecklistClosed: boolean; pickingChecklistJson?: string | null
  rowVersion: number
  lines: Array<{
    id: string; itemId: string; itemName: string | null; nameSnapshot: string
    vatRateSnapshot: string; qty: number; unitPrice: number
    discountType: string; discountValue: number
    net: number; vat: number; gross: number; warehouseId: string | null; availableQty: number
  }>
}

// ── Статусы ────────────────────────────────────────────────────────────────

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Черновик', Processing: 'Обрабатывается', WaitingForPayment: 'Ожидает оплаты',
  BillConfirmed: 'Счёт подтверждён', Manufacturing: 'Изготавливается', Picking: 'Собирается',
  TransferredToTransport: 'Передан в ТК', DeliveringByTransport: 'Доставляется ТК',
  Delivering: 'Доставляется', AwaitingPickup: 'Готов к получению',
  Completed: 'Получен', Cancelled: 'Отменён',
}
const STATUS_BADGE: Record<string, string> = {
  Draft: 'badge-gray', Processing: 'badge-blue', WaitingForPayment: 'badge-orange',
  BillConfirmed: 'badge-green', Manufacturing: 'badge-purple', Picking: 'badge-teal',
  TransferredToTransport: 'badge-blue', DeliveringByTransport: 'badge-blue',
  Delivering: 'badge-green', AwaitingPickup: 'badge-orange',
  Completed: 'badge-green', Cancelled: 'badge-red',
}
const FINAL_STATUSES = ['Completed', 'Cancelled']
const LOCKED_STATUSES = ['DeliveringByTransport', 'Delivering', 'AwaitingPickup', 'Completed', 'Cancelled']

const fmt = (n: number) => n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₽'
const emptyLine = () => ({ itemId: '', nameSnapshot: '', vatRateSnapshot: 'Vat0', qty: 1, unitPrice: 0, discountType: 'None', discountValue: 0 })

// ── Маппинг ApiOrder → Order ───────────────────────────────────────────────

function mapApiOrder(o: ApiOrder): Order {
  return {
    id: o.id, number: o.number, date: o.date,
    organization: o.organizationId ? { id: o.organizationId, name: o.organizationName ?? '—', address: null, isDefault: false } : null,
    counterparty: o.counterpartyId ? { id: o.counterpartyId, name: o.counterpartyName ?? '—' } : null,
    status: o.status,
    pickerId: o.pickerId, driverId: o.driverId, bankAccount: o.bankAccount,
    contract: o.contract, deliveryAddress: o.deliveryAddress,
    totalNet: o.totalNet, totalVat: o.totalVat, totalGross: o.totalGross,
    comment: o.comment, isAssemblyChecklistClosed: !!o.isAssemblyChecklistClosed,
    pickingChecklistJson: o.pickingChecklistJson ?? null, rowVersion: o.rowVersion,
    lines: (o.lines ?? []).map(l => ({
      id: l.id, itemId: l.itemId,
      nameSnapshot: l.nameSnapshot || l.itemName || '',
      vatRateSnapshot: l.vatRateSnapshot,
      qty: l.qty, unitPrice: l.unitPrice, discountType: l.discountType,
      discountValue: l.discountValue, net: l.net, vat: l.vat, gross: l.gross,
      availableQty: l.availableQty,
    }))
  }
}

// ── Компонент страницы ────────────────────────────────────────────────────

export function OrdersPage() {
  const { profile } = useAuth()
  const { organizations, filterString } = useOrganizations()
  const qc = useQueryClient()

  // ── Модальное состояние ────────────────────────────────────────────────
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | 'view' | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [selectedForDelete, setSelectedForDelete] = useState<Order | null>(null)

  const [form, setForm] = useState({
    number: '', date: new Date().toISOString().slice(0, 10),
    organizationId: '', counterpartyId: '', comment: '',
    bankAccount: '', contract: '',
    deliveryType: 'selfPickup', isPriority: false, isLongPicking: false, deliveryAddress: '',
    pickerId: '', driverId: '',
    lines: [emptyLine()],
  })
  const [selectedRowVersion, setSelectedRowVersion] = useState<number | undefined>()

  // ── Права доступа ──────────────────────────────────────────────────────
  const hasFullAccess = !!(profile?.isCompanyOwner || profile?.isBasicProfile)
  const canViewOrders = !!(hasFullAccess || profile?.isAccessOrdersEdit || profile?.isOrderPicker || profile?.isExpeditor || profile?.isDriver)
  const canEditOrders = !!(hasFullAccess || profile?.isAccessOrdersEdit || profile?.isExpeditor)

  // ── Queries ────────────────────────────────────────────────────────────
  const ordersQuery = useOrders(filterString ?? undefined)
  const orders: Order[] = (ordersQuery.data as ApiOrder[] | undefined ?? []).map(mapApiOrder)

  // Полный заказ для просмотра/редактирования
  const orderDetailQuery = useOrder(modal === 'view' || modal === 'edit' ? selectedId : null)
  const selectedOrder = orderDetailQuery.data ? mapApiOrder(orderDetailQuery.data as unknown as ApiOrder) : null

  // Вспомогательные данные
  const cpQuery = useCounterparties(filterString ?? undefined)
  const counterparties = (cpQuery.data ?? []) as Counterparty[]

  const itemsQuery = useItems(filterString ?? undefined)
  const items = (itemsQuery.data ?? []) as Item[]

  // Пользователи для назначения (отдельный эндпоинт, некритичен)
  const [users, setUsers] = useState<UserForOrders[]>([])
  useEffect(() => {
    api.get<UserForOrders[]>('/users/for-orders').then(setUsers).catch(() => {})
  }, [])

  // ── Mutations ──────────────────────────────────────────────────────────
  const createOrder  = useCreateOrder()
  const updateOrder  = useUpdateOrder()
  const deleteOrder  = useDeleteOrder()
  const transitionMutation = useOrderTransition()

  // ── Handlers ──────────────────────────────────────────────────────────
  const loading  = ordersQuery.isLoading
  const saving   = createOrder.isPending || updateOrder.isPending || deleteOrder.isPending

  const canClosePickingForOrder = (order: Order) =>
    canEditOrders || (!!(profile?.isOrderPicker) && order.status === 'Picking')

  const closeModal = () => { setModal(null); setSelectedId(null); setSelectedForDelete(null) }

  const openCreate = () => {
    if (!canEditOrders) { toast('error', 'Недостаточно прав для создания заказа'); return }
    setForm({
      number: '', date: new Date().toISOString().slice(0, 10),
      organizationId: organizations[0]?.id ?? '', counterpartyId: counterparties[0]?.id ?? '',
      comment: '', bankAccount: '', contract: '',
      deliveryType: 'selfPickup', isPriority: false, isLongPicking: false, deliveryAddress: '',
      pickerId: '', driverId: '', lines: [emptyLine()],
    })
    setModal('create')
  }

  const openEdit = (o: Order) => {
    if (!canEditOrders) { toast('error', 'Недостаточно прав для редактирования заказа'); return }
    if (LOCKED_STATUSES.includes(o.status)) { toast('error', 'Редактирование недоступно: заказ уже в доставке или завершён'); return }
    setSelectedId(o.id)
    setSelectedRowVersion(o.rowVersion)
    setModal('edit')
  }

  // Эффект: когда данные для edit-режима загружены — заполняем форму
  const [editFormFilled, setEditFormFilled] = useState<string | null>(null)
  if (modal === 'edit' && selectedOrder && editFormFilled !== selectedOrder.id) {
    setEditFormFilled(selectedOrder.id)
    setForm({
      number: selectedOrder.number ?? '', date: selectedOrder.date.slice(0, 10),
      organizationId: selectedOrder.organization?.id ?? '',
      counterpartyId: selectedOrder.counterparty?.id ?? '',
      comment: selectedOrder.comment ?? '', bankAccount: selectedOrder.bankAccount ?? '',
      contract: selectedOrder.contract ?? '',
      deliveryType: 'selfPickup', isPriority: false, isLongPicking: false,
      deliveryAddress: selectedOrder.deliveryAddress ?? '',
      pickerId: selectedOrder.pickerId ?? '', driverId: selectedOrder.driverId ?? '',
      lines: selectedOrder.lines.map(l => ({
        itemId: l.itemId, nameSnapshot: l.nameSnapshot, vatRateSnapshot: l.vatRateSnapshot,
        qty: l.qty, unitPrice: l.unitPrice, discountType: l.discountType, discountValue: l.discountValue,
      })),
    })
  }

  const openView = (o: Order) => {
    setSelectedId(o.id)
    setModal('view')
  }

  const openDelete = (o: Order) => {
    if (!canEditOrders) { toast('error', 'Недостаточно прав для удаления заказа'); return }
    setSelectedForDelete(o); setModal('delete')
  }

  const setLine = (i: number, k: string, v: string | number) =>
    setForm(prev => {
      const lines = [...prev.lines]
      lines[i] = { ...lines[i], [k]: v }
      if (k === 'itemId') {
        const item = items.find(x => x.id === v)
        if (item) lines[i] = { ...lines[i], nameSnapshot: item.name, vatRateSnapshot: item.vatRate }
      }
      return { ...prev, lines }
    })

  const addLine = () => setForm(prev => ({ ...prev, lines: [...prev.lines, emptyLine()] }))
  const removeLine = (i: number) => setForm(prev => ({ ...prev, lines: prev.lines.filter((_, idx) => idx !== i) }))

  const buildBody = (rowVersion?: number) => ({
    number: form.number || null, date: form.date,
    organizationId: form.organizationId, counterpartyId: form.counterpartyId,
    comment: form.comment || null, pickerId: form.pickerId || null,
    driverId: form.driverId || null, bankAccount: form.bankAccount || null,
    contract: form.contract || null, deliveryAddress: form.deliveryAddress || null,
    rowVersion: rowVersion ?? null,
    lines: form.lines.map(l => ({
      itemId: l.itemId, nameSnapshot: l.nameSnapshot, vatRateSnapshot: l.vatRateSnapshot,
      qty: Number(l.qty), unitPrice: Number(l.unitPrice),
      discountType: l.discountType, discountValue: Number(l.discountValue), warehouseId: null,
    })),
  })

  const transition = async (id: string, action: 'confirm' | 'cancel' | 'next-status'): Promise<boolean> => {
    if (!canEditOrders) { toast('error', 'Недостаточно прав для изменения статуса'); return false }
    const labels: Record<string, string> = { confirm: 'подтверждён', cancel: 'отменён', 'next-status': 'обновлён' }
    try {
      await transitionMutation.mutateAsync({ id, action })
      toast('success', `Статус заказа ${labels[action]}`)
      return true
    } catch (e: any) { toast('error', e.message ?? 'Ошибка'); return false }
  }

  const handleSave = async () => {
    if (!canEditOrders) { toast('error', 'Недостаточно прав для сохранения'); return }
    if (!form.organizationId) { toast('error', 'Выберите организацию'); return }
    if (!form.counterpartyId) { toast('error', 'Выберите контрагента'); return }
    if (form.lines.some(l => !l.itemId)) { toast('error', 'Выберите товар во всех строках'); return }
    try {
      if (modal === 'create') {
        await createOrder.mutateAsync(buildBody())
        toast('success', 'Заказ создан')
      } else {
        await updateOrder.mutateAsync({ id: selectedId!, body: buildBody(selectedRowVersion) })
        toast('success', 'Заказ обновлён')
      }
      closeModal()
      setEditFormFilled(null)
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
  }

  const handleDelete = async () => {
    if (!canEditOrders) { toast('error', 'Недостаточно прав для удаления'); return }
    try {
      await deleteOrder.mutateAsync(selectedForDelete!.id)
      toast('success', 'Заказ удалён')
      closeModal()
    } catch (e: any) { toast('error', e.message ?? 'Ошибка') }
  }

  const handleChecklistClosed = (orderId: string) => {
    // Инвалидируем кэш заказа — TanStack Query перезагрузит данные
    qc.invalidateQueries({ queryKey: QK.order(orderId) })
    qc.invalidateQueries({ queryKey: ['orders'] })
  }

  // ── Рендер ─────────────────────────────────────────────────────────────
  return (
    <PageLayout title="Заказы" actions={
      canEditOrders ? <button className="btn btn-primary" onClick={openCreate}>+ Создать заказ</button> : null
    }>
      <div className="tableWrap">
        {!canViewOrders ? (
          <div className="tableEmpty">Нет доступа к заказам</div>
        ) : loading ? <div className="tableLoading">Загрузка…</div> : orders.length === 0 ? (
          <div className="tableEmpty">Нет заказов</div>
        ) : (
          <table className="crmTable">
            <thead><tr>
              <th>№</th><th>Дата</th><th>Организация</th><th>Контрагент</th><th>Статус</th>
              <th>Сумма</th><th>НДС</th><th>Итого</th><th></th>
            </tr></thead>
            <tbody>
              {orders.map(o => (
                <tr key={o.id} style={{ cursor: 'pointer' }} onClick={() => openView(o)}>
                  <td>{o.number || '—'}</td>
                  <td>{o.date.slice(0, 10)}</td>
                  <td>{o.organization?.name ?? '—'}</td>
                  <td>{o.counterparty?.name ?? '—'}</td>
                  <td><span className={`badge ${STATUS_BADGE[o.status] ?? 'badge-gray'}`}>{STATUS_LABELS[o.status] ?? o.status}</span></td>
                  <td>{fmt(o.totalNet)}</td>
                  <td>{fmt(o.totalVat)}</td>
                  <td><strong>{fmt(o.totalGross)}</strong></td>
                  <td onClick={e => e.stopPropagation()}>
                    {canEditOrders && (
                      <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                        {!LOCKED_STATUSES.includes(o.status) && <button className="btn btn-secondary btn-sm" onClick={() => openEdit(o)}>Изм.</button>}
                        {o.status === 'Draft' && <button className="btn btn-primary btn-sm" onClick={() => transition(o.id, 'confirm')}>Подтв.</button>}
                        {!FINAL_STATUSES.includes(o.status) && o.status !== 'Draft' && <button className="btn btn-primary btn-sm" onClick={() => transition(o.id, 'next-status')}>Далее ›</button>}
                        {!FINAL_STATUSES.includes(o.status) && <button className="btn btn-danger btn-sm" onClick={() => transition(o.id, 'cancel')}>Отменить</button>}
                        <button className="btn btn-danger btn-sm" onClick={() => openDelete(o)}>Удал.</button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {(modal === 'create' || modal === 'edit') && (
        <OrderUpsertPanel
          mode={modal === 'create' ? 'create' : 'edit'}
          titleNumber={modal === 'edit' ? selectedOrder?.number : null}
          form={form}
          organizations={organizations}
          counterparties={counterparties}
          items={items}
          users={users}
          saving={saving || orderDetailQuery.isLoading}
          onClose={() => { closeModal(); setEditFormFilled(null) }}
          onSave={handleSave}
          onChangeForm={patch => setForm(prev => ({ ...prev, ...patch }))}
          onSetLine={setLine}
          onAddLine={addLine}
          onRemoveLine={removeLine}
        />
      )}

      {modal === 'view' && selectedId && (
        <OrderDetailPanel
          order={selectedOrder ?? { id: selectedId, number: null, date: '', organization: null, counterparty: null, status: '', totalNet: 0, totalVat: 0, totalGross: 0, comment: null, isAssemblyChecklistClosed: false, lines: [] }}
          users={users}
          canEditOrders={canEditOrders}
          canClosePicking={selectedOrder ? canClosePickingForOrder(selectedOrder) : false}
          onClose={closeModal}
          onEdit={() => { closeModal(); setTimeout(() => selectedOrder && openEdit(selectedOrder), 50) }}
          onDelete={() => { closeModal(); setTimeout(() => selectedOrder && openDelete(selectedOrder), 50) }}
          onTransition={async action => { const ok = await transition(selectedId, action); if (ok) closeModal() }}
          onChecklistClosed={handleChecklistClosed}
        />
      )}

      {modal === 'delete' && selectedForDelete && (
        <Modal title="Удалить заказ" subtitle="Это действие нельзя отменить" onClose={closeModal} size="sm">
          <div className="confirmBox">
            <div className="confirmBoxIcon"><Trash2 size={22} /></div>
            <p>Удалить заказ <strong>{selectedForDelete.number ?? selectedForDelete.id.slice(0, 8)}</strong>?</p>
            <div className="confirmActions">
              <button className="btn btn-secondary" onClick={closeModal}>Отмена</button>
              <button className="btn btn-danger" onClick={handleDelete} disabled={saving}>{saving ? 'Удаление…' : 'Удалить'}</button>
            </div>
          </div>
        </Modal>
      )}
    </PageLayout>
  )
}
