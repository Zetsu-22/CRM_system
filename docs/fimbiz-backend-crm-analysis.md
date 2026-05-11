# FIMBIZ Backend (TimeTracingServer) — Подробный анализ CRM-части

> Источник: `FIM-TECHNOLOGY/TimeTracingServer`, склонирован в `c:\Users\Doffy\source\repos\TimeTracingServer`.
> Solution: `TimeTracingServer.sln`, основной проект: `TimeTracingServer/TimeTracingServer.csproj` (**.NET 8**).
>
> Документ описывает **только CRM-сегмент** монолита FimBiz: Заказы, Контрагенты, Номенклатура, Счета, УПД, и связанная инфраструктура. URV/мессенджер/задачи/ЭДО — упоминаются только если связаны с CRM.

---

## 0. Высокоуровневая карта

### 0.1. Стек
- **ASP.NET Core 8** (Web API + MVC views для legacy)
- **EF Core 8 + Pomelo MySQL 8**, две БД: `fimbiz_identity`, `fimbiz`
- **ASP.NET Core Identity** (`User`, `IdentityRole`)
- **JWT Bearer** + дополнительная схема **`ApiKey`** (`ApiKeyAuthenticationHandler`) — для интеграций (1С и т. п.)
- **SignalR**: `MasterHub` (CRM-фронт), `NotificationHub*`, `TerminalHub`, `QrAuthHub`
- **gRPC**: `contractor_sync.proto`, `order_sync.proto`, `permissions.proto`, `smm_planner_projects.proto`
- **Serilog** (Console + File rolling, 30 дней)
- **Swagger** + `SignalRSwaggerGen` (только в Development)
- **Firebase Admin SDK** для push
- **Документы**: `DinkToPdf`, `DocumentFormat.OpenXml`, `EPPlus`, `MiniExcel`, `PDFsharp`, `FreeSpire.Doc`, `Mammoth`

### 0.2. Стиль архитектуры
- **Монолит** с условной DDD-структурой: `v2/DDD/<Aggregate>/{Entities, DTO, Mappers, Services, UseCases, Enums}`.
- Контроллеры (`v2/Controllers`) — **тонкие**: авторизация + проверка подписки + делегирование в `*UseCases`.
- Бизнес-логика живёт в `*UseCases.cs` (часто гигантских, например `OrdersUseCases.cs` ~419 KB).
- В `Services/<Domain>MySQLService.cs` — слой запросов (фильтры/сортировки/пагинация).

### 0.3. Tenant-модель (это главное отличие от текущего `CRM_system`)
1. **`User` (Identity)** — учётка (email/телефон + пароль).
2. **`Employee` ↔ `User`** через `Employee.TargetUserId == user.Id`.
3. **`EmployeeProfile`** — рабочая единица CRM:
   - `CompanyId` — к какой компании относится профиль;
   - `OrganizationIdListJSON` — какие юр.лица доступны внутри компании;
   - набор `IsAccess*` булевых флагов;
   - `IsCompanyOwner` — owner-bypass.
4. **Все CRM-эндпоинты принимают `?profileId=...`**. `GetAuthorizedProfileAsync(profileId)` проверяет, что профиль принадлежит **текущему `User`** из JWT — иначе `Unauthorized`. Это даёт один логин и **много профилей** (несколько компаний).

### 0.4. Доступ к CRM завязан на подписку
В каждом CRM-эндпоинте:
```csharp
var subCheck = await _subscriptionCheckService.CheckSubscription(profileId, SubscriptionArticulEnum.CrmOrders);
if (!subCheck.IsAvailable)
    return StatusCode(403, new { error = subCheck.ErrorMessage, subscriptionRequired = "crmorders" });
```

### 0.5. Права доступа CRM
| Право `EmployeeProfile`            | Смысл                                                              |
|------------------------------------|--------------------------------------------------------------------|
| `IsCompanyOwner`                   | Полный доступ ко всем сущностям компании                           |
| `IsAccessOrdersEdit`               | Редактировать заказы / документы / статусы                         |
| `IsOrderPicker`                    | Сборщик: видит назначенные заказы, может закрывать чек-лист        |
| `IsExpeditor`                      | Экспедитор: доставка/статусы отгрузки                              |
| `IsDriver`                         | Водитель: только просмотр своих заказов                            |
| `IsAccessContractors / Edit`       | Контрагенты                                                        |
| `IsAccessContractorDiscountEdit`   | Скидки контрагентов                                                |
| `IsAccessNomenclature / Edit`      | Номенклатура                                                       |
| `IsAccessFinancialJournal / Edit`  | Счета, УПД, платежи                                                |
| `IsAccessWarehouses / Edit`        | Склады, остатки                                                    |
| `IsAccessInternetShopAdmin`        | Администрирование интеграции с интернет-магазином (gRPC)           |

---

## 1. Карта CRM-модулей

```
v2/DDD/
├── Orders/          ← заказ — центральный «дирижёр» CRM-pipeline
├── Contractors/     ← контрагенты, иерархия групп, скидки, личные цены, адреса
├── Nomenclatures/   ← товары/услуги/сырьё, дерево, варианты, витрина, QR
├── Bills/           ← инфраструктура генерации Word/PDF (УПД, счёт)
├── Promotions/      ← акции / автоскидки
├── Organizations/   ← юр. лица компании-продавца, реквизиты, банки
├── Tasks/           ← служебные задания (используются для ManufacturingTask)
├── RecycleBin/      ← soft-delete + восстановление + фоновые сервисы
├── OneCIntegration/ ← синк контрагентов/номенклатуры/документов с 1С
├── Edo/             ← Контур.Диадок (ЭДО)
└── ValueObjects/    ← BankRequesites, DeliveryAddressModel и пр.
```

### Контроллеры CRM (`v2/Controllers/`)
| Файл                          | Размер  | Назначение |
|-------------------------------|---------|------------|
| `OrdersController.cs`         | ~200 KB | Заказы, канбан, статусы, печать, 1С |
| `OrderBillsController.cs`     | ~74 KB  | Счета по заказам и standalone |
| `OrderUpdsController.cs`      | ~35 KB  | УПД |
| `ContractorController.cs`     | ~123 KB | Контрагенты, скидки, Excel, DaData, 1С |
| `NomenclatureController.cs`   | ~62 KB  | Номенклатура, группы, варианты |
| `ProductGroupController.cs`   | ~20 KB  | Группы вариаций |
| `PromotionsController.cs`     | ~9 KB   | Акции |
| `BillsController.cs`          | ~7 KB   | Тестовая генерация УПД (dev) |
| `RecycleBinController.cs`     | ~67 KB  | Корзина |
| `OneCIntegrationController.cs`| ~10 KB  | Импорт/экспорт документов 1С |
| `WebhookController.cs`        | ~52 KB  | Внешние интеграции |

