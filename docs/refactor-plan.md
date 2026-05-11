# CRM_system — пошаговый план стабилизации и развития

> Цель: довести проект до зрелого состояния поэтапно, **без длинных простоев**. После каждого шага система **должна запускаться и работать** (`./run.ps1` поднимает БД + API + UI, основные сценарии не ломаются).
>
> Принципы:
> - **Сначала фундамент (отладка/тесты/наблюдаемость), потом новый функционал.**
> - **Один шаг = одна проверяемая поставка** с критериями приёмки.
> - **Каждая фаза заканчивается зелёной сборкой и работающей системой.**
> - **Никакая фаза не блокирует другую** — порядок строго одностороннем направлении (сверху вниз).
>
> Метки:
> - `[BE]` — backend (.NET)
> - `[FE]` — frontend (React)
> - `[INF]` — инфраструктура / DevOps / docs
> - `[DB]` — миграции и схема данных

---

## Карта фаз (TL;DR)

| # | Фаза | Цель | Срок (приблиз.) |
|---|------|------|-----------------|
| **0** | Подготовка | Аудит, заморозка контракта API, baseline-смок-тесты | 0.5–1 день |
| **1** | Backend foundation | Serilog, ProblemDetails, Swagger UI, валидация, фикс БД, тест-проект | 2–3 дня |
| **2** | Auth & Tenant | Refresh-токены, надёжный tenant-фильтр, audit interceptor | 2 дня |
| **3** | Application-слой (рефакторинг) | Вынести бизнес-логику из контроллеров в сервисы, покрыть тестами | 4–6 дней |
| **4** | Order pipeline (отладка) | Чёткая state-machine статусов, история, снапшоты, чек-лист | 3–4 дня |
| **5** | Frontend foundation | Типизированный API-клиент, TanStack Query, zod, Vitest | 3–4 дня |
| **6** | Расширение функционала по образцу FimBiz | Канбан, генерация PDF счёта/УПД, снапшоты, скидки/акции, чек-лист на фронте | 1.5–2 недели |
| **7** | DevOps | Dockerfile, GitHub Actions CI, healthchecks, structured config | 1–2 дня |
| **8** | Жёсткая полировка | Performance, security review, e2e Playwright, мониторинг | 2–3 дня |

> Ниже — детальные шаги. Каждый шаг указан в формате:
> **Шаг N.X — Название** → задачи, файлы, **критерий «работает»**.

---

## Фаза 0 — Подготовка (фиксируем точку отсчёта)

### Шаг 0.1 — Smoke-сценарии текущей системы `[INF]`
**Что делать:**
- Создать `docs/smoke-scenarios.md`: 10–15 ручных сценариев (login, создать компанию, создать контрагента, создать заказ, перевести статус, создать счёт, удалить контрагента, ...). Это baseline.
- Прогнать сценарии один раз и зафиксировать поведение (скриншоты/комментарии).

**Критерий:** существует документ; известно, что сейчас работает, а что — нет.

### Шаг 0.2 — `progress.txt` `[INF]`
**Что делать:**
- В корне — `progress.txt` (gitignored) для self-notes между сессиями: «что сделано, что в процессе, какие ловушки».

**Критерий:** файл создан, добавлен в `.gitignore`.

### Шаг 0.3 — Зафиксировать публичный контракт API `[BE]`
**Что делать:**
- В `docs/api-contract-baseline.md` выписать **существующие** маршруты, тела запросов/ответов на основании контроллеров. Это якорь, чтобы при рефакторинге не сломать фронт.
- Опционально: добавить пакет `Swashbuckle.AspNetCore` (см. шаг 1.4) и сохранить `swagger.json` baseline.

**Критерий:** документ существует, фронт продолжает работать с теми же URL.

---

## Фаза 1 — Backend foundation (отладка инфраструктуры)

> Цель: сделать backend наблюдаемым, валидируемым, тестируемым, без изменения публичного API.

### Шаг 1.1 — Reorganize Domain `[BE]`
**Что делать:**
- Создать папки `CrmSystem.Domain/Common`, `Companies`, `Counterparties`, `Items`, `Orders`, `Invoices`, `Upds`, `Payments`.
- Разнести `CrmEntities.cs` и `Class1.cs` по файлам:
  - `Common/Entity.cs`, `Common/AuditableEntity.cs`
  - `Companies/Company.cs`, `Companies/Organization.cs`
  - `Counterparties/Counterparty.cs`, `CounterpartyAddress.cs`, `CounterpartyContact.cs`, `Enums.cs`
  - `Items/Item.cs`, `Items/Enums.cs`
  - `Orders/SalesOrder.cs`, `SalesOrderLine.cs`, `Enums.cs`
  - `Invoices/Invoice.cs`, `InvoiceLine.cs`, `Enums.cs`
  - `Upds/Upd.cs`, `UpdLine.cs`, `Enums.cs`
  - `Payments/Payment.cs`, `PaymentAllocation.cs`, `Enums.cs`
- Удалить `Class1.cs` и оригинальные «свалки».
- **Никакой логики не меняем**, только перемещения.

