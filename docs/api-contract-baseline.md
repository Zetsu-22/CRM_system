# API contract — baseline (фаза 0.3)

> Источник правды на момент начала рефакторинга. Цель: при последующих изменениях не сломать публичные маршруты, ожидаемые фронтом (`crm-web`).
>
> Если маршрут меняется — фиксируется в **CHANGELOG** этого документа в самом низу.
>
> Все маршруты:
> - префикс `/api/v1`
> - требуют JWT (`Authorization: Bearer <token>`), кроме `auth/register`, `auth/login`
> - возвращают `application/json`
> - на ошибке — JSON с полем `error` (см. `DevSeedController` / `OrdersController`) либо «голый» текст. Будет унифицировано в фазе 1.3 (ProblemDetails).

---

## Auth — `/api/v1/auth`

| Метод | Маршрут | Auth | Назначение |
|---|---|---|---|
| POST | `/register` | — | Регистрация пользователя. Возвращает `{ token, profile }`. |
| POST | `/login` | — | Логин. `{ email, password }` → `{ token, profile }`. |
| POST | `/setup-company` | JWT | Создать компанию для текущего пользователя. `{ companyName }` → `{ token, profile }`. |
| GET | `/me` | JWT | Текущий профиль. |
| GET | `/profiles` | JWT | Список доступных профилей пользователя. |
| POST | `/switch-company` | JWT | Переключение профиля/компании. `{ companyId: Guid? }` → `{ token, profile }`. |
| PUT | `/me` | JWT | Обновить профиль (`fullName`, `phone`). |
| PUT | `/me/password` | JWT | Сменить пароль. |

**Profile DTO (фактический baseline):**
```json
{
  "id": "guid",
  "email": "string",
  "fullName": "string",
  "phone": "string|null",
  "companyId": "guid|null",
  "companyName": "string|null",
  "isCompanyOwner": false,
  "isBasicProfile": true,
  "isAccessOrdersEdit": false,
  "isOrderPicker": false,
  "isExpeditor": false,
  "isDriver": false,
  "isAccessContractors": false,
  "isAccessContractorsEdit": false,
  "isAccessContractorDiscountEdit": false,
  "isAccessNomenclature": false,
  "isAccessNomenclatureEdit": false,
  "isAccessFinancialJournal": false,
  "isAccessFinancialJournalEdit": false,
  "isAccessWarehouses": false,
  "isAccessWarehousesEdit": false,
  "availableProfiles": [
    {
      "companyId": "guid|null",
      "name": "string",
      "role": "Личный|Владелец|...",
      "isBasicProfile": true,
      "isCompanyOwner": false
    }
  ]
}
```

---

## Companies — `/api/v1/companies`

| Метод | Маршрут | Auth | Назначение |
|---|---|---|---|
| GET | `/` | JWT | Список доступных компаний. |
| GET | `/{id}` | JWT | Получить компанию. |
| GET | `/mine` | JWT | Текущая компания пользователя. |
| PUT | `/{id}` | JWT, owner | Обновить компанию (реквизиты). |

> ⚠️ Создание компании сейчас идёт через `/auth/setup-company`. Это надо учесть при реорганизации (см. `docs/known-issues.md#UX-001`).

---

## Organizations — `/api/v1/organizations`

CRUD без особенностей.

| Метод | Маршрут | Auth | Назначение |
|---|---|---|---|
| GET | `/` | JWT | Список организаций компании. |
| GET | `/{id}` | JWT | Получить организацию. |
| POST | `/` | JWT, edit | Создать. `{ name, address, isDefault }`. **201 Created**. |
| PUT | `/{id}` | JWT, edit | Обновить. **200 OK**. |
| DELETE | `/{id}` | JWT, edit | Soft-delete. **204 NoContent**. |

---

## Counterparties — `/api/v1/counterparties`

| Метод | Маршрут | Auth | Назначение |
|---|---|---|---|
| GET | `/` | JWT, `IsAccessContractors` | Список. |
| GET | `/{id}` | JWT, `IsAccessContractors` | Получить. |
| POST | `/` | JWT, `IsAccessContractorsEdit` | Создать. **201**. |
| PUT | `/{id}` | JWT, `IsAccessContractorsEdit` | Обновить. **200**. |
| DELETE | `/{id}` | JWT, `IsAccessContractorsEdit` | Soft-delete. **204**. |

**Поля DTO (создание):** `name, fullName, type, status, inn, kpp, ogrn, paymentTermsDays`.

---

## Items — `/api/v1/items`

CRUD. Уникальность `(CompanyId, Sku)`.

| Метод | Маршрут | Auth |
|---|---|---|
| GET | `/` | JWT, `IsAccessNomenclature` |
| GET | `/{id}` | JWT, `IsAccessNomenclature` |
| POST | `/` | JWT, `IsAccessNomenclatureEdit` |
| PUT | `/{id}` | JWT, `IsAccessNomenclatureEdit` |
| DELETE | `/{id}` | JWT, `IsAccessNomenclatureEdit` |

---

## Warehouses — `/api/v1/warehouses` *(планируется удалить — нет в прототипе)*

| Метод | Маршрут | Auth |
|---|---|---|
| GET | `/` | JWT |
| GET | `/{id}` | JWT |
| POST | `/` | JWT, `IsAccessWarehousesEdit` |
| PUT | `/{id}` | JWT, `IsAccessWarehousesEdit` |
| DELETE | `/{id}` | JWT, `IsAccessWarehousesEdit` |
| GET | `/stock` | JWT |

> Будет удалено в фазе 1.x (см. обновлённый план). Фронт не должен на них опираться.

