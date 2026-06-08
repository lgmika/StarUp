# StartupConnect

StartupConnect is a modular monolith web platform for startup founders, members, investors, moderators, and admins.

## Phase 1 Stack

- Backend: ASP.NET Core Web API
- Runtime in this workspace: .NET 10
- ORM: Entity Framework Core
- Database: PostgreSQL
- API docs: Swagger / OpenAPI
- Container: Docker Compose
- Tests: xUnit

## Solution Structure

```text
src/
  StartupConnect.Api/
  StartupConnect.Application/
  StartupConnect.Domain/
  StartupConnect.Infrastructure/
  StartupConnect.Shared/
tests/
  StartupConnect.Tests/
```

## Run Locally

```bash
dotnet restore
dotnet build StartupConnect.slnx
dotnet test StartupConnect.slnx
```

Start PostgreSQL:

```bash
docker compose up -d postgres
```

Apply migrations:

```bash
dotnet tool restore
dotnet ef database update --project src/StartupConnect.Infrastructure/StartupConnect.Infrastructure.csproj --startup-project src/StartupConnect.Api/StartupConnect.Api.csproj
```

Run API:

```bash
dotnet run --project src/StartupConnect.Api/StartupConnect.Api.csproj
```

Useful endpoints:

```http
GET /api/v1/
GET /api/v1/health
GET /swagger
```