**Критерий:** проект компилируется, все контроллеры работают, миграции не требуются.

### Шаг 1.2 — Serilog + контекст логов `[BE]`
**Что делать:**
- Подключить `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`.
- В `Program.cs` сконфигурировать sink Console + rolling file `logs/log-.txt` (30 дней).
- Добавить `app.UseSerilogRequestLogging()`.
- Все `ILogger<T>` уже работают — ничего в контроллерах не меняем.

**Критерий:** при запуске API идут структурированные логи в консоль и в файл `logs/`.

### Шаг 1.3 — ProblemDetails + глобальный exception handler `[BE]`
**Что делать:**
- В `Program.cs` добавить `builder.Services.AddProblemDetails()`.
- Создать `Infrastructure/GlobalExceptionHandler.cs` (`IExceptionHandler`).
- Регистрировать: `builder.Services.AddExceptionHandler<GlobalExceptionHandler>();` + `app.UseExceptionHandler();`.
- Маппинг: `ValidationException` → 400, `KeyNotFoundException` → 404, `InvalidOperationException` → 409, `UnauthorizedAccessException` → 401, остальное → 500. **Без stacktrace в Production.**

**Критерий:** при принудительном `throw new InvalidOperationException("test")` в любом контроллере приходит JSON `ProblemDetails` со статусом 409, без stacktrace в Production.

### Шаг 1.4 — Swagger UI `[BE]`
**Что делать:**
- Заменить `AddOpenApi()` / `MapOpenApi()` на `Swashbuckle.AspNetCore`:
  - `AddSwaggerGen` с JWT bearer security definition.
  - `app.UseSwagger(); app.UseSwaggerUI();` только в Development.
- Снабдить контроллеры XML-комментариями (постепенно, не блокирующе).

**Критерий:** `http://localhost:5035/swagger` показывает все эндпоинты, можно «Authorize» через JWT и дёргать API.

### Шаг 1.5 — FluentValidation `[BE]`
**Что делать:**
- Подключить `FluentValidation.AspNetCore` + `FluentValidation.DependencyInjectionExtensions`.
- Создать `CrmSystem.Api/Validators/`:
  - `SalesOrderDtoValidator` (Number ≤ 32, Date обязателен, Lines.Count > 0 при подтверждении и т.д.).
  - `InvoiceDtoValidator`, `CounterpartyDtoValidator`, `ItemDtoValidator`, ...
- Подключить через middleware: ошибки летят в `ProblemDetails` (см. 1.3).

**Критерий:** при отправке `POST /orders` с пустым `Lines` и `Date = default` приходит 400 с понятным телом.

### Шаг 1.6 — Конфиг и секреты `[BE][INF]`
**Что делать:**
- Удалить дубликат соединения. Решение по одному из вариантов (выбрать **A** для простоты):
  - **A.** Один `crm_main`, два `DbContext` оставляем (так и есть сейчас) — но в `appsettings.json` оставить **одну** строку и читать оба контекста из неё (или разделить таблицы префиксами/схемами Postgres `auth.*` и `crm.*`).
  - **B.** Разделить: `crm_auth` + `crm_main`. Тогда поменять connection strings и обновить `run.ps1`.
- Перенести секреты в `appsettings.Development.json` + **dotnet user-secrets**:
  - `dotnet user-secrets init --project CrmSystem.Api`
  - `dotnet user-secrets set "Jwt:Key" "..."`
- В `appsettings.json` оставить **только публичные значения** (Issuer/Audience/Lifetime).

**Критерий:** `Jwt:Key` отсутствует в репозитории; `./run.ps1` поднимает API без падений.

### Шаг 1.7 — Health checks `[BE]`
**Что делать:**
- `AddHealthChecks().AddDbContextCheck<CrmDbContext>().AddDbContextCheck<AuthDbContext>();`
- `app.MapHealthChecks("/health");`
- Старый `/api/v1/health` оставить для обратной совместимости.

**Критерий:** `GET /health` возвращает `Healthy` при поднятой БД и `Unhealthy` при остановленном Postgres.

### Шаг 1.8 — Тест-проект `CrmSystem.Tests` `[BE]`
**Что делать:**
- `dotnet new xunit -o CrmSystem.Tests`, добавить в slnx.
- Подключить `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`, `Testcontainers.PostgreSql`.
- Создать `WebAppFactory` (наследник `WebApplicationFactory<Program>`) с подменой connection-strings на Testcontainers Postgres.
- Один smoke-тест: `GET /health` → 200.
- Один тест на регистрацию + login → получаем JWT.

**Критерий:** `dotnet test` зелёный, тесты можно запускать локально без Docker — отдельным флагом `[Trait("Category","Integration")]` отделять интеграционные.

### Шаг 1.9 — Синхронизация RowVersion `[BE][DB]`
**Что делать:**
- Решить: использовать optimistic concurrency для `SalesOrder/SalesOrderLine` или нет.
- **Если да** (рекомендуется): в `ConfigureOrders` убрать `b.Ignore(x => x.RowVersion)` и для `SalesOrder` и для `SalesOrderLine`, добавить `ConfigureXminConcurrency`. Создать миграцию `EnableConcurrencyOnOrders` (фактически no-op для xmin).
- **Если нет**: убрать `RowVersion` из `OrderResponse` и `SalesOrderDto`.