---

## 2. Модуль `Orders` — центральный

### 2.1. Сущность `Order`
Файл: `v2/DDD/Orders/Entities/Order.cs`. Группы полей:

- **Идентификация**: `Id`, `OrderNumber` (до 50 симв., формат `<InfobasePrefix>-<seq>`).
- **Tenant**: `TargetCompanyId`, `OrganizationId`.
- **Контрагент**: `ContractorId`, `ContractorContractInfo` (TEXT).
- **Документы (история)**: коллекции `OrderBills`, `OrderUpds`. NotMapped: `CurrentBill = First(IsActive)`, `CurrentUpd = First(IsActive)`. При создании нового документа старые помечаются `IsActive=false`.
- **Статус**: `Status` (`OrderStatus`), `StatusHistoryJSON`, `StatusChangedAt`.
- **Доставка**: `DeliveryAddressId` (string — поддерживает int и GUID для интернет-магазина), `DeliveryAddress` (TEXT — снапшот строкой), `DeliveryAddressSnapshotJSON` (структурированный снапшот: Street/City/Region/PostalCode/Apartment), `DeliveryType`, `DeliveryMethod`, `TrackingNumber`.
- **Стоимость и НДС**:
  - `TotalPrice` — итог в **копейках**.
  - `ModifiedPrice` — ручная коррекция (опционально).
  - `DiscountPercent`, `TotalDiscountAmount` (копейки).
  - `DiscountApplicationMode` (`None | PerItem | WholeDocument`).
  - `VatCalculationMode` (`Included | OnTop`).
  - `VatApplicationMode` (`PerItem | WholeDocument`) + `DocumentVatRate`.
- **Снапшот реквизитов**: `BankRequesitesSnapshotJSON`.
- **Исполнители**: `AuthorProfileId`, `PickerProfileId?`, `DriverProfileId?` — все ссылаются на `EmployeeProfile`.
- **Флаги**: `IsPriority`, `Proveden`, `IsLongPicking`+`LongPickingActivatedAt`.
- **Производство**: `ManufacturingTaskId?` → `TaskEmploy`.
- **Чек-лист**: `PickingChecklistJSON`.
- **Прочее**: `AttachedFilesJSON`, `Comment`, `Logs`, `Comments` (JSON массив id комментариев).
- **Интернет-магазин**: `ExternalOrderId`, `ExternalOrderDataJSON` (например `metadata["carrier"]`).
- **Таймстемпы**: `CreatedAt`, `PaymentDate`, `StatusChangedAt`, `ReadyForPickingAt`, `CompletedAt`.

### 2.2. Перечисления

#### `OrderStatus`
```
Draft (0)              Picking (5)
Processing (1)         TransferredToTransport (6)
WaitingForPayment (2)  DeliveringByTransport (7)
PaymentConfirmed (3)   Delivering (8)
Manufacturing (4)      AwaitingPickup (9)
                       Completed (10)
                       Cancelled (11)
                       ReadyForShipment (12)
```

#### `KanbanColumn` → `OrderStatus`
- `Processing` → `[Processing]`
- `New`        → `[WaitingForPayment]`
- `InWork`     → `[PaymentConfirmed, ReadyForShipment, TransferredToTransport, DeliveringByTransport, Delivering, AwaitingPickup]`
- `Picking`    → `[Picking, Manufacturing]`
- `Completed`  → `[Completed]`
- `Cancelled`  → `[Cancelled]`

#### `OrderDeliveryType`
`SelfPickup = 1`, `CompanyDelivery = 2`, `TransportCompany = 3`.

#### `DiscountApplicationMode`
- `None` — скидки запрещены.
- `PerItem` — на позиции (контрагент/акции/явная); ИХ выбор — максимум.
- `WholeDocument` — только на весь заказ; на позиции запрещены.

#### `VatCalculationMode` / `VatApplicationMode`
- `Included` — НДС уже в цене / `OnTop` — начисляется поверх.
- `PerItem` — НДС из позиции / `WholeDocument` — единый из `Order.DocumentVatRate`.

### 2.3. Связанные сущности

#### `OrderItem`
- `NomenclatureId?`, `BillItemId?` (если из счёта).
- `Name`, `Quantity` (decimal 10,4), `UnitType`, `Price` (копейки), `BasePrice`, `DiscountPercent?`, `VatRate?`.
- `UrlPhotos` JSON / `PhotoUrls` NotMapped.
- `IsAvailable`, `RequiresManufacturing` — рассчитываются на основании `Nomenclature.StockQty`.

#### `OrderBill` (счёт)
- Может быть привязан к заказу (`OrderId`) или **standalone**.
- Снапшот-поля: реквизиты банка, адрес доставки, перевозчик, ИНН.
- Денормализованные строки: `Articles`, `Quantities`, `VatRates` (`"20, 10"`), `Prices`, `ItemTypes` (для 1С/отчётов).
- `CalculationSnapshotJson` (LONGTEXT) — `OrderDocumentCalculationSnapshot` с разбивкой по строкам/НДС/скидкам.
- `PDFurl` / `UnsignedPdfUrl` — пути к PDF (с подписью/печатью и без).
- `PaymentStatus` (`OrderBillPaymentStatus`), `PaymentDate`.
- `IsActive` — текущий, `Proveden` — проведён.
- 1С: `IsExportedTo1C`, `OneCExportId`, `ExportAttempts`, `LastExportError`, `NeedUpdateIn1C`.

#### `OrderUpd`
- Структурно почти как `OrderBill`, но без `PaymentStatus`.
- `OrderBillId` — на основе какого счёта сгенерирован.
- `UPDNumber` (строка с префиксом) + `Number` (int).

### 2.4. `IOrdersService` (инфраструктура)
Реализация — `OrdersMySQLService`. Методы:
- `AddOrderAsync` / `UpdateOrderAsync` / `DeleteOrderAsync`.
- `GetOrderByIdAsync(id)`, `GetOrderByExternalIdAsync(externalId, companyId)`.
- 4 перегрузки `GetOrdersAsync(...)` / `GetOrdersCountAsync(...)` с фильтрами:
  `companyId, organizationId(s), status(es), contractorId, isPriority, createdFrom/To, totalSumFrom/To, searchQuery, assignedProfileId + restrictToAssigned`, пагинация и сортировка.
