# Order Status Lifecycle

## Статусы заказа (SalesOrder)

| Значение | Код | Описание |
|----------|-----|----------|
| `Draft` | 1 | Черновик — заказ создан, редактируется |
| `Confirmed` | 2 | Подтверждён — заказ зафиксирован |
| `Fulfilled` | 5 | Выполнен — товар отгружен / услуга оказана |
| `Closed` | 3 | Закрыт — заказ завершён |
| `Cancelled` | 4 | Отменён — заказ аннулирован |

## Допустимые переходы

```
Draft ──► Confirmed ──► Fulfilled ──► Closed
  │           │                         ▲
  │           └─────────────────────────┘
  │                  (skip Fulfilled)
  ▼               ▼
Cancelled     Cancelled
```

| Из → В | Endpoint | Условие |
|--------|----------|---------|
| `Draft` → `Confirmed` | `POST /api/v1/orders/{id}/confirm` | Статус должен быть `Draft` |
| `Confirmed` → `Fulfilled` | `POST /api/v1/orders/{id}/fulfill` | Статус должен быть `Confirmed` |
| `Confirmed` → `Closed` | `POST /api/v1/orders/{id}/close` | Статус должен быть `Confirmed` или `Fulfilled` |
| `Fulfilled` → `Closed` | `POST /api/v1/orders/{id}/close` | Статус должен быть `Confirmed` или `Fulfilled` |
| `Draft` → `Cancelled` | `POST /api/v1/orders/{id}/cancel` | Статус должен быть `Draft` или `Confirmed` |
| `Confirmed` → `Cancelled` | `POST /api/v1/orders/{id}/cancel` | Статус должен быть `Draft` или `Confirmed` |

## Правила редактирования

- `PUT /api/v1/orders/{id}` — доступно **только** в статусе `Draft`.
- Попытка изменить заказ в любом другом статусе возвращает `409 Conflict`.
- При создании (`POST /api/v1/orders`) статус всегда устанавливается в `Draft` автоматически — передавать `status` в теле запроса не нужно.

## Ошибки при недопустимом переходе

```json
HTTP 409 Conflict
{
  "error": {
    "code": "invalid_transition",
    "message": "Переход Confirm возможен только из статуса Draft. Текущий статус: Confirmed."
  }
}
```

## Статусы других документов

### Invoice (Счёт)

```
Draft ──► Issued ──► Closed
  │          │
  ▼          ▼
Cancelled  Cancelled
```

| Значение | Код |
|----------|-----|
| `Draft` | 1 |
| `Issued` | 2 |
| `Closed` | 3 |
| `Cancelled` | 4 |

### UPD (УПД)

```
Draft ──► Registered ──► Completed
  │            │
  ▼            ▼
Cancelled   Cancelled
```

| Значение | Код |
|----------|-----|
| `Draft` | 1 |
| `Registered` | 2 |
| `Completed` | 3 |
| `Cancelled` | 4 |

### Payment (Платёж)

```
Draft ──► Posted
  │          │
  ▼          ▼
Cancelled  Cancelled
```

| Значение | Код |
|----------|-----|
| `Draft` | 1 |
| `Posted` | 2 |
| `Cancelled` | 3 |