**Критерий:** `OrdersController.Update` корректно обрабатывает RowVersion (или не использует его), фронт продолжает работать.

> **На выходе фазы 1:** API наблюдаем, валидируем, имеет Swagger, есть smoke-тесты, секреты не в репо, ProblemDetails единообразен.

---

## Фаза 2 — Auth & Tenant (надёжный фундамент авторизации)

### Шаг 2.1 — Refresh-токены `[BE][DB]`
**Что делать:**
- `Auth/RefreshToken.cs` (Token, UserId, ExpiresAt, RevokedAt, ReplacedBy).
- Миграция `AddRefreshTokens` в `AuthDbContext`.
- В `AuthController`:
  - `POST /auth/refresh { refreshToken }` — выдаёт новую пару, ротация (старый помечается `RevokedAt`).
  - `POST /auth/logout` — ревокация.
- В `Login` дополнительно возвращать `refreshToken`.
- `appsettings`: `AccessTokenExpiryMinutes = 15`, `RefreshTokenExpiryDays = 30`.

**Фронт:**
- `crm-web/src/api.ts`: при 401 пытается `POST /auth/refresh`, при успехе — повторяет исходный запрос; при провале — логаут.

**Критерий:** access-токен на 15 минут, фронт «сам» обновляет; logout инвалидирует refresh.

### Шаг 2.2 — Tenant-фильтр через interceptor `[BE]`
**Что делать:**
- `Persistence/Interceptors/TenantSaveChangesInterceptor.cs` — на `SaveChangesAsync` проверяет, что все изменяемые `ITenantOwned`-сущности имеют `CompanyId == CurrentUser.CompanyId` (если не Owner-bypass нужен).
- Маркер-интерфейс `ITenantOwned { Guid CompanyId { get; } }` или базовый класс. Реализовать у `Counterparty`, `Item`, `SalesOrder`, `Invoice`, `Upd`, `Payment`.
- Это **второй слой защиты** в дополнение к фильтрам в контроллерах. На случай, если кто-то забудет фильтр.

**Критерий:** unit-тест: при попытке сохранить `Counterparty` с чужим `CompanyId` interceptor бросает `InvalidOperationException("Tenant violation")`.

### Шаг 2.3 — Audit interceptor `[BE]`
**Что делать:**
- `Persistence/Interceptors/AuditSaveChangesInterceptor.cs`:
  - на `Added` — `CreatedAt`, `CreatedBy = currentUser.UserId`, `UpdatedAt = CreatedAt`, `UpdatedBy = currentUser.UserId`.
  - на `Modified` — `UpdatedAt`, `UpdatedBy`.
- Удалить ручное проставление `CreatedAt/UpdatedAt` из всех контроллеров (оставить, где нет user — например, фоновые задачи).

**Критерий:** при `POST /counterparties` без явного указания дат — сущность сохраняется с корректными `CreatedAt/By/UpdatedAt/By`.

### Шаг 2.4 — Permissions из БД (опционально, не блокирует) `[BE]`
**Что делать:** (если нужно гибче, чем claims в JWT)
- `EmployeeProfile` или `UserCompanyMembership` (UserId, CompanyId, IsCompanyOwner, IsBasicProfile, разрешения). Сейчас права хранятся как claims в JWT — при изменении прав нужен новый JWT.
- Endpoint: `GET /auth/me/permissions` возвращает текущие права. Фронт использует их вместо парсинга JWT.

**Критерий:** изменение прав отражается в UI без релогина (после `refresh`-токена).

> **На выходе фазы 2:** долговечный логин, безопасный tenant-isolation, корректный аудит. Никаких новых сущностей в UI.

---

## Фаза 3 — Application-слой (рефакторинг)

> Цель: вынести бизнес-логику из контроллеров. **Без MediatR** (избыток для текущего размера) — простые сервисы.

### Шаг 3.1 — Создать проект `CrmSystem.Application` `[BE]`
**Что делать:**
- `dotnet new classlib -o CrmSystem.Application`. Зависит от `CrmSystem.Domain`. **НЕ** зависит от `CrmSystem.Persistence`.
- Внутри: интерфейсы репозиториев или прямой `IUnitOfWork` + `IQueryable`-тип-обёртка (вариант проще: пусть `Application` зависит от `Persistence` — как часто и делают, не догматично).
- Common: `Result<T>`, `IDateTimeProvider`, `ICurrentUser` (интерфейс, реализованный в Api как `CurrentUserService`).

**Критерий:** проект собирается, добавлен в slnx, ничем пока не используется.

### Шаг 3.2 — Перенести Orders в Application `[BE]`
**Что делать:**
- `Application/Orders/OrdersService.cs`:
  - `Task<Result<OrderResponse>> CreateAsync(SalesOrderDto, CancellationToken)`
  - `UpdateAsync`, `DeleteAsync`, `GetAllAsync`, `GetByIdAsync`,
  - `ConfirmAsync(id)`, `CancelAsync(id)`, `NextStatusAsync(id, deliveryType)`, `CloseAssemblyChecklistAsync(id)`.
