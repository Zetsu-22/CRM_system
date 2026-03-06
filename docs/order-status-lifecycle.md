# Order Status Lifecycle

## Статусы заказа (SalesOrder)

| Код | Имя | Отображение | Группа (канбан) |
|-----|-----|-------------|-----------------|
| 0 | `Draft` | Черновик | Черновик |
| 1 | `Processing` | Обрабатывается | Обрабатывается |
| 2 | `WaitingForPayment` | Ожидает оплаты | В работе |
| 3 | `BillConfirmed` | Счёт подтверждён | В работе |
| 4 | `Manufacturing` | Изготавливается | В работе |
| 5 | `Picking` | Собирается | В работе |
| 6 | `TransferredToTransport` | Передан в ТК | В работе |
| 7 | `DeliveringByTransport` | Доставляется ТК | В работе |
| 8 | `Delivering` | Доставляется | В работе |
| 9 | `AwaitingPickup` | Готов к получению | В работе |
| 10 | `Completed` | Получен | Получено ✓ |
| 11 | `Cancelled` | Отменён | Отменено ✗ |

## Схема переходов

### Базовая линейка (без учёта типа доставки)
```
Draft(0) → Processing(1) → WaitingForPayment(2) → BillConfirmed(3) → Manufacturing(4)
  → Picking(5) → TransferredToTransport(6) → DeliveringByTransport(7) → Delivering(8)
  → AwaitingPickup(9) → Completed(10)

Любой не-финальный → Cancelled(11)
```

### Пути по типу доставки (параметр `deliveryType`)

| Тип доставки | После `Picking` | Пропускаемые статусы |
|---|---|---|
| `selfPickup` — Самовывоз | → `AwaitingPickup(9)` | 6, 7, 8 |
| `companyDelivery` — Доставка магазином | → `Delivering(8)` | 6, 7 |
| `transportCompany` — Транспортная компания | → `TransferredToTransport(6)` → `DeliveringByTransport(7)` → `AwaitingPickup(9)` | 8 |

## API Endpoints

| Endpoint | Из | В | Описание |
|---|---|---|---|
| `POST /api/v1/orders/{id}/confirm` | `Draft` | `Processing` | Подтвердить черновик |
| `POST /api/v1/orders/{id}/next-status?deliveryType=...` | Любой не-финальный | Следующий по логике | Продвинуть на следующий шаг |
| `POST /api/v1/orders/{id}/cancel` | Любой не-финальный | `Cancelled` | Отменить заказ |

### Параметр `deliveryType` (опциональный)
- `selfPickup` — самовывоз
- `companyDelivery` — доставка силами магазина
- `transportCompany` — транспортная компания

Если `deliveryType` не передан — используется базовая линейная последовательность.

## Правила редактирования (`PUT /api/v1/orders/{id}`)

Редактирование **заблокировано** начиная с момента передачи в доставку:

| Статус | Редактирование |
|--------|----------------|
| `Draft` … `Picking` (0–5) | ✅ Разрешено |
| `TransferredToTransport` (6) | ✅ Разрешено (ещё не у перевозчика) |
| `DeliveringByTransport` (7) | ❌ **Заблокировано** |
| `Delivering` (8) | ❌ **Заблокировано** |
| `AwaitingPickup` (9) | ❌ **Заблокировано** |
| `Completed` (10) | ❌ **Заблокировано** |
| `Cancelled` (11) | ❌ **Заблокировано** |

При попытке редактировать заблокированный заказ возвращается `409 Conflict`:
```json
{
  "error": {
    "code": "invalid_state",
    "message": "Нельзя редактировать заказ в статусе 'Delivering'. Редактирование заблокировано после передачи в доставку."
  }
}
```

## Создание заказа

- `POST /api/v1/orders` — статус всегда автоматически устанавливается в `Draft(0)`.
- Поле `status` в теле запроса не принимается.

## Финальные статусы

`Completed(10)` и `Cancelled(11)` — финальные, переходы из них невозможны.

---

## Статусы других документов

### Invoice (Счёт)

```
Draft(1) ──► Issued(2) ──► Closed(3)
  │               │
  ▼               ▼
Cancelled(4)   Cancelled(4)
```

### UPD (УПД)

```
Draft(1) ──► Registered(2) ──► Completed(3)
  │               │
  ▼               ▼
Cancelled(4)   Cancelled(4)
```

### Payment (Платёж)

```
Draft(1) ──► Posted(2)
  │               │
  ▼               ▼
Cancelled(3)   Cancelled(3)
```
