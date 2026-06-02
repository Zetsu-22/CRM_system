import { useEffect, useState } from 'react'
import { Trash2 } from 'lucide-react'
import { PageLayout } from '../components/PageLayout'
import { Modal } from '../components/Modal'
import { toast } from '../components/Toast'
import { api } from '../api'
import { useAuth } from '../AuthContext'
import { useOrganizations } from '../OrganizationContext'
import { InvoiceDetailPanel } from '../components/InvoiceDetailPanel'
import { InvoiceUpsertPanel } from '../components/InvoiceUpsertPanel'
import type { Invoice } from '../components/InvoiceDetailPanel'
import type { InvoiceFormState } from '../components/InvoiceUpsertPanel'
import '../components/common.css'

interface Counterparty { id: string; name: string }
interface Item { id: string; name: string; vatRate: string }

interface ApiInvoice {
  id: string; number: string | null; date: string
  companyId: string; organizationId: string; counterpartyId: string; orderId: string | null
  status: string; totalNet: number; totalVat: number; totalGross: number
  paidAmount: number; comment: string | null
  lines: Array<{
    id: string; itemId: string; nameSnapshot: string; vatRateSnapshot: string
    qty: number; unitPrice: number; discountType: string; discountValue: number
    net: number; vat: number; gross: number
  }>
}

const STATUS_LABELS: Record<string, string> = { Draft: 'Черновик', Issued: 'Выставлен', Closed: 'Закрыт', Cancelled: 'Отменён' }
const STATUS_BADGE: Record<string, string> = { Draft: 'badge-gray', Issued: 'badge-blue', Closed: 'badge-green', Cancelled: 'badge-red' }
const fmt = (n: number) => n.toLocaleString('ru-RU', { minimumFractionDigits: 2 }) + ' ₽'
const emptyLine = () => ({ itemId: '', nameSnapshot: '', vatRateSnapshot: 'Vat0', qty: 1, unitPrice: 0, discountType: 'None', discountValue: 0 })