- `GetPickingOrdersCountTodayAsync(pickerProfileId)`,
- `GetDeliveryOrdersCountTodayAsync(driverProfileId)`.

### 2.5. `OrdersUseCases` — фасад бизнес-логики
Файл: `v2/DDD/Orders/UseCases/OrdersUseCases.cs` (~7800 строк).

Зависимости (DI):
`IOrdersService`, `ApplicationContext`, `RecycleBinBaseService`, `ContractorUseCases`, `ContractorDiscountUseCases`, `ILogger`, `OrderConnectionManager`, `OrderNotificationService`, `IServiceScopeFactory`, `BillsWordDocumentService`, `IDocumentConversionService`, `InvoiceHtmlPdfService`, `IOneCExportService?`, `IOneCSyncService?`, `UserManager<User>`.

#### 2.5.1. Карта публичных методов (выборочно)
**Создание/обновление/удаление**
- `Task<Order> CreateOrderAsync(OrderCreateDTO dto, int authorProfileId, bool isDraft = false)`
- `Task<Order> UpdateOrderAsync(int orderId, OrderCreateDTO dto, int? profileId = null)`
- `Task UpdateOrderDirectlyAsync(Order)` — внутренний путь (например, после gRPC синка).
- `Task DeleteOrderAsync(int orderId, EmployeeProfile)` — soft-delete в корзину.
- `Task RestoreOrderAsync(int orderId, EmployeeProfile)`.

**Чтение**
- `Task<Order?> GetOrderByIdAsync(int)`, `GetOrderByExternalIdAsync(string, int companyId)`.
- 4 перегрузки `GetOrdersAsync(...)` / `GetOrdersCountAsync(...)`.

**Жизненный цикл**
- `Task<Order> ChangeOrderStatusAsync(int orderId, OrderStatus newStatus, int changedByProfileId, string? comment, bool skipNotification = false)`.
- `private Task HandleStatusTransitionAsync(Order, OrderStatus oldStatus, OrderStatus newStatus, int changedByProfileId, HttpContext?)` — все автоматические побочные действия.

**Сборка/доставка**
- `AssignPickerManuallyAsync`, `AssignDriverManuallyAsync`.
- `CreatePickingChecklistIfNeededAsync(int orderId)`.
- `CheckAndActivateLongPickingFlagAsync(Order)`.

**Производство**
- `HandleManufacturingTaskStatusChangedAsync(int taskId)` — при закрытии задачи переводит заказ из `Manufacturing` в `Picking`.

**Документы** — приватные `EnsureBillAsync`, `RegenerateBillPdfAsync`, `CreateUpdAsync` и т.п. (вызываются из `HandleStatusTransitionAsync` и из контроллеров `OrderBillsController` / `OrderUpdsController`).

**Уведомления** — `NotifyPickerAsync`, `NotifyDriverAsync` обёрнуты в `try/catch`, чтобы ошибка push/SignalR не валила транзакцию.

#### 2.5.2. Workflow `CreateOrderAsync` — детально

1. **Генерация номера**:
   - Если `dto.OrderNumber` задан — проверка уникальности по `(TargetCompanyId, OrderNumber)`.
   - Иначе `GenerateOrderNumberAsync(companyId)` + префикс из `CompanyRequesites.InfobasePrefix` → `BuildOrderNumber(prefix, seq)`.
2. Загрузка `Contractor`.
3. **Адрес доставки** (приоритет):
   - `dto.DeliveryAddressId` → берётся адрес из `Contractor.DeliveryAddresses` (через `_contractorUseCases.GetByIdWithAddressesForOrdersAsync`); сравнение `Id` универсальное (`ToString`↔`ToString` — поддержка GUID интернет-магазина).
   - Иначе если задана строка — `FindMatchingDeliveryAddress` пробует сматчить.
   - Если адрес найден — берутся `Id` + структурированный snapshot + нормализованная строка через `GetFullAddress()`.
   - Иначе → `ArgumentException("Адрес доставки обязателен для заполнения")`.
4. `Carrier` из DTO кладётся в `metadata["carrier"]` (для совместимости).
5. **Загрузка номенклатур** одним запросом по `nomenclatureIds` (для VAT/цен/остатков).
6. **Активные акции** — `BuildActivePromotionIndexesAsync(companyId)` → `{nomenclatureId → %}` и `{groupId → %}`.
7. **Цикл по позициям**:
   - **Базовая цена** — приоритет: `itemDto.BasePrice` → личная цена (`PersonalPriceModel`) → `Nomenclature.PriceCost` → `itemDto.Price`.
   - **Скидка**:
     - В режимах `None`/`WholeDocument` индивидуальные скидки **обнуляются**.
     - Иначе если задана `itemDto.DiscountPercent` — используется явно.
     - Иначе в `PerItem` — **максимум** из (а) скидки контрагента (`CalculatePriceWithDiscountAsync`) и (б) акции на номенклатуру или её `TargetGroupId`.
   - Флаг `hasAvailableDiscounts` сигнализирует фронту, что были доступные скидки (даже если не применены).
   - **Защита от продажи ниже себестоимости**: `finalPrice >= PricePurchase ?? PriceProduction`, иначе `ArgumentException`.
   - **НДС позиции**: `itemDto.VatRate` → `DocumentVatRate` (если режим WholeDocument) → `Nomenclature.VatRate` → `0`.
   - **Доступность позиции и `RequiresManufacturing`**: по `Nomenclature.StockQty` (`StockQty == null/0` или `< Quantity` → manufacturing).
   - **`finalItemTotal`**: для `OnTop+PerItem` НДС начисляется поверх; иначе уже включён.
8. **Скидка на заказ в целом** (только в `WholeDocument`) применяется к итогу. Иначе суммируются `DiscountAmount` по позициям.
9. **НДС на весь документ** (`OnTop+WholeDocument`) — однократно поверх итога.
10. **Начальный статус**:
    - явный `dto.Status` → его;
    - иначе `isDraft` → `Draft`;
    - иначе `ExternalOrderId != null` → `Processing` (заказ из интернет-магазина);
    - иначе → `WaitingForPayment`.
11. Создание `Order` + `Items` через навигационное свойство (`OrderId` проставит EF). История статусов инициализируется одной записью.
12. `_ordersService.AddOrderAsync(order)` + `_context.SaveChangesAsync()`.
13. Дальнейшие шаги делает `HandleStatusTransitionAsync` (если статус не `Draft`/`WaitingForPayment` — например, при `Processing` сразу формируется счёт и идёт экспорт в 1С).

