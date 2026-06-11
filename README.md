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
  Api/
  Application/
  Domain/
  Infrastructure/
  Shared/
frontend/
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
dotnet ef database update --project src/Infrastructure/StartupConnect.Infrastructure.csproj --startup-project src/Api/StartupConnect.Api.csproj
```

Run API:

```bash
dotnet run --project src/Api/StartupConnect.Api.csproj
```

Run frontend:

```bash
cd frontend
npm install
npm run dev
```

Frontend defaults to `NEXT_PUBLIC_API_BASE_URL=http://localhost:8080/api/v1`.
Backend CORS allows `localhost` and `127.0.0.1` on ports `3000` and `5173`.
Authentication tokens are stored in `localStorage` for this phase. Notification, Admin, and user Report screens use a mock service until backend endpoints are added.

Useful endpoints:

```http
GET /api/v1/
GET /api/v1/health
GET /swagger
```

## Phase 2 Auth Endpoints

```http
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh-token
POST /api/v1/auth/logout
POST /api/v1/auth/verify-email
POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password
GET  /api/v1/auth/me
```

Basic test flow:

```bash
docker compose up -d postgres
dotnet ef database update --project src/Infrastructure/StartupConnect.Infrastructure.csproj --startup-project src/Api/StartupConnect.Api.csproj
dotnet run --project src/Api/StartupConnect.Api.csproj
```

Register returns a `devEmailVerificationToken` until a real email provider is added.

## Phase 3 Profile Endpoints

```http
GET    /api/v1/profiles/me
POST   /api/v1/profiles/me
PUT    /api/v1/profiles/me
GET    /api/v1/profiles/{userId}
GET    /api/v1/skills
POST   /api/v1/users/me/skills
DELETE /api/v1/users/me/skills/{skillId}
GET    /api/v1/cvs/me
POST   /api/v1/cvs
POST   /api/v1/cvs/upload
PUT    /api/v1/cvs/{cvId}
DELETE /api/v1/cvs/{cvId}
POST   /api/v1/portfolios
```

CV uploads accept PDF files up to 5 MB and store metadata in the `files` table.

## Phase 4 Project Core Endpoints

```http
GET    /api/v1/projects
GET    /api/v1/projects/{projectId}
POST   /api/v1/projects/drafts
PUT    /api/v1/projects/{projectId}
DELETE /api/v1/projects/{projectId}
POST   /api/v1/projects/{projectId}/submit-review
POST   /api/v1/projects/{projectId}/close
GET    /api/v1/projects/me/owned
GET    /api/v1/projects/me/joined
GET    /api/v1/projects/{projectId}/versions
POST   /api/v1/projects/{projectId}/save
DELETE /api/v1/projects/{projectId}/save
GET    /api/v1/users/me/saved-projects
```

Creating a draft automatically creates the founder project member and the first project version.

## Phase 5 AI Support Endpoints

```http
POST /api/v1/projects/{projectId}/ai/suggestions
POST /api/v1/projects/{projectId}/ai/review
GET  /api/v1/projects/{projectId}/ai/reviews
GET  /api/v1/projects/{projectId}/ai/reviews/latest
POST /api/v1/ai/recommendations/{recommendationId}/apply
POST /api/v1/applications/{applicationId}/ai/cover-letter
POST /api/v1/projects/{projectId}/ai/investor-summary
GET  /api/v1/projects/{projectId}/investor-summary
```

Phase 5 uses `MockAIService`. AI results are advisory only, persisted for review, and limited to 20 requests per user per UTC day.

## Phase 6 Moderator Review Endpoints

```http
GET  /api/v1/moderator/dashboard
GET  /api/v1/moderator/projects/pending
GET  /api/v1/moderator/projects/{projectId}
POST /api/v1/moderator/projects/{projectId}/approve
POST /api/v1/moderator/projects/{projectId}/request-improvement
POST /api/v1/moderator/projects/{projectId}/reject
POST /api/v1/moderator/projects/{projectId}/hide
POST /api/v1/moderator/projects/{projectId}/restore
```

Moderator endpoints require `Moderator` or `Admin` role. Every decision requires a reason, writes audit log, stores moderation history, and notifies the project founder.

## Phase 7 Application Flow Endpoints

```http
POST /api/v1/projects/{projectId}/applications
GET  /api/v1/projects/{projectId}/applications
GET  /api/v1/projects/{projectId}/applications/{applicationId}
GET  /api/v1/users/me/applications
POST /api/v1/projects/{projectId}/applications/{applicationId}/withdraw
POST /api/v1/projects/{projectId}/applications/{applicationId}/shortlist
POST /api/v1/projects/{projectId}/applications/{applicationId}/interview
POST /api/v1/projects/{projectId}/applications/{applicationId}/accept
POST /api/v1/projects/{projectId}/applications/{applicationId}/reject
```

Applicants must be verified users. Founders/co-founders can manage applications, and accepting an application creates a `ProjectMember`.

## Phase 8 Investor Flow Endpoints

```http
GET  /api/v1/investors/me/profile
POST /api/v1/investors/me/profile
PUT  /api/v1/investors/me/profile
GET  /api/v1/investors/projects
GET  /api/v1/projects/{projectId}/investor-summary
POST /api/v1/projects/{projectId}/investor-interests
GET  /api/v1/investors/me/interests
GET  /api/v1/projects/{projectId}/investor-interests
POST /api/v1/projects/{projectId}/investor-interests/{interestId}/accept
POST /api/v1/projects/{projectId}/investor-interests/{interestId}/reject
POST /api/v1/projects/{projectId}/investor-interests/{interestId}/request-more-info
POST /api/v1/projects/{projectId}/investor-interests/{interestId}/withdraw
```

Investor endpoints require the `Investor` role for investor-owned actions. Accepting an interest grants project access unless the project requires NDA, in which case the status becomes `AcceptedPendingNda`.

## Phase 9 NDA Module Endpoints

```http
GET  /api/v1/nda/templates
POST /api/v1/nda/templates
POST /api/v1/nda/templates/{templateId}/versions
GET  /api/v1/projects/{projectId}/nda/current
POST /api/v1/projects/{projectId}/nda/accept
GET  /api/v1/projects/{projectId}/nda/agreements
GET  /api/v1/users/me/nda-agreements
```

Admins manage NDA templates and template versions. Users can view and accept the active project NDA, founders/co-founders can view project NDA agreements, and accepting an NDA unlocks pending investor access when the related interest was waiting for NDA acceptance.