- `Application/Orders/Mappers/OrderMapper.cs` — перенести `ToResponse`.
- `Application/Orders/Pricing/OrderTotalsCalculator.cs` — расчёт `Net`, `Vat`, `Gross`.
- `OrdersController` становится тонким: вызов `IOrdersService` + маппинг ошибок. `~70 строк вместо 457`.

**Критерий:** все эндпоинты работают идентично, baseline smoke-сценарии (шаг 0.1) — зелёные. Покрытие unit-тестами `OrderTotalsCalculator` и `OrdersService.NextStatusAsync` — ≥ 80%.

### Шаг 3.3 — State-machine статусов как отдельный класс `[BE]`
**Что делать:**
- `Application/Orders/OrderStatusTransitions.cs`:
  - таблица `(currentStatus, deliveryType) → nextStatus | invalid`.
  - метод `bool CanTransit(from, to, deliveryType)` для будущего ручного перехода.
- Покрыть таблицей-теории (`[Theory]`) все комбинации.

**Критерий:** unit-тесты по каждому варианту доставки — зелёные.

### Шаг 3.4 — Перенести остальные контроллеры `[BE]`
**По одному за PR:**
- `CounterpartiesService` (CRUD + фильтры).
- `ItemsService`.
- `InvoicesService` (включая обновление `PaidAmount` через `Payments`).
- `UpdsService`.
- `PaymentsService`.
- `CompaniesService`, `OrganizationsService`.

**Критерий:** контроллеры по 50–100 строк каждый, все тесты smoke остаются зелёными.

### Шаг 3.5 — Общие фильтры/пагинация `[BE]`
**Что делать:**
- `Application/Common/PagedResponse<T>`.
- `Application/Common/PageRequest` с `Page`, `PageSize`, `SortBy`, `SortDirection`.
- Расширения `IQueryable.ToPagedResponseAsync(...)`.
- Перевести `GET /orders`, `/counterparties`, `/items` на пагинацию (но **сохранить** старый формат при `?page=` отсутствует — для обратной совместимости).

**Критерий:** фронт без изменений работает; новый `?page=1&pageSize=20&sortBy=date&sortDirection=desc` возвращает `PagedResponse<T>`.

### Шаг 3.6 — Изолировать `DevSeedController` `[BE]`
**Что делать:**
- Перенести в отдельный namespace + `#if DEBUG` или регистрация только в Development:
  ```csharp
  if (app.Environment.IsDevelopment()) app.MapControllerRoute(...);
  ```
- Убрать из релизной сборки.

**Критерий:** в Production билд (`dotnet publish -c Release`) `DevSeedController` отсутствует.

> **На выходе фазы 3:** контроллеры тонкие, тесты покрывают расчёты и state-machine; бизнес-логика в `Application/`; baseline-сценарии работают.

---

## Фаза 4 — Order pipeline (отладка ядра)

> Цель: довести заказ до того уровня предсказуемости, что есть в FimBiz, оставаясь в текущей доменной модели.

### Шаг 4.1 — История статусов `[BE][DB][FE]`
**Что делать:**
- `Domain/Orders/OrderStatusHistory.cs` (`OrderId`, `Status`, `ChangedAt`, `ChangedBy`, `Comment?`).
- Миграция `AddOrderStatusHistory`.
- `OrdersService.ChangeStatusAsync` пишет запись в историю при каждом успешном переходе.
- Endpoint `GET /orders/{id}/status-history` → список событий.
- Фронт: вкладка/таб «История» в `OrderDetailPanel` (только чтение).

**Критерий:** при `confirm`/`next-status`/`cancel` появляется запись; на фронте видно цепочку переходов с авторами.

### Шаг 4.2 — Снапшоты реквизитов в заказе `[BE][DB]`
**Что делать:**
- В `SalesOrder` добавить `BankAccountSnapshotJson?` (структурированный снапшот реквизитов компании на момент создания/подтверждения).
- `DeliveryAddressSnapshotJson?` — структурированный адрес доставки (если контрагент передал ID адреса из своего справочника, как в FimBiz).
- При `Confirm` (Draft → Processing) делать снапшоты.
- В `OrderResponse` отдавать оба.

**Критерий:** изменение реквизитов компании после `Confirm` не меняет суммы/реквизиты в уже подтверждённом заказе.

### Шаг 4.3 — Снапшоты в `Invoice`/`Upd` `[BE][DB]`
**Что делать:**
- В `Invoice` и `Upd` добавить:
  - `BankRequisitesSnapshotJson`, `CounterpartyInnSnapshot`, `CounterpartyKppSnapshot`, `DeliveryAddressSnapshotJson`.
  - `CalculationSnapshotJson` (LONGTEXT) — точная разбивка по строкам/НДС/скидкам, как в FimBiz (`OrderDocumentCalculationSnapshot`).
- Сервисы при `Create` заполняют снапшоты из текущих данных.