---

## Orders — `/api/v1/orders`

| Метод | Маршрут | Auth | Назначение |
|---|---|---|---|
| GET | `/` | JWT | Список заказов. |
| GET | `/{id}` | JWT | Получить заказ. |
| POST | `/` | JWT, `IsAccessOrdersEdit` | Создать (Draft). **201**. |
| PUT | `/{id}` | JWT, `IsAccessOrdersEdit` | Обновить (только не-locked статусы). **200**, **409** на concurrency. |
| POST | `/{id}/confirm` | JWT, edit | Draft → Processing. |
| POST | `/{id}/cancel` | JWT, edit | → Cancelled (если допустимо). |
| POST | `/{id}/next-status` | JWT, edit | Линейный переход. Тело: `{ deliveryType?: "selfPickup"\|"companyDelivery"\|"transportCompany" }`. |
| POST | `/{id}/assembly-checklist/close` | JWT, picker/edit | Закрыть чек-лист сборки в `Picking`. |
| DELETE | `/{id}` | JWT, edit | Soft-delete. **204**. |

**SalesOrderDto (тело создания/обновления):**
```json
{
  "number": "string|null",
  "date": "yyyy-MM-dd",
  "companyId": "guid",
  "counterpartyId": "guid",
  "comment": "string|null",
  "pickerId": "guid|null",
  "driverId": "guid|null",
  "bankAccount": "string|null",
  "contract": "string|null",
  "deliveryAddress": "string|null",
  "rowVersion": "number|null",
  "lines": [
    {
      "itemId": "guid",
      "nameSnapshot": "string",
      "vatRateSnapshot": "Vat0|Vat10|Vat20|Vat22|NoVat|...",
      "qty": 0,
      "unitPrice": 0,
      "discountType": "None|Percent|Amount",
      "discountValue": 0,
      "warehouseId": "guid|null"
    }
  ]
}
```

**OrderResponse (фактический baseline):**
```json
{
  "id": "guid",
  "number": "string|null",
  "date": "yyyy-MM-dd",
  "companyId": "guid",
  "companyName": "string",
  "counterpartyId": "guid",
  "counterpartyName": "string",
  "status": "Draft|Processing|...",
  "comment": "string|null",
  "totalNet": 0,
  "totalVat": 0,
  "totalGross": 0,
  "isAssemblyChecklistClosed": false,
  "pickerId": "guid|null",
  "driverId": "guid|null",
  "bankAccount": "string|null",
  "contract": "string|null",
  "deliveryAddress": "string|null",
  "createdAt": "ISO",
  "updatedAt": "ISO",
  "lines": [
    {
      "id": "guid",
      "itemId": "guid",
      "itemName": "string",
      "nameSnapshot": "string",
      "vatRateSnapshot": "Vat22",
      "qty": 0,
      "unitPrice": 0,
      "discountType": "Percent",
      "discountValue": 0,
      "net": 0,
      "vat": 0,
      "gross": 0,
      "warehouseId": "guid|null",
      "availableQty": 0
    }
  ],
  "rowVersion": 0
}
```

**Известные ошибки:**
- `409 concurrency_conflict` — формат:
  ```json
  { "error": { "code": "concurrency_conflict", "message": "...", "details": { "reason": "...", "entries": [...] } } }
  ```
- `409 invalid_transition` — для запретного перехода статуса.

---

## Invoices — `/api/v1/invoices`

CRUD, права `IsAccessFinancialJournal` / `Edit`.

| Метод | Маршрут |
|---|---|
| GET | `/` |
| GET | `/{id}` |
| POST | `/` |
| PUT | `/{id}` |
| DELETE | `/{id}` |

---

## UPDs — `/api/v1/upds`

CRUD, права `IsAccessFinancialJournal` / `Edit`.

| Метод | Маршрут |
|---|---|
| GET | `/` |
| GET | `/{id}` |
| POST | `/` |
| PUT | `/{id}` |
| DELETE | `/{id}` |

---

## Payments — `/api/v1/payments`

CRUD, права `IsAccessFinancialJournal` / `Edit`.

| Метод | Маршрут |
|---|---|
| GET | `/` |
| GET | `/{id}` |
| POST | `/` |
| PUT | `/{id}` |
| DELETE | `/{id}` |

---

## Users — `/api/v1/users`

| Метод | Маршрут | Назначение |
|---|---|---|
| GET | `/` | Список пользователей компании. |
| GET | `/for-orders` | Список (упрощённый), для дропдаунов в заказе. |
| GET | `/{id}` | Получить пользователя. |
| POST | `/` | Создать сотрудника. |
| PUT | `/{id}/permissions` | Обновить права. |
| PUT | `/{id}/profile` | Обновить профиль (fullName, phone). |
| PUT | `/{id}/activate` | Активировать. |
| PUT | `/{id}/deactivate` | Деактивировать. |
| PUT | `/{id}/reset-password` | Сброс пароля. |

Все требуют `CanManageUsers` (Owner / BasicProfile).

---

## Dev — `/api/v1/dev` *(только для отладки)*

| Метод | Маршрут | Назначение |
|---|---|---|
| POST | `/seed-test-company` | Засеять тестовую компанию. **Удалить из релизной сборки в фазе 3.6.** |

---

## Health

| Метод | Маршрут | Назначение |
|---|---|---|
| GET | `/api/v1/health` | Минимальный healthcheck. В фазе 1.7 будет добавлен `/health` с DB-проверкой. |

---

## CHANGELOG

- **2026-05-09** — baseline зафиксирован (фаза 0.3).