export function InvoicesPage() {
  const { profile } = useAuth()
  const { organizations, filterString } = useOrganizations()
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [counterparties, setCounterparties] = useState<Counterparty[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | 'view' | null>(null)
  const [selected, setSelected] = useState<Invoice | null>(null)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState<InvoiceFormState>({
    number: '', date: new Date().toISOString().slice(0, 10),
    organizationId: '', counterpartyId: '', status: 'Draft', comment: '',
    lines: [emptyLine()]
  })

  const hasFullAccess = !!(profile?.isCompanyOwner || profile?.isBasicProfile)
  const canView = !!(hasFullAccess || profile?.isAccessFinancialJournal)
  const canEdit = !!(hasFullAccess || profile?.isAccessFinancialJournalEdit)

  const mapApiInvoice = (inv: ApiInvoice): Invoice => ({
    ...inv,
    organizationName: organizations.find(o => o.id === inv.organizationId)?.name,
    counterpartyName: counterparties.find(c => c.id === inv.counterpartyId)?.name,
    lines: inv.lines.map(l => ({ ...l, discountType: l.discountType ?? 'None', discountValue: l.discountValue ?? 0 }))
  })

  const getInvoiceDetails = async (id: string): Promise<Invoice> => {
    const dto = await api.get<ApiInvoice>(`/invoices/${id}`)
    return mapApiInvoice(dto)
  }

  const load = () => {
    setLoading(true)
    const path = filterString ? `/invoices?organizationIds=${encodeURIComponent(filterString)}` : '/invoices'
    Promise.all([
      api.get<ApiInvoice[]>(path),
      api.get<Counterparty[]>('/counterparties'),
      api.get<Item[]>('/items')
    ]).then(([inv, cp, it]) => {
      setCounterparties(cp)
      setItems(it)
      setInvoices(inv.map(i => ({
        ...i,
        organizationName: organizations.find(o => o.id === i.organizationId)?.name,
        counterpartyName: cp.find(x => x.id === i.counterpartyId)?.name,
        lines: i.lines.map(l => ({ ...l, discountType: l.discountType ?? 'None', discountValue: l.discountValue ?? 0 }))
      })))
    }).catch(() => toast('error', 'Ошибка загрузки')).finally(() => setLoading(false))
  }
  useEffect(() => { load() }, [filterString])

  const openCreate = () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для создания счёта'); return }
    setForm({
      number: '', date: new Date().toISOString().slice(0, 10),
      organizationId: organizations[0]?.id ?? '', counterpartyId: counterparties[0]?.id ?? '',
      status: 'Draft', comment: '', lines: [emptyLine()]
    })
    setModal('create')
  }

  const openEdit = async (inv: Invoice) => {
    if (!canEdit) { toast('error', 'Недостаточно прав для редактирования счёта'); return }
    setSaving(true)
    try {
      const full = await getInvoiceDetails(inv.id)
      setSelected(full)
      setForm({
        number: full.number ?? '',
        date: full.date.slice(0, 10),
        organizationId: full.organizationId,
        counterpartyId: full.counterpartyId,
        status: full.status,
        comment: full.comment ?? '',
        lines: full.lines.map(l => ({
          itemId: l.itemId,
          nameSnapshot: l.nameSnapshot,
          vatRateSnapshot: l.vatRateSnapshot,
          qty: l.qty,
          unitPrice: l.unitPrice,
          discountType: l.discountType ?? 'None',
          discountValue: l.discountValue ?? 0
        }))
      })
      setModal('edit')
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка загрузки счёта')
    } finally {
      setSaving(false)
    }
  }

  const openView = async (inv: Invoice) => {
    setSaving(true)
    try {
      const full = await getInvoiceDetails(inv.id)
      setSelected(full)
      setModal('view')
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка загрузки счёта')
    } finally {
      setSaving(false)
    }
  }

  const openDelete = (inv: Invoice) => { setSelected(inv); setModal('delete') }
  const closeModal = () => { setModal(null); setSelected(null) }

  const setLine = (i: number, k: string, v: string | number) => setForm(prev => {
    const lines = [...prev.lines]; lines[i] = { ...lines[i], [k]: v }
    if (k === 'itemId') {
      const item = items.find(x => x.id === v)
      if (item) lines[i] = { ...lines[i], nameSnapshot: item.name, vatRateSnapshot: item.vatRate }
    }
    return { ...prev, lines }
  })
  const addLine = () => setForm(prev => ({ ...prev, lines: [...prev.lines, emptyLine()] }))
  const removeLine = (i: number) => setForm(prev => ({ ...prev, lines: prev.lines.filter((_, idx) => idx !== i) }))

  const buildBody = () => ({
    number: form.number || null,
    date: form.date,
    organizationId: form.organizationId,
    counterpartyId: form.counterpartyId,
    orderId: null,
    status: form.status,
    comment: form.comment || null,
    lines: form.lines.map(l => ({
      itemId: l.itemId,
      nameSnapshot: l.nameSnapshot,
      vatRateSnapshot: l.vatRateSnapshot,
      qty: Number(l.qty),
      unitPrice: Number(l.unitPrice),
      discountType: l.discountType,
      discountValue: Number(l.discountValue)
    }))
  })

  const handleSave = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для сохранения'); return }
    if (!form.organizationId || !form.counterpartyId) { toast('error', 'Выберите организацию и контрагента'); return }
    if (form.lines.some(l => !l.itemId)) { toast('error', 'Выберите товар во всех строках'); return }
    setSaving(true)
    try {
      if (modal === 'create') {
        await api.post('/invoices', buildBody())
        toast('success', 'Счёт создан')
      } else {
        await api.put(`/invoices/${selected!.id}`, buildBody())
        toast('success', 'Счёт обновлён')
      }
      load()
      closeModal()
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!canEdit) { toast('error', 'Недостаточно прав для удаления'); return }
    setSaving(true)
    try {
      await api.delete(`/invoices/${selected!.id}`)
      toast('success', 'Счёт удалён')
      load()
      closeModal()
    } catch (e: any) {
      toast('error', e.message ?? 'Ошибка')
    } finally {
      setSaving(false)
    }
  }

  return (
    <PageLayout title="Счета" actions={canEdit ? <button className="btn btn-primary" onClick={openCreate}>+ Создать счёт</button> : null}>
      <div className="tableWrap">
        {!canView ? <div className="tableEmpty">Нет доступа к счетам</div> : loading ? <div className="tableLoading">Загрузка…</div> : invoices.length === 0 ? <div className="tableEmpty">Нет счетов</div> : (
          <table className="crmTable">
            <thead><tr><th>№</th><th>Дата</th><th>Статус</th><th>Сумма</th><th>НДС</th><th>Итого</th><th>Оплачено</th><th></th></tr></thead>
            <tbody>
              {invoices.map(inv => (
                <tr key={inv.id} style={{ cursor: 'pointer' }} onClick={() => openView(inv)}>
                  <td>{inv.number || '—'}</td>
                  <td>{inv.date.slice(0, 10)}</td>
                  <td><span className={`badge ${STATUS_BADGE[inv.status] ?? 'badge-gray'}`}>{STATUS_LABELS[inv.status] ?? inv.status}</span></td>
                  <td>{fmt(inv.totalNet)}</td>
                  <td>{fmt(inv.totalVat)}</td>
                  <td><strong>{fmt(inv.totalGross)}</strong></td>
                  <td style={{ color: inv.paidAmount >= inv.totalGross ? '#2ac36a' : '#eab308' }}>{fmt(inv.paidAmount)}</td>
                  <td onClick={e => e.stopPropagation()}>
                    {canEdit && (
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-secondary btn-sm" onClick={() => openEdit(inv)}>Изм.</button>
                        <button className="btn btn-danger btn-sm" onClick={() => openDelete(inv)}>Удал.</button>
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
        <InvoiceUpsertPanel
          mode={modal}
          titleNumber={selected?.number}
          form={form}
          organizations={organizations}
          counterparties={counterparties}
          items={items}
          saving={saving}
          onClose={closeModal}
          onSave={handleSave}
          onChangeForm={(patch) => setForm(prev => ({ ...prev, ...patch }))}
          onSetLine={setLine}
          onAddLine={addLine}
          onRemoveLine={removeLine}
        />
      )}

      {modal === 'view' && selected && (
        <InvoiceDetailPanel
          invoice={selected}
          canEdit={canEdit}
          onClose={closeModal}
          onEdit={() => openEdit(selected)}
          onDelete={() => openDelete(selected)}
        />
      )}

      {modal === 'delete' && selected && (
        <Modal title="Удалить счёт" subtitle="Это действие нельзя отменить" onClose={closeModal} size="sm">
          <div className="confirmBox">
            <div className="confirmBoxIcon"><Trash2 size={22} /></div>
            <p>Удалить счёт <strong>{selected.number ?? selected.id.slice(0, 8)}</strong>?</p>
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