**Критерий:** удаление контрагента (soft-delete) не делает счёт нечитаемым: PDF строится из снапшота.

### Шаг 4.4 — Чек-лист сборки в JSON `[BE][DB][FE]`
**Что делать:**
- В `SalesOrder` добавить `PickingChecklistJson` — массив `{ ItemId, Name, Qty, Done, DoneBy?, DoneAt? }`.
- При переходе в `Picking` чек-лист генерируется автоматически из строк заказа (если ещё не существует).
- Endpoints:
  - `POST /orders/{id}/picking-checklist` — обновить целиком (для фронта).
  - `POST /orders/{id}/picking-checklist/items/{itemId}/done` — отметить позицию.
- Когда **все** позиции `Done == true`, флаг `IsAssemblyChecklistClosed` ставится автоматически. Существующий `assembly-checklist/close` оставляем как «закрыть досрочно».

**Критерий:** сборщик в `Picking` отмечает позиции, на фронте видит прогресс; после полного закрытия можно перевести статус.

### Шаг 4.5 — `OrdersController.NextStatus` — без рекурсии и с явной таблицей `[BE]`
**Что делать:**
- Переписать `GetNextStatus` через таблицу из шага 3.3, без рекурсии.
- Добавить guards:
  - `WaitingForPayment → BillConfirmed` требует наличие `Invoice` со статусом `Issued` для заказа (по аналогии с FimBiz `CurrentBill != null`).
  - `Picking → ReadyForShipment` (для `companyDelivery`/`transportCompany`) требует закрытый чек-лист.
- При невозможности — 409 с понятным `code` и `message`.

**Критерий:** все валидные комбинации покрыты тестами; запрещённые возвращают 409.

> **На выходе фазы 4:** заказ становится надёжной агрегатной единицей: история, снапшоты, чек-лист, чёткие переходы. Фронт пользуется этими возможностями.

---

## Фаза 5 — Frontend foundation

> Цель: типобезопасность, кеширование, валидация форм, переиспользуемость, тесты. **Без редизайна** — текущие страницы продолжают работать.

### Шаг 5.1 — Типизированный API-клиент `[FE]`
**Что делать:**
- Вариант A (**рекомендуется**): сгенерировать TypeScript-типы из Swagger:
  - подключить `openapi-typescript`: `npx openapi-typescript http://localhost:5035/swagger/v1/swagger.json -o src/api/schema.ts`.
  - тонкая обёртка `src/api/client.ts` с типами-хелперами.
- Вариант B: вручную создать `src/api/types.ts` с DTO и постепенно заменить дубликаты в страницах.
- Перевести один модуль (Orders) на типизированный клиент → проверить → дальше остальные.

**Критерий:** в `pages/Orders.tsx` нет дублированных интерфейсов DTO; `tsc -b` зелёный.

### Шаг 5.2 — TanStack Query `[FE]`
**Что делать:**
- `npm i @tanstack/react-query @tanstack/react-query-devtools`.
- `src/main.tsx`: `QueryClientProvider` + `ReactQueryDevtools` в dev.
- Пример хука: `src/api/orders/useOrders.ts` (`useQuery`/`useMutation`).
- Перевести `Orders.tsx` (потом остальные) на хуки → пропадают локальные `useState` для списка/loading/error.

**Критерий:** список заказов кешируется; при возврате на страницу не делается повторный запрос (если данные свежие); refetch при мутации.

### Шаг 5.3 — zod для форм `[FE]`
**Что делать:**
- `npm i zod`.
- `src/api/schemas/order.ts`: `z.object({ number, date, counterpartyId, lines: z.array(...) })`.
- Парсинг ответов API через zod (опционально) и **обязательно** валидация форм перед отправкой.
- Использовать в `OrderUpsertPanel`, `InvoiceUpsertPanel`, `Counterparties`, `Items`, ...

**Критерий:** в форме создания заказа нельзя сабмитить с пустым списком позиций; ошибки выводятся под полями.

### Шаг 5.4 — Структура `src/features/` `[FE]`
**Что делать:**
- Перейти от `pages/` + `components/` (кучей) к feature-folders:
  ```
  src/
    features/
      orders/{api, components, hooks, pages}
      counterparties/...
      invoices/...
    shared/{ui, api, lib}
    app/  (роуты, провайдеры)
  ```
- Делать постепенно: Orders → Counterparties → ... В пределах одной фичи всё рядом.
- Старые пути сохранить через re-export или обновить `App.tsx` сразу.

**Критерий:** структура `src/features/orders/` полностью укомплектована, маршрут `/orders` работает.

### Шаг 5.5 — Vitest + React Testing Library `[FE]`
**Что делать:**
- `npm i -D vitest @testing-library/react @testing-library/user-event jsdom`.
- `vite.config.ts` секция `test`.
- Несколько тестов:
  - utils (форматтеры сумм, дат, скидок).
  - hooks `useOrders` (с моком fetch).
  - компонент `OrderUpsertPanel` (валидация формы).
- `package.json`: `"test": "vitest"`.

