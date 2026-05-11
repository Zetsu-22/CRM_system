# CrmSystem.Tests

Тестовый проект (xUnit + FluentAssertions + Mvc.Testing + Testcontainers.PostgreSql).

## Запуск

### Только юнит-тесты (без Docker)
```powershell
dotnet test CrmSystem.Tests --filter "Category!=Integration"
```

### Все тесты (нужен запущенный Docker Desktop)
Интеграционные тесты в `Integration/` поднимают временный Postgres через Testcontainers.
```powershell
dotnet test CrmSystem.Tests
```

## Структура

- `Integration/PostgresWebAppFactory.cs` — наследник `WebApplicationFactory<Program>`,
  поднимает API в памяти и подменяет `ConnectionStrings:CrmDb`/`AuthDb` на временный Postgres-контейнер.
- `Integration/HealthEndpointTests.cs` — smoke-тест `GET /health`, `GET /api/v1/health`.
- `Integration/AuthEndpointTests.cs` — register + login → JWT.

Все интеграционные тесты помечены `[Trait("Category","Integration")]`.