#### 2.5.3. `ChangeOrderStatusAsync` + `HandleStatusTransitionAsync`

`ChangeOrderStatusAsync`:
1. Загрузка заказа, проверка прав через `EmployeeProfile`.
2. **Pre-валидация перехода** (`switch (newStatus)`):
   - `PaymentConfirmed` / `Manufacturing` / `Picking` / `AwaitingPickup` / `Completed` — требуется `CurrentBill`.
   - `ReadyForShipment` — запрещён для `SelfPickup`.
   - `TransferredToTransport` / `DeliveringByTransport` — только для `TransportCompany`.
   - `Delivering` — только для `CompanyDelivery`.
3. Обновление `Status`, `StatusChangedAt`, `StatusHistoryJSON`.
4. Берёт `HttpContext`, вызывает `HandleStatusTransitionAsync`.
5. Сохраняет, отправляет уведомления через **отдельный `IServiceScopeFactory`** (изоляция от текущего DbContext).

`HandleStatusTransitionAsync` — автоматические побочные действия:
- **`Cancelled`** — связанные `OrderBills` и `OrderUpds` (где `OrderId == order.Id`) переводятся в корзину. Standalone-документы не трогаются.
- **`Processing`** — если нет `CurrentBill`, автоматически создаётся счёт и инициируется экспорт в 1С (приём из интернет-магазина).
- **`WaitingForPayment`** — создаётся счёт, если его нет.
- **`PaymentConfirmed`** — точка «оплата подтверждена»; здесь может создаваться `ManufacturingTask` (если есть позиции с `RequiresManufacturing`).
- **`Picking`** — `ReadyForPickingAt = UtcNow`, назначается сборщик (если не задан) + push сборщику.
- **`ReadyForShipment`** — назначается водитель для `CompanyDelivery`/`TransportCompany`.
- **`TransferredToTransport`/`DeliveringByTransport`** — обновление трекинга/уведомлений.
- **`Completed`** — `CompletedAt = UtcNow`, финализация.

> Несмотря на эти проверки, **state-machine не закрыт**: фронт может попросить любой статус, бэк локально валидирует. Логика «правильного перехода» размазана.

### 2.6. `OrdersController`
Маршрут: `[Route("[controller]/v2/[action]")]` → `/Order/v2/<Action>`. `[Authorize]`, `?profileId=...`.

**Чтение**
- `GET GetById(id, profileId)` → `OrderDTO`.
- `POST GetList(profileId, ...30+ фильтров)` → `PagedResponseDTO<OrderListDTO>`. Сортировки: `createdAt | paymentDate | status | totalPrice | orderNumber | contractorName | deliveryAddress`.
- `GET GetPickerOrders(profileId, ...)` — заказы текущего сборщика.
- `GET GetDriverOrders(profileId, ...)` — заказы водителя.
- `POST GetKanban(profileId, pageSize=20, ...)` — N заказов на каждую `KanbanColumn`.

**Запись**
- `POST Create(dto, profileId, isDraft=false)` → `OrderDTO`.
- `POST Update(id, dto, profileId)` — полная актуализация (включая опциональные `PickerProfileId`/`DriverProfileId`).
- `POST ChangeStatus(id, newStatus, profileId, ...)`.
- `POST SetPriority(id, isPriority, profileId)`.
- `POST SetProveden({orderId, proveden}, profileId)`.
- `POST AssignPicker(id, pickerProfileId, profileId)`, `POST AssignDriver(id, driverProfileId, profileId)`.

**Чек-лист сборки**
- `POST UpdatePickingChecklist(orderId, profileId, body: List<CheckListModel>)` — записывается в `Order.PickingChecklistJSON`. Сборщик может закрывать только при `Status == Picking`.

**Документы**
- `POST CreateBill(id, profileId, billNumber?)` → `OrderBill`.
- `POST CreateTransferDocument(id, profileId)` → `OrderUpd` (на основе `CurrentBill`).
- `POST ConfirmBill(id, contractorId)` — подтверждение со стороны контрагента (публичная ссылка для оплаты).

**1С**
- `POST ExportBillTo1C(billId, profileId)`, `POST ExportUPDTo1C(updId, profileId)` → `OneCExportResult`.
- `GET GetActiveOrderBills` — `[ApiKeyAuth("1C-Integration")]` (не JWT).
- `POST UpdateOrderBillPaymentStatus(items, oneCSystemTime?)` — пакетное обновление статусов оплаты из 1С.

**Печать и медиа**
- `POST PrintPreview` — preview PDF без подписи/печати.
- Эндпоинты `MediaContent` для фотографий, прикреплённых к заказу.

### 2.7. `OrderBillsController` (`/OrderBill/v2/<Action>`)
- `GET GetList(profileId, searchQuery?, organizationId?, ...)` → `List<OrderBillListDto>`.
- `GET GetById(id, profileId)` → `OrderBillDto`.
- `POST CreateStandalone(profileId, dto)` — счёт без заказа.
- `PUT Update(profileId, body)` — изменение позиций/реквизитов; PDF перегенерируется.
- `POST ChangeNumber(profileId, dto)`.
- `POST SetProveden(profileId, dto)`.
- `DELETE Delete(id, profileId)` — soft-delete.
- `POST PrintPreview(dto)`.

Перед возвратом DTO выполняется `BuildCalculationSnapshotFromBill(bill)` — гарантирует, что `CalculationSnapshot` присутствует.

### 2.8. `OrderUpdsController` (`/OrderUpd/v2/<Action>`)
- `GET GetList(profileId, ...)` → `PagedResponseDTO<OrderUpdListDto>`.
- `GET GetById(id, profileId)`.
- `POST Create(profileId, dto)` — УПД из счёта (`OrderBillId` обязателен).
- `POST ChangeNumber(profileId, dto)`, `POST SetProveden`, `DELETE Delete`, `POST PrintPreview`.

### 2.9. SignalR / уведомления
- `MasterHub` (для CRM-фронта). Параметры в `Program.cs`: KeepAlive 8s, ClientTimeout 20s, Handshake 30s.
- `OrderConnectionManager` (singleton) — реестр клиентов по компаниям/профилям.
- `OrderNotificationService` (scoped) — отправка событий по заказу всем подключённым из `TargetCompanyId`.
- Для мобильных — push через Firebase, если SignalR недоступен.