**Критерий:** `npm test` — зелёный, в CI прогоняется.

### Шаг 5.6 — Обработка ошибок и Toasts `[FE]`
**Что делать:**
- Глобальный handler в `QueryClient` `onError` → `toast.error(...)` (использовать существующий `Toast.tsx`).
- При 401 (после неудачного refresh) → редирект на `/login` (уже есть в `api.ts`, перенести в правильное место после интеграции с Refresh из шага 2.1).
- Сделать единый компонент-обёртку для отображения `ProblemDetails`-ошибок API (показ `code`, `message`, при необходимости `details`).

**Критерий:** при ошибке API в любом месте появляется toast с понятным сообщением; нет «голых» exceptions в консоли.

> **На выходе фазы 5:** фронт типобезопасный, форма-валидируемый, кешируемый, тестируемый, разложенный по фичам.

---

## Фаза 6 — Расширение функционала по образцу FimBiz

> Цель: добавить ключевые фичи из FimBiz, **не копируя архитектурные ошибки** (god-class, секреты, profileId в query).
>
> Выполнять **по одной фиче**, каждая — отдельный шаг.

### Шаг 6.1 — Канбан заказов `[BE][FE]`
**Что делать:**
- `Application/Orders/Kanban/KanbanColumn.cs` — enum `Processing/New/InWork/Picking/Completed/Cancelled` + маппинг → `OrderStatus[]` (как в FimBiz, см. `docs/fimbiz-backend-crm-analysis.md` §2.2).
- `GET /orders/kanban?pageSize=20&...` → `{ columns: [{ column, total, items: [...] }] }`.
- Фронт: новая страница/таб `OrdersKanban.tsx` с колонками, drag-n-drop (использовать `@dnd-kit/core`).
- При drop — вызов `next-status` (или явный `change-status` — см. ниже).

**Критерий:** Kanban-страница работает, можно перетаскивать карточку из `New` в `InWork` через смену статуса.

### Шаг 6.2 — Явный `POST /orders/{id}/change-status` `[BE][FE]`
**Что делать:**
- В дополнение к `next-status` — явный переход с указанием `{ targetStatus, comment? }`.
- Использовать `OrderStatusTransitions.CanTransit` (шаг 3.3) с расширенной таблицей.
- На фронте — выпадающий список доступных переходов из текущего статуса.

**Критерий:** UI показывает только валидные переходы; нелегитимные приводят к 409 при попытке вызвать напрямую.

### Шаг 6.3 — Скидки на позицию и на заказ `[BE][DB][FE]`
**Что делать:**
- В `SalesOrder` добавить `DiscountApplicationMode`, `DiscountPercent?`, `TotalDiscountAmount` (см. FimBiz §2.1).
- В `OrderTotalsCalculator` — режимы:
  - `None`, `PerItem` (уже близко к существующему), `WholeDocument`.
- В UI — переключатель режима + поле для процента скидки на документ.

**Критерий:** перевод между режимами корректно пересчитывает `TotalNet/Vat/Gross`; покрыто unit-тестами.

### Шаг 6.4 — Режимы НДС `[BE][DB][FE]`
**Что делать:**
- `VatCalculationMode` (`Included | OnTop`) и `VatApplicationMode` (`PerItem | WholeDocument`) + `DocumentVatRate` (см. FimBiz §2.1).
- Расширить `OrderTotalsCalculator`. Покрыть таблицей `[Theory]`.
- В UI — селекты в форме заказа.

**Критерий:** все 4 комбинации (Included/OnTop × PerItem/WholeDocument) дают корректный итог; тесты зелёные.

### Шаг 6.5 — Адреса и контакты контрагента `[BE][FE]`
**Что делать:**
- В UI добавить редактирование `CounterpartyAddress` и `CounterpartyContact` (сущности уже есть, но в UI не используются — `docs/project-overview.md` §10).
- При создании заказа — выбор адреса доставки из справочника (как в FimBiz `DeliveryAddressId`).
- Сохранение `DeliveryAddressId` + снапшота в `SalesOrder` (см. шаг 4.2).

**Критерий:** в карточке контрагента есть вкладки «Адреса»/«Контакты»; в форме заказа можно выбрать адрес из выпадающего списка.

### Шаг 6.6 — Генерация PDF счёта (Invoice) `[BE]`
**Что делать:**
- Подключить `QuestPDF` (Apache 2.0, без коммерческой лицензии при выручке < $1M) или `PDFsharp`. **QuestPDF** — рекомендую: без шаблонов, всё кодом, минимум зависимостей.
- `Application/Invoices/Pdf/InvoicePdfGenerator.cs`:
  - читает `Invoice` + `CalculationSnapshot` + `BankRequisitesSnapshot`.
  - генерирует PDF в `MemoryStream`.
- Endpoint: `GET /invoices/{id}/pdf` → `application/pdf`.
- Frontend: кнопка «Скачать счёт» в `InvoiceDetailPanel`.

**Критерий:** скачанный PDF содержит реквизиты, позиции, итоги; повторное скачивание после изменения реквизитов компании даёт **тот же** PDF (использует снапшот).