### 2.10. Фоновые сервисы, влияющие на CRM
- `IsLongPicking` активируется фоновой проверкой при `ReadyForPickingAt < startOfToday`.
- `OrganizationProductionCalendarAutoCreateService` — авто-создание производственных календарей.
- `BillVerificationService` (singleton + HostedService) — сверка/проверка счетов.
- `BillIssuerService` — автогенерация счетов.
- `EdoPollingService` — Diadoc.
- `EventCleanupBackgroundService`, `RecycleBin*` сервисы.

---

## 3. Модуль `Contractors`

### 3.1. Сущность `Contractor` (~23 KB)
Группы полей:
- **Tenant**: `Id`, `TargetCompanyId`, `OrganizationIdListJSON` (с какими организациями работает).
- **Иерархия**: `IsGroup`, `TargetGroupId` (см. `fimbiz-contractor-hierarchy-1c.md`).
- **Юр.часть**: `Name`, `FullName`, `Type` (физ/юр/ИП), `INN`, `KPP`, `OGRN`, `OGRNIP`.
- **Контакты**: `Phone`, `Email`, `Address`, `AddressFact`, `Country`.
- **Руководство**: `DirectorFullName`, `DirectorPost`.
- **Корпоративный номер** (`CorporatePhone`) — для распознавания звонков.
- **Скидки**: `DiscountModelJSON`, `PersonalPricesJSON`.
- **Адреса доставки**: список `DeliveryAddressModel` (структурированный), может включать GUID интернет-магазина.
- **Флаги**: `IsBuyer`, `IsSupplier`.
- **1С**: `OneCExportId`, `IsExportedTo1C`, `NeedUpdateIn1C`, `LastExportError`.

### 3.2. Use-cases
- `ContractorUseCases.cs` (~71 KB) — CRUD, иерархия, переносы групп, привязка к организациям, Excel-импорт, поиск, корпоративные номера.
- `ContractorDiscountUseCases.cs` (~40 KB) — скидки, акции, личные цены, расчёт итоговой цены.

Ключевые методы:
- `CreateAsync(Contractor, EmployeeProfile)`, `UpdateAsync(int, Contractor, EmployeeProfile)`.
- `DeleteAsync(int, EmployeeProfile, bool deleteWithContents)` — soft-delete; `deleteWithContents` = удаление группы с содержимым.
- `DeleteGroupWithContentsAsync(int, EmployeeProfile)`.
- `GetByIdWithAddressesAsync(...)` / `GetByIdWithAddressesForOrdersAsync(...)` — две версии: вторая полнее, доступна при `IsAccessOrdersEdit` или `IsAccessInternetShopAdmin`.
- `GetListAsync(...)` / `GetListForOrdersAsync(...)` — пагинация + фильтры (`TargetOrganizationId(s)`, `IsBuyers`, `GroupId`, `WithCorporatePhone`, `SearchTerm`).
- `UpdateCorporatePhoneAsync`, `GetByCorporatePhoneAsync(phone, companyId)` — для входящих звонков.
- `CalculatePriceWithDiscountAsync(contractor, nomenclatureId, basePriceKopecks)` → `(finalPrice, appliedDiscount)`. Учитывает «лучшую» из доступных скидок.
- `GetPersonalPriceForNomenclature(contractor, nomenclatureId)` → `PersonalPriceModel?`.

### 3.3. `ContractorController` (`/Contractor/v2/<Action>`)
- `POST Create(dto, profileId)` — `IsAccessContractorsEdit`.
- `GET FindByInn(inn, profileId)` — `DaDataService` обогащает по ИНН (валидирует длину 10/12 цифр).
- `PUT Update(dto, profileId)`, `PUT UpdateCorporatePhone(...)`.
- `GET Get(id, profileId)` — выбирает «короткую» или «полную» загрузку (с адресами) в зависимости от прав.
- `POST GetList(dto, profileId)` — пагинация + проверка доступа к организациям через `OrganizationIdListJSON`.
- `GET GetByCorporatePhone(phoneNumber, profileId)`.
- `DELETE Delete(id, profileId, deleteWithContents=false)`, `DELETE DeleteGroupWithContents(id, profileId)`.
- Импорт/экспорт Excel (`ContractorExcelGenerator`, `ContractorExcelDocumentParser`).
- 1С: через опциональные `IOneCExportService`, `IOneCSyncService`.

> Большая часть кода контроллера — `try/catch` с подробным логированием:
> `UnauthorizedAccessException → 401`, `KeyNotFoundException → 404`, `ArgumentException/InvalidOperationException → 400`, `Exception → 500`.

---

## 4. Модуль `Nomenclatures`

### 4.1. Сущность `Nomenclature`
Файл: `v2/DDD/Nomenclatures/Entities/Nomenclature.cs`. Группы:
- **Tenant**: `TargetCompanyId`, `TargetOrganizationId`.
- **Дерево**: `IsGroup`, `TargetGroupId`.
- **Основа**: `Name`, `UrlPhotos` (JSON), `Description`, `Specifications` (JSON `[{Key, Value, PropertyType, IsGrouped}]`).
- **Цены**: `PriceCost` (продажа), `PriceProduction`, `PricePurchase`.
- **Характеристики**: `UnitType`, `Sku`, `VatRate`, `BarcodeValue`, `IsMarked`, `HasExcise`.
- **Витрина**: `IsListed`, `DisplayPriceMode`, `StockVisibility`, `ShowcaseDescription`.
- **Склад**: `StockQty`, `WeightKg`, `Dimensions`, `NetWeight`, `GrossWeight`.
- **Тип**: `ItemType` (`Product | Goods | Service | Raw`).
- **Группировка вариаций**: `ProductGroupId` (старая модель), `ProductGroupingListId` (новая, см. `API_PRODUCT_VARIANTS.md`).
- **QR**: `QrCodeId`, `PublicStoreUrl`, `NomenclatureUrl`.
- **1С**: `IsExportedTo1C`, `OneCExportId`, `NeedUpdateIn1C`, `ExportAttempts`, `LastExportError`.

### 4.2. Use-cases
- `NomenclatureUseCases.cs` (~67 KB) — CRUD, иерархия, удаление папок с содержимым, варианты, привязка к группам, видимость на витрине.
- `ProductGroupUseCases.cs` — старая модель.
- `ProductGroupingListUseCases.cs` — новая модель.
- `PublicShowcaseUseCases.cs` — публичная витрина.

### 4.3. `NomenclatureController` (`/Nomenclature/v2/<Action>`)
- `GET GetById(id, profileId)` → `NomenclatureDTO`.
- `GET GetWithVariants(id, profileId)` → `NomenclatureWithVariantsDTO` — с группировкой/вариациями.
- `GET GetWithVariantsForCard(id, profileId)` — облегчённая для карточки на витрине.
- `GET GetList(groupId?, profileId, organizationIds?, page, pageSize=50)` → пагинация.
- `GET GetAvailableGroups(parentGroupId?, profileId, organizationIds?, ...)` — для дерева.
- `POST Create(dto, profileId)`, `PUT Update(dto, profileId)`.
- `DELETE Delete(id, profileId, deleteWithContents=false)`, `DELETE DeleteFolderWithContents(id, profileId)`.
- `DELETE DeletePermanently(id, isGroup, profileId)` — окончательное удаление из корзины.
- `POST RestoreFolderWithContents(id, profileId)`.

### 4.4. Витрина (`PublicShowcaseController`)
- Публичный доступ (без логина).
- Возвращает каталог компании (категории, товары `IsListed`).
- Использует `Nomenclature.PublicStoreUrl` и `DisplayPriceMode`.

---

## 5. Модуль `Bills` (документоформирование)

Этот домен **не управляет бизнес-логикой счетов** (она в `OrderBill`/`OrderUpd`), а отвечает **за генерацию Word/PDF**:
- `BillsWordDocumentService.cs` (~173 KB) — заполнение Word-шаблонов (товары, реквизиты, подпись/печать как изображения, если `PastePrintsAndSignatures=true`).
- `IDocumentConversionService` / `DocumentConversionService` — DOCX → PDF.
- `InvoiceHtmlPdfService.cs` (~63 KB) — альтернативный путь HTML → PDF.
- `BillsController` — содержит `GET /api/Bills/test-upd` для **dev-проверки**. **Не помечен `[Authorize]`** — потенциальная проблема в проде.

### 5.1. Workflow печати
1. `OrderBillsController.PrintPreview` / `OrderUpdsController.PrintPreview` принимают `id`.
2. Получают сущность, формируют `WordModel`.
3. Загружают шаблон из `wwwroot/Files/Templates/*.docx`.
4. `BillsWordDocumentService.FillTransferDocumentDataAsync(template, model)` → DOCX в `temp`.
5. `DocumentConversionService.ConvertDocxToPdfAsync(docx, pdf)` → PDF в `temp`.
6. PDF читается в массив байт, временные файлы удаляются.
7. Возврат через `File(pdfBytes, "application/pdf", fileName)`.

---

## 6. Интеграции

### 6.1. Интернет-магазин (gRPC)
Прото: `Protos/contractor_sync.proto`, `order_sync.proto`, `permissions.proto`.
- `ContractorSyncServiceImpl` — двунаправленная синхронизация контрагентов.
- `OrderSyncServiceImpl` — заказы (создание/обновление статуса/позиций).
- `OrderCommentSyncServiceImpl` — комментарии.
- `PermissionsServiceImpl` — выдача разрешений.
- `InternetShopDeliveryAddressClient` — клиент для адресов из магазина (поэтому `DeliveryAddressId` поддерживает GUID).
- Для предотвращения цикла «магазин → FimBiz → магазин» используется `skipNotification=true` в `ChangeOrderStatusAsync`.

### 6.2. 1С
- Сервисы: `IOneCExportService`, `IOneCSyncService`, `OneCExportService`, `OneCSyncService` (~54 KB), `OneCTimeConverter`.
- `[ApiKeyAuth("1C-Integration")]` — отдельная схема ApiKey:
  - `GetActiveOrderBills` — выгрузка активных счетов.
  - `UpdateOrderBillPaymentStatus(...)` — приём статусов оплаты (с серверным временем 1С).
- Метаданные: `IsExportedTo1C`, `OneCExportId`, `ExportAttempts`, `LastExportError`, `NeedUpdateIn1C`.

### 6.3. Контур.Диадок (ЭДО)
- Конфиг `DiadocTest` в `appsettings.json`.
- `EdoPollingService` (HostedService) — периодически опрашивает Диадок.
- `EdoController` — UI-эндпоинты ЭДО.

### 6.4. DaData
- `DaDataService` — обогащение по ИНН (название, адрес, директор и т. п.).
- Используется при создании контрагента (`FindByInn`).

### 6.5. Telephony
- `ZvonokAPI` (по флагу `CallsConfiguration.Provider`).
- При входящем звонке `Contractor.GetByCorporatePhoneAsync` определяет, какой контрагент звонит, и контекст подгружается в CRM.

---

## 7. Сквозная инфраструктура

### 7.1. RecycleBin (soft-delete)
- Все CRM-сущности реализуют `IRecyclable`.
- `*UseCases.Delete*Async(...)` вызывают `RecycleBinBaseService` — переносят сущность в «корзину», `Remove` не делается.
- `RecycleBinController` отдаёт списки/восстановление/окончательное удаление.
- `RestoreFolderWithContents` рекурсивно поднимает содержимое.

### 7.2. EventSync
- `IEventSyncService` / `EventSyncService` + `EventCleanupBackgroundService` — синхронизация событий с подписчиками (push, gRPC, webhooks). Настройки `EventSyncOptions`.

### 7.3. Подписки (биллинг FimBiz)
- `SubscriptionCheckService` — проверка `SubscriptionArticulEnum.CrmOrders`.
- `SubscriptionBlockService` — фоновая блокировка протухших подписок.
- `SubscriptionCalculationService`, `BillIssuerService` — расчёт стоимости и выставление счетов **компании-клиенту FimBiz**.
- `TrialPeriodService` + `TrialPeriodExpirationService` — пробный период.

### 7.4. Логирование
- Serilog → Console + File (`/app/logs/log-.txt`, ротация по дню, 30 дней).
- `_logger?.LogInformation/LogWarning/LogError` с контекстом (`OrderId`, `ContractorId`, `ProfileId`).
- `IGlobalLogger` / `GlobalLogger` — единые сообщения.

### 7.5. Identity
- `AddCustomIdentity<User, IdentityRole>` — длина пароля 6, `RequireUniqueEmail = false` (логин email + телефон).
- `RoleInitializer.InitializeAsync` при старте.
- `ApplicationContextIdentity` отдельный DbContext, БД `fimbiz_identity`.
- **JWT lifetime = 5 минут** (`Jwt:Lifetime = "0:5:0"`). Refresh-токены не используются — обновление через `AccountTokenController` / cookie / re-auth.