### Шаг 6.7 — Генерация PDF УПД `[BE]`
**Что делать:**
- Аналогично шагу 6.6 для `Upd`. Использовать `UpdFunction` (`ShippingAndInvoice` / `ShippingOnly` / `InvoiceOnly`) для разных шапок.
- Endpoint: `GET /upds/{id}/pdf`.

**Критерий:** PDF УПД корректно отображает функцию и реквизиты; ссылка из `OrderDetailPanel`.

### Шаг 6.8 — Распределение платежей (`PaymentAllocation`) `[BE][FE]`
**Что делать:**
- При `POST /payments` с `type=Incoming`, разрешить указать `allocations: [{ invoiceId, amount }]`.
- При `Posted` — увеличить `Invoice.PaidAmount` на сумму распределения.
- Если `Invoice.PaidAmount >= TotalGross` → `Invoice.Status = Closed`.
- В UI — мастер «Распределить платёж по счетам» с выбором счетов и сумм.

**Критерий:** при создании платежа на сумму счёта — счёт автоматически становится `Closed`; покрыто тестом.

### Шаг 6.10 — RecycleBin (корзина) `[BE][FE]`
**Что делать:**
- Сейчас `IsDeleted` + query filter уже есть; добавить:
  - `GET /recycle-bin?type=order|counterparty|...&page=...` — возвращает удалённые сущности.
  - `POST /recycle-bin/restore?type=order&id=...`.
  - `DELETE /recycle-bin/permanent?type=order&id=...` (только Owner).
- Реализовать через единый `IRecyclableService` с `Dictionary<Type, ...>`.
- Фронт: страница «Корзина».

**Критерий:** удалённый заказ виден в корзине, восстанавливается, окончательно удаляется (только владельцем).

### Шаг 6.11 — Поиск/фильтры по заказам `[BE][FE]`
**Что делать:**
- В `GET /orders` добавить параметры: `searchQuery` (по `Number`, `Counterparty.Name`, `Comment`), `status[]`, `counterpartyId`, `dateFrom/To`, `totalFrom/To`.
- Использовать `EF.Functions.ILike` для регистронезависимого поиска (Postgres).
- На фронте — панель фильтров над списком.

**Критерий:** поиск по части номера/имени контрагента работает; фильтр по диапазону дат корректен.

> **На выходе фазы 6:** функциональность приближена к FimBiz по объёму, но без god-class и других антипаттернов.

---

## Фаза 7 — DevOps

### Шаг 7.1 — Dockerfile для API `[INF]`
**Что делать:**
- `CrmSystem.Api/Dockerfile` (multi-stage: `mcr.microsoft.com/dotnet/sdk:8.0` для build, `aspnet:8.0` для runtime).
- Расширить `docker-compose.yml`: сервис `api`, `web` (опционально build of vite + nginx), общая сеть с `postgres`.

**Критерий:** `docker compose up` поднимает Postgres + API; `http://localhost:5035/health` отвечает `Healthy`.

### Шаг 7.2 — GitHub Actions CI `[INF]`
**Что делать:**
- `.github/workflows/ci.yml`:
  - matrix: backend (`dotnet build`, `dotnet test --collect:"XPlat Code Coverage"`), frontend (`npm ci`, `npm run build`, `npm test`).
  - upload coverage в Codecov (опционально).
- Запрет мержа PR без зелёных проверок.

**Критерий:** PR с пушем в `main` запускает CI, фейлится на падающих тестах.

### Шаг 7.3 — Конфиг через env-vars `[INF][BE]`
**Что делать:**
- Все переменные в `appsettings*.json` дублировать через ENV: `Jwt__Key`, `ConnectionStrings__CrmDb`, ...
- В `Dockerfile` / `docker-compose.yml` использовать env.

**Критерий:** запуск `docker run -e Jwt__Key=... -e ConnectionStrings__CrmDb=... crm-api` работает без файла `appsettings.json`.

### Шаг 7.4 — Healthcheck UI и метрики `[INF][BE]`
**Что делать:**
- `AspNetCore.HealthChecks.UI` (опционально) для красивой страницы статуса.
- `OpenTelemetry.Extensions.Hosting` + `OpenTelemetry.Instrumentation.AspNetCore` + `OpenTelemetry.Exporter.Console` (для начала). Позже — Prometheus/Grafana.

**Критерий:** в логах виден трейс HTTP-запроса (`TraceId`), на `/health-ui` (если включить) — статус Postgres.

---

## Фаза 8 — Полировка

### Шаг 8.1 — Performance review `[BE]`
- Проверить `N+1`-запросы (включая ToList/.Include в `OrdersController`).
- Добавить `AsNoTracking` для read-only маршрутов.
- Индексы в БД на горячих фильтрах (`SalesOrder(CompanyId, Status, Date)`, `Counterparty(CompanyId, Inn)`).
- Бенчмарки `BenchmarkDotNet` для `OrderTotalsCalculator` (опционально).