### 7.6. Безопасность транспорта
- `ForwardedHeaders` middleware (для прокси).
- `SameSite=None`, `SecurePolicy=Always` для cookie.
- CORS: `AllowedOrigins` строго в конфиге; `AllowCredentials()` (без wildcard origin).
- SignalR: JWT может приходить через `?access_token=` (фоллбэк на header).

---

## 8. Глобальный воркфлоу (end-to-end)

### 8.1. Создание из CRM-фронта (менеджер вручную)
1. `POST /Order/v2/Create` с `OrderCreateDTO`, `profileId`.
2. Контроллер: `GetAuthorizedProfileAsync`, проверка подписки `crmorders`.
3. `OrdersUseCases.CreateOrderAsync(dto, profile.Id, isDraft=false)` (см. 2.5.2). Без `ExternalOrderId` → `Status = WaitingForPayment`.
4. `HandleStatusTransitionAsync(_, _, WaitingForPayment, _)`:
   - Создаётся **счёт (`OrderBill`)**, генерируется PDF, опционально подписывается.
   - Опционально стартует экспорт счёта в 1С.
5. SignalR-событие в `MasterHub` — обновляется список/канбан у всех онлайн-пользователей компании.
6. Менеджер видит заказ в колонке `New`, отправляет счёт контрагенту.

### 8.2. Подтверждение оплаты
1. Бухгалтер/1С обновляет `PaymentStatus`:
   - `OrderBillsController.Update`, либо
   - `[ApiKeyAuth] UpdateOrderBillPaymentStatus` пакетно из 1С.
2. Менеджер (или авто-мост) → `POST ChangeStatus(id, PaymentConfirmed, profileId)`.
3. `HandleStatusTransitionAsync(..., PaymentConfirmed)`:
   - Если есть позиции `RequiresManufacturing` — создаётся `ManufacturingTask`, статус → `Manufacturing`.
   - Иначе — может сразу переходить в `Picking`.
4. SignalR + push.

### 8.3. Производство → Сборка → Доставка
1. Закрытие `ManufacturingTask` → `OrdersUseCases.HandleManufacturingTaskStatusChangedAsync(taskId)` переводит заказ в `Picking`.
2. `HandleStatusTransitionAsync(..., Picking)`:
   - `ReadyForPickingAt = UtcNow`.
   - Если `PickerProfileId == null` — может назначаться (обычно вручную через `AssignPicker`).
   - SignalR/push сборщику.
3. Сборщик через моб.приложение/CRM:
   - `POST UpdatePickingChecklist(orderId, profileId, [items])` — отмечает пункты;
   - после полного закрытия → `POST ChangeStatus(id, ReadyForShipment / AwaitingPickup, ...)`.
4. `ReadyForShipment` (для `CompanyDelivery`/`TransportCompany`): назначается водитель, push.
5. Дальнейшие переходы зависят от типа доставки:
   - `SelfPickup`: `Picking → AwaitingPickup → Completed`.
   - `CompanyDelivery`: `Picking → ReadyForShipment → Delivering → Completed`.
   - `TransportCompany`: `Picking → ReadyForShipment → TransferredToTransport → DeliveringByTransport → AwaitingPickup → Completed`.
6. После `Completed` → `CompletedAt = UtcNow`. УПД создаётся вручную: `POST CreateTransferDocument(id, profileId)`.
7. Опционально: УПД отправляется в Диадок и/или экспортируется в 1С.

### 8.4. Заказ из интернет-магазина
1. Магазин по gRPC (`OrderSyncService.CreateOrder`) шлёт заказ.
2. `OrderSyncServiceImpl.CreateOrder` → `OrdersUseCases.CreateOrderAsync(dto, ..., isDraft=false)`, `Status = Processing`.
3. `HandleStatusTransitionAsync(..., Processing)` создаёт счёт + 1С.
4. Дальше — как в 8.2/8.3.
5. При смене статуса в FimBiz → magazine notify через `OrderNotificationService`. При смене статуса со стороны магазина — `skipNotification=true`, чтобы не зациклиться.

### 8.5. Отмена заказа
1. `POST ChangeStatus(id, Cancelled, profileId)`.
2. `HandleStatusTransitionAsync(..., Cancelled)`:
   - Все связанные `OrderBills` и `OrderUpds` (`OrderId == order.Id`) → корзина. Standalone-документы не трогаются.
   - SignalR/push.
3. Возврат остатков **не делается автоматически** — это отдельный модуль складов (вне CRM-зоны).

---

## 9. Сводный список «глобальных функций» CRM

| Функция                       | Где                                                                | Что внутри |
|------------------------------|---------------------------------------------------------------------|------------|
| Создание заказа              | `OrdersUseCases.CreateOrderAsync` / `OrdersController.Create`       | Номер, адрес, скидки, акции, личные цены, НДС, manufacturing, snapshot реквизитов |
| Обновление заказа            | `OrdersUseCases.UpdateOrderAsync` / `OrdersController.Update`       | Перерасчёт total, перепроверка manufacturing, регенерация PDF счёта |
| Смена статуса                | `ChangeOrderStatusAsync` + `HandleStatusTransitionAsync`            | Pre-валидация, история, side-effects (счёт/УПД/manufacturing/назначения/уведомления) |
| Назначение сборщика/водителя | `AssignPickerManuallyAsync` / `AssignDriverManuallyAsync`           | Проверка прав, уведомление, лог |
| Чек-лист сборки              | `OrdersController.UpdatePickingChecklist`                           | `PickingChecklistJSON`, контроль `Status == Picking` |
| Создание счёта               | `OrdersController.CreateBill` (+ `EnsureBill*`)                     | Снапшоты, расчёт, PDF, опционально 1С |
| Standalone-счёт              | `OrderBillsController.CreateStandalone`                             | Без `OrderId`, привязка `Organization`+`Contractor` |
| Создание УПД                 | `OrdersController.CreateTransferDocument` / `OrderUpdsController.Create` | Из текущего активного счёта, снапшот расчёта |
| Печать документа             | `*Controller.PrintPreview` + `BillsWordDocumentService` + `DocumentConversionService` | Word шаблон → DOCX → PDF |
| Корзина / восстановление     | `*UseCases.Delete/Restore` + `RecycleBinController`                 | Soft-delete всего CRM, поддержка групп с содержимым |
| Скидки                       | `ContractorDiscountUseCases.CalculatePriceWithDiscountAsync`        | Контрагент vs Акции vs Личные цены — лучший вариант |
| Контрагент по ИНН            | `ContractorController.FindByInn` + `DaDataService`                  | Внешний сервис обогащения |
| 1С — выгрузка/синхронизация  | `OrdersController.ExportBillTo1C/UPDTo1C` + `OneC*Service`          | API ключ, повторные попытки, метаданные |
| Интернет-магазин (gRPC)      | `OrderSyncServiceImpl` / `ContractorSyncServiceImpl`                | Двунаправленная синхронизация, защита от цикла через `skipNotification` |
| Канбан                       | `OrdersController.GetKanban` + `KanbanColumnExtensions`             | Маппинг колонок в статусы, отдельная пагинация на колонку |
| Уведомления                  | `OrderNotificationService` + `MasterHub` + Firebase                 | По событиям заказа в рамках компании |
| Подписки                     | `SubscriptionCheckService` + `SubscriptionBlockService`             | 403 при отсутствии `crmorders` |