### Шаг 8.2 — Security review `[BE]`
- HTTPS-redirect в Production.
- Rate-limiting на `/auth/login` и `/auth/register` (`Microsoft.AspNetCore.RateLimiting`).
- Антибрутфорс: блокировка после N неудачных попыток входа.
- Заголовки: HSTS, X-Frame-Options, Content-Security-Policy.
- Заголовок `Server` — убрать.
- Проверка зависимостей через `dotnet list package --vulnerable --include-transitive`.

### Шаг 8.3 — End-to-end Playwright `[FE]`
- 5–10 e2e сценариев (login → создать заказ → подтвердить → перевести в Picking → закрыть чек-лист → создать счёт → создать УПД).
- Запускать в CI на отдельной job.

### Шаг 8.4 — Документация и онбординг `[INF]`
- Обновить `docs/project-overview.md` под новую архитектуру.
- `docs/dev-onboarding.md` — как запускать, как писать тесты, как добавлять миграции.
- ADR (`docs/adr/`) — фиксировать ключевые архитектурные решения.

---

## Контрольная таблица «работает ли проект на каждом этапе»

| Конец фазы | Что должно работать |
|-----------|----------------------|
| **0** | Текущая система; baseline-документы; запускается через `./run.ps1`. |
| **1** | То же + Swagger UI, Serilog в файл, ProblemDetails, валидация DTO, secrets вне репо, smoke-тесты xunit. |
| **2** | То же + refresh-токены, tenant interceptor, audit interceptor (поведение наружу не меняется). |
| **3** | То же + контроллеры тонкие, бизнес-логика в `Application`, тесты state-machine и расчётов. |
| **4** | То же + история статусов, снапшоты в Order/Invoice/Upd, чек-лист сборки в JSON, чёткие переходы. |
| **5** | То же + типизированный API-клиент, TanStack Query, zod, vitest, feature-folders. |
| **6** | То же + Канбан, скидки/НДС режимы, адреса контрагентов, PDF Invoice/Upd, резервирование, корзина, поиск/фильтры. |
| **7** | То же + Dockerfile API, CI зелёный, env-config, healthchecks. |
| **8** | То же + перфоманс, секьюрити, e2e Playwright, обновлённые docs. |

---

## Стартовый пакет инструментов (что подключаем суммарно)

**Backend NuGet:**
- `Serilog.AspNetCore`, `Serilog.Sinks.File`
- `Swashbuckle.AspNetCore`
- `FluentValidation.AspNetCore`
- `Microsoft.AspNetCore.Diagnostics.HealthChecks`
- `AspNetCore.HealthChecks.NpgSql` (для Postgres)
- `Microsoft.AspNetCore.Mvc.Testing`, `xunit`, `FluentAssertions`, `Testcontainers.PostgreSql`
- `QuestPDF` (PDF, см. шаг 6.6)
- `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore` (фаза 7)
- `Microsoft.AspNetCore.RateLimiting` (фаза 8)

**Frontend npm:**
- `@tanstack/react-query`, `@tanstack/react-query-devtools`
- `zod`
- `openapi-typescript` (codegen)
- `@dnd-kit/core` (канбан, фаза 6)
- `vitest`, `@testing-library/react`, `@testing-library/user-event`, `jsdom`
- `@playwright/test` (фаза 8)

---

## Правила выполнения плана

1. **Не делать два шага параллельно.** Один шаг = один зелёный коммит/PR.
2. **После каждого шага — `./run.ps1` и проверка smoke-сценария** из шага 0.1, к которому относится изменение.
3. **Все breaking-changes в API — после фазы 5** (когда фронт перевели на codegen и легко регенерировать типы).
4. **Миграции БД** — только аддитивные (новые колонки nullable, новые таблицы). Удаление колонок — отдельной миграцией после переноса данных.
5. **`progress.txt`** обновляется в конце каждого дня: что сделано, какие хвосты, на чём остановился.
6. **Фронт и бек коммитятся в одном PR**, если шаг затрагивает оба — чтобы CI проверял сразу.
7. **Каждая фаза заканчивается тегом** в git: `phase-1-done`, `phase-2-done`, ... — точка отката.

---

## Что НЕ делаем намеренно

- **Не пишем god-class** (как `OrdersUseCases` на 400 KB в FimBiz).
- **Не копируем `profileId` в query string** — у нас claims в JWT и refresh-токены.
- **Не делаем gRPC/SignalR** до фазы 7+ — текущему размеру проекта это избыточно.
- **Не интегрируемся с 1С/Диадоком/DaData/Zvonok** — отложено за рамки плана. Интерфейсы (`IExternalSync`) можно подготовить, реализации — позже.
- **Не вводим MediatR/CQRS-фреймворки** — простые сервисы достаточны до момента, когда команд >50.

---

## Что делать прямо сейчас (первый день)

1. Создать `progress.txt`, добавить в `.gitignore`.
2. Заполнить `docs/smoke-scenarios.md` (10–15 пунктов).
3. Сделать **шаг 1.1** (рефакторинг `Domain` по папкам).
4. Сделать **шаг 1.2** (Serilog).
5. Закоммитить и проверить, что `./run.ps1` всё ещё работает.

После — двигаться по фазам строго по порядку.