---

## 10. Сильные стороны и риски

### 10.1. Сильные стороны
- **Снапшоты** реквизитов/адресов/расчётов в каждом документе → исторически правильный PDF, независимо от изменений контрагента/банка.
- **Денормализация** в `OrderBill`/`OrderUpd` (`Articles`, `Quantities`, `VatRates`, `Prices`, `ItemTypes`) — быстрый 1С/отчёты.
- DI-граф полностью построен в `Program.cs` — легко найти регистрацию любого сервиса.
- Чёткое разделение **владелец / редактор / сборщик / водитель** + дополнительный контур через `IsAccessInternetShopAdmin`.
- Сквозная **корзина** (RecycleBin) — нет «hard-delete» в CRM.
- Подробное логирование с контекстом (хорошо для прод-диагностики).

### 10.2. Риски и техдолг
- **`OrdersUseCases.cs` ~7800 строк / ~419 KB** — god-class, нарушает SRP, плохо тестируется. Желательно разбить по сценариям (Create/Update/Status/Manufacturing/Picking/Notifications).
- **State-machine статусов не закрыт**: проверки разнесены по `switch (newStatus)` в pre-валидации и в `HandleStatusTransitionAsync`, легко рассинхронизировать.
- **JWT lifetime = 5 минут** без refresh-токенов — частые re-auth, требует аккуратного UX.
- **Секреты в `appsettings.json`** (`Jwt:SymmetricSigningKey`, API keys, MySQL пароли). Минимум — env / user-secrets. Если репо публичный — серьёзный риск.
- **`BillsController.test-upd`** — без `[Authorize]`. Должен быть отключён в проде или закрыт.
- **`profileId` в query string** читается в логах прокси/аналитике. Лучше — заголовок `X-Profile-Id` или клейм.
- **Дублирующая авторизация**: проверки прав есть и в контроллере, и в use-case. Возможны места, где забыли проверить.
- Много схем аутентификации: gRPC + REST + SignalR + ApiKey + JWT — нужна чёткая политика.
- **Контроллеры по 100+ KB** — сложно ревьюить.
- **Round-to-int** через `Math.Round` в нескольких местах — потенциальные копеечные расхождения. Нужны интеграционные тесты (есть `tests/TimeTracingServer.FinancialJournal.IntegrationTests`).
- В `BillsController.test-upd` `wordPath` чистится только в одной ветке — потенциальная утечка временных файлов.
- В `OrdersUseCases` есть комментарии «ВАЖНО: явно завершаем синхронные операции...» — следы прошлых проблем с конкурентным `DbContext`.

### 10.3. Что можно перенести в `CRM_system` 1:1 (или почти)
- **Модель статусов заказа** + переходы (упрощённо: единый state-machine).
- **DiscountApplicationMode / VatCalculationMode / VatApplicationMode** — концепция режимов скидок и НДС.
- **Снапшоты** банковских реквизитов и адреса доставки в `Order`/`OrderBill`/`OrderUpd`.
- **Денормализованные снапшоты строк** (`CalculationSnapshotJson`).
- **Канбан-колонки** через маппинг → статусы.
- **Чек-лист сборки** в JSON + закрытие сборщиком.
- **RecycleBin** как сквозная политика.
- Идея **`EmployeeProfile`** — поддержка нескольких профилей одного `User`.

### 10.4. Что НЕ стоит копировать
- Размер god-class’ов (`OrdersUseCases`).
- `profileId` в query string как стандарт — лучше claim/header.
- Хранение секретов в `appsettings.json`.
- 5-минутный JWT без refresh.
- Тестовые эндпоинты в боевых контроллерах.

---

## 11. Точки расширения

- **State machine заказов** — собрать в один сервис `OrderStatusTransitions` с явной таблицей разрешённых переходов и side-effects. Позволит покрыть unit-тестами все сценарии.
- **Pricing engine** — выделить `IPricingEngine` (контрагент vs акции vs личные цены vs документ-уровень) с явными политиками. Сейчас логика дублируется в `CreateOrderAsync` и `UpdateOrderAsync`.
- **Manufacturing** — оформить как доменный сервис: вход — Order, выход — `ManufacturingTask`. Сейчас рассыпано по нескольким случаям `switch`.
- **Документы (счёт/УПД)** — единый pipeline: `Build → Snapshot → Render(DOCX|HTML) → Convert(PDF) → Sign(stamp/signature) → Persist`. Сейчас `BillsWordDocumentService` и `InvoiceHtmlPdfService` живут параллельно.
- **gRPC и REST** — общий фасад, чтобы `OrderSyncServiceImpl` и `OrdersController` использовали один API без дублирования валидаций.

---

## 12. TL;DR

CRM в TimeTracingServer — это **полноценный модуль учёта заказов** с:
- продуманной моделью **Order ↔ OrderBill ↔ OrderUpd ↔ Contractor ↔ Nomenclature**;
- автоматической оркестрацией pipeline через `ChangeOrderStatusAsync` + `HandleStatusTransitionAsync`;
- интеграциями с интернет-магазином (gRPC), 1С (REST + ApiKey), Диадок (Polling), DaData, Zvonok;
- многослойной авторизацией (`User → EmployeeProfile → IsAccess*` флаги + подписка `CrmOrders`);
- soft-delete через RecycleBin, snapshots реквизитов/адресов/расчётов в документах, канбан-доской, чек-листами сборки.

Главные **архитектурные риски**: god-class в `OrdersUseCases`, незакрытый state-machine, секреты в репо, короткий JWT без refresh, дублирующая авторизация. Эти места стоит **не копировать** при заимствовании логики в текущий `CRM_system`.
