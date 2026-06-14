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
GET /api/v1/health/live
GET /api/v1/health/ready
GET /swagger
```

## Production Security Configuration

The API validates critical security settings on startup. Outside `Development`, replace all `DEV_ONLY` or `CHANGE_ME` secrets, restrict `AllowedHosts`, and configure production CORS origins. Startup fails fast if these values are unsafe.

Key settings:

```text
Jwt:SigningKey
AllowedHosts
Cors:AllowedOrigins
Security:UseForwardedHeaders
Security:RequireHttpsRedirection
Security:KnownProxies
Security:RefreshTokenCookie:Enabled
Security:RefreshTokenCookie:Secure
Security:RefreshTokenCookie:SameSite
```

Refresh tokens are still returned in `ApiResponse<AuthResponse>` for existing clients. Production can additionally enable `Security:RefreshTokenCookie:Enabled=true` to write the refresh token as an httpOnly cookie. When `SameSite=None`, `Secure=true` is required.

## Phase 2 Auth Endpoints

```http
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh-token
POST /api/v1/auth/logout
POST /api/v1/auth/verify-email
POST /api/v1/auth/resend-verification
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

Register and forgot-password now send links through `IEmailService`. In development, emails are written to `src/Api/App_Data/emails` when running the API locally. Set `Email:Provider=Smtp` and the `Email:Smtp:*` settings, or the matching `Email__...` environment variables, to use a real SMTP provider.

Email verification and password reset tokens are stored as hashes, expire by configuration, and reset tokens can only be used once.

Production email startup validation requires `Email:Provider=Smtp`, a non-local `Email:FromEmail`, an HTTPS `Email:AppBaseUrl`, and SMTP host/username/password. Set `Email:RequireVerifiedSenderDomain=true` with `Email:VerifiedSenderDomain=your-domain.com` after SPF/DKIM/DMARC are verified in your mail provider. SMTP sends use `Email:Smtp:TimeoutSeconds` and `Email:Smtp:MaxRetryAttempts` to avoid hanging requests during transient provider failures.

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

CV uploads accept PDF files up to 5 MB, validate extension, MIME type, and PDF file signature, then store metadata in the `files` table. File bytes are stored through `IFileStorageService`, not in PostgreSQL.

Production file storage supports `FileStorage:Provider=S3` through the official AWS S3 SDK. Use a private bucket and serve files through short-lived presigned URLs returned by `GET /api/v1/files/{fileId}/download-url`. Required settings are `FileStorage:S3:BucketName` and either `FileStorage:S3:Region` or `FileStorage:S3:ServiceUrl`. `FileStorage:S3:AccessKeyId` and `SecretAccessKey` may be omitted when the host provides an IAM role/default AWS credentials. `FileStorage:S3:KeyPrefix` keeps application objects isolated inside the bucket, and `UseServerSideEncryption=true` enables SSE-S3 for uploads.

Production startup validation rejects `FileStorage:Provider=Local`.

## Production Operations

Request logging is controlled by the `Observability` section. Each response includes `X-Correlation-Id`, and slow requests are logged at warning level based on `Observability:SlowRequestThresholdMs`. Set `Observability:UseJsonConsole=true` in production container platforms that collect structured stdout logs.

API rate limiting uses ASP.NET Core fixed-window rate limiting. Default limits are configured under `RateLimiting`, with tighter limits for `/api/v1/auth` and wider limits for `/api/v1/webhooks`. Production startup validation rejects `RateLimiting:Enabled=false`.

Health endpoints:

```http
GET /api/v1/health       # full health response
GET /api/v1/health/live  # process liveness, no database check
GET /api/v1/health/ready # readiness, includes database check
```

PostgreSQL backups can be created from the local Docker database:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\backup-postgres.ps1
```

The script writes custom-format `.dump` files to `backups/` by default and keeps the latest 14 backups. Restore with:

```powershell
docker exec -i startupconnect-postgres pg_restore -U startupconnect -d startupconnect --clean --if-exists < .\backups\startupconnect_YYYYMMDD_HHMMSS.dump
```

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

Phase 11 splits AI orchestration from model providers. The backend supports:

- `AI:Provider=Mock` for deterministic local development and automated tests.
- `AI:Provider=Ollama` for free/open-source local models through Ollama.

Recommended local models: `llama3.1`, `qwen2.5`, `mistral`, or another Ollama model that fits your machine.

```bash
ollama serve
ollama pull llama3.1
```

Then set:

```json
"AI": {
  "Provider": "Ollama",
  "DailyQuota": 20,
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.1",
    "TimeoutSeconds": 120
  }
}
```

AI results are advisory only, persisted for review, and limited to 20 requests per user per UTC day by default.

## Phase 12 Notification Endpoints

```http
GET    /api/v1/notifications
GET    /api/v1/notifications?status=unread&type=System&from=2026-01-01T00:00:00Z&to=2026-12-31T23:59:59Z&page=1&pageSize=20
GET    /api/v1/notifications/unread-count
POST   /api/v1/notifications/{notificationId}/read
POST   /api/v1/notifications/read-all
DELETE /api/v1/notifications/{notificationId}
```

Users can only read, mark, or delete their own notifications. Delete is a soft delete. Notification responses include `actionUrl`, `resourceType`, `resourceId`, `isRead`, `readAt`, and `createdAt`.

## Phase 13 Report Endpoints

```http
POST /api/v1/reports
GET  /api/v1/users/me/reports
GET  /api/v1/users/me/reports/{reportId}

GET  /api/v1/moderator/reports
GET  /api/v1/moderator/reports/{reportId}
POST /api/v1/moderator/reports/{reportId}/assign
POST /api/v1/moderator/reports/{reportId}/investigate
POST /api/v1/moderator/reports/{reportId}/resolve
POST /api/v1/moderator/reports/{reportId}/dismiss
```

Supported report target types: `User`, `Project`, `Message`, `Portfolio`, `Application`.
Reason codes: `Spam`, `Scam`, `Harassment`, `HateSpeech`, `InappropriateContent`, `CopyrightViolation`, `FakeInformation`, `PrivacyViolation`, `Other`.

Duplicate active reports from the same reporter for the same target are collapsed into the existing report. Moderator actions require a reason, create `ReportAction` rows, write audit logs, and notify the reporter when a report is resolved or dismissed.

## Phase 14 Admin And User Management Endpoints

```http
GET    /api/v1/admin/dashboard
GET    /api/v1/admin/users
GET    /api/v1/admin/users/{userId}
POST   /api/v1/admin/users/{userId}/suspend
POST   /api/v1/admin/users/{userId}/unsuspend
POST   /api/v1/admin/users/{userId}/ban
POST   /api/v1/admin/users/{userId}/unban
POST   /api/v1/admin/users/{userId}/roles
DELETE /api/v1/admin/users/{userId}/roles/{roleCode}
GET    /api/v1/admin/roles
GET    /api/v1/admin/audit-logs
```

Admin APIs require the `Admin` role. User status supports `Active`, `Suspended`, `Banned`, and `Deleted`. Suspend and ban revoke active refresh tokens, and authentication blocks suspended, banned, or deleted users. Admins cannot suspend or ban themselves, and the system blocks removing the last `Admin` role.

## Phase 15 Project Team Management Endpoints

```http
GET    /api/v1/projects/{projectId}/members
GET    /api/v1/projects/{projectId}/members/{memberId}
PATCH  /api/v1/projects/{projectId}/members/{memberId}
DELETE /api/v1/projects/{projectId}/members/{memberId}

POST   /api/v1/projects/{projectId}/invitations
GET    /api/v1/projects/{projectId}/invitations
POST   /api/v1/project-invitations/{invitationId}/accept
POST   /api/v1/project-invitations/{invitationId}/reject
POST   /api/v1/project-invitations/{invitationId}/cancel

POST   /api/v1/projects/{projectId}/transfer-ownership
POST   /api/v1/projects/{projectId}/transfer-ownership/accept
POST   /api/v1/projects/{projectId}/members/{memberId}/promote-cofounder
POST   /api/v1/projects/{projectId}/members/{memberId}/remove-cofounder
POST   /api/v1/projects/{projectId}/leave
```

Project team management adds `ProjectInvitation`, `ProjectOwnershipTransfer`, and `ProjectMemberHistory`. Invitations expire after 7 days, ownership transfer uses a one-time confirmation token, and member role changes are written to member history plus audit logs. Founder cannot leave or be removed before ownership transfer.

## Phase 16 Interview Scheduling Endpoints

```http
POST /api/v1/applications/{applicationId}/interviews
GET  /api/v1/applications/{applicationId}/interviews
GET  /api/v1/users/me/interviews
PUT  /api/v1/interviews/{interviewId}
POST /api/v1/interviews/{interviewId}/cancel
POST /api/v1/interviews/{interviewId}/complete
```

Interview scheduling adds `ProjectInterview`, `InterviewParticipant`, and `InterviewStatusHistory`. Founders/co-founders can schedule or update interviews, applicants can view their own interviews, times are stored as UTC `DateTimeOffset`, and the backend validates time ranges, past schedules, online meeting URLs, in-person locations, and participant schedule conflicts. The backend does not generate fake meeting URLs.

## Phase 17 Chat And Messaging Endpoints

```http
POST   /api/v1/conversations
GET    /api/v1/conversations
GET    /api/v1/conversations/{conversationId}
GET    /api/v1/conversations/{conversationId}/messages?before={cursor}&pageSize=20
POST   /api/v1/conversations/{conversationId}/messages
POST   /api/v1/conversations/{conversationId}/read
DELETE /api/v1/messages/{messageId}
```

Chat adds `Conversation`, `ConversationParticipant`, `Message`, `MessageAttachment`, and `MessageReadReceipt`. Users can only read conversations where they are participants. Messages use soft delete, message pagination uses a `before` cursor, direct conversations are de-duplicated, attachments must be owned active files and dangerous executable content types are blocked. New messages create notifications for other participants.

Chat is access-controlled messaging only. End-to-end encryption is not implemented yet.

## Phase 18 SignalR Real-Time

```http
GET /hubs/startupconnect
```

The SignalR hub requires the same JWT authentication as the REST API. On connect, the backend automatically joins the caller to `user:{userId}`. Clients can call `JoinProject(projectId)` only when they have project access, and `JoinConversation(conversationId)` only when they are a conversation participant. Matching `LeaveProject(projectId)` and `LeaveConversation(conversationId)` methods are also available.

Published client events:

```text
notification.created
notification.read
notifications.readAll
message.created
message.read
application.statusChanged
interview.changed
```

Realtime is wired through `IRealtimeNotifier`, so domain services do not depend directly on SignalR. The current deployment supports a single API instance only. Add a Redis SignalR backplane before running multiple API instances behind a load balancer.

## Phase 19 Activity Feed Endpoints

```http
GET /api/v1/feed?page=1&pageSize=20
GET /api/v1/projects/{projectId}/activities?page=1&pageSize=20
```

Activity feed adds a dedicated `Activity` entity instead of rebuilding business feed items from audit logs. Supported activity types include project created/published/updated, member joined, member role changed, application received/accepted, investor interest received, funding milestone, and project milestone completed.

Feed visibility is enforced at query time:

```text
Public
MembersOnly
Private
```

Anonymous users only see public activities for published public/limited projects. Authenticated users can also see member-only or private activities when they have project membership, project ownership, co-founder/founder permissions, or an active access grant. Private application and investor-interest events are not exposed through the public feed.

## Phase 20 Advanced Search Endpoints

```http
GET /api/v1/search/projects?keyword=ai&stage=MVP&requiredRole=backend&page=1&pageSize=20
GET /api/v1/search/members?keyword=backend&skillId={skillId}&verifiedOnly=true&page=1&pageSize=20
GET /api/v1/search/investors?keyword=fintech&minTicketSize=10000&page=1&pageSize=20
GET /api/v1/search/suggestions?keyword=design&limit=10
```

Search uses PostgreSQL full-text search through `to_tsvector` / `plainto_tsquery`, relevance ranking, pagination, and capped page sizes. The migration adds GIN indexes for project text, public member profile text, investor profile text, project roles, skills, and user names. Search does not use audit logs and does not use full-table `ToLower().Contains()` matching.

Project search respects project visibility and NDA rules. Anonymous users only see published public/limited projects. Authenticated users can also see projects they own, projects where they are active members, or projects with an active access grant. Member search only returns public profiles. Investor search requires an Investor, Business, Moderator, or Admin role.

## Phase 21 Recommendation Engine Endpoints

```http
GET  /api/v1/recommendations/projects?page=1&pageSize=20
GET  /api/v1/recommendations/members?page=1&pageSize=20
GET  /api/v1/projects/{projectId}/recommended-members?page=1&pageSize=20
POST /api/v1/recommendations/{recommendationId}/dismiss
```

Recommendations are rule-based first and include scoring breakdowns so the frontend can explain why an item appears. Project recommendations use visible/recruiting projects, skill overlap, open roles, saved-project behavior, and application history. Member recommendations use public profiles only, required skill overlap, skill experience, open roles, previous project membership, and profile completeness.

The engine does not use sensitive attributes such as gender, religion, ethnicity, or other protected data. It does not create AI requests during page refresh. AI can be added later for explanation/reranking, but the foundation remains deterministic rule scoring. Users can dismiss recommendations through a stable recommendation id stored in `recommendation_dismissals`.

## Phase 22 Dashboard Analytics Endpoints

```http
GET /api/v1/dashboard/me?from=2026-01-01T00:00:00Z&to=2026-01-31T23:59:59Z&timezoneOffsetMinutes=420
GET /api/v1/projects/{projectId}/dashboard?from=2026-01-01T00:00:00Z&to=2026-01-31T23:59:59Z&timezoneOffsetMinutes=420
GET /api/v1/investors/me/dashboard?from=2026-01-01T00:00:00Z&to=2026-01-31T23:59:59Z&timezoneOffsetMinutes=420
```

Dashboard aggregates are served through `IDashboardService` with a short in-memory cache. Date ranges use `DateTimeOffset`; `timezoneOffsetMinutes` lets the client align ranges with the user's local timezone while queries remain stored in UTC.

User dashboard includes applications by status, upcoming interviews, joined projects, saved projects, and profile completion. Founder project dashboard includes saved count, applications, application conversion, team size, investor interests, NDA agreements, and recent application status history for that project. Investor dashboard includes interested projects, interest status counts, NDA pending, accepted access grants, and saved projects.

Project dashboard requires founder/co-founder access. Investor dashboard requires the Investor role. `ProjectViews` currently returns `0` because the backend does not yet have a project view tracking table; no fake view metrics are generated.

## Phase 23 Cloud File Storage

```http
POST /api/v1/cvs/upload
GET  /api/v1/files/{fileId}/download-url
GET  /api/v1/files/download?path={storageKey}&expires={unixSeconds}&signature={signature}
```

File storage is abstracted behind `IFileStorageService`. The current provider is `LocalFileStorageService`, selected by `FileStorage:Provider=Local`. Uploaded file metadata stays in the `files` table while file bytes are stored under the configured private storage root. The API stores generated storage keys instead of user filenames and does not expose absolute filesystem paths.

CV upload validation checks file size, `.pdf` extension, `application/pdf` MIME type, file name path traversal, and PDF file signature. Download URLs are short-lived signed URLs. If database metadata creation fails after upload, the uploaded file is deleted to avoid orphan files. Deleting an uploaded CV soft-deletes metadata and asks the storage provider to delete the stored file.

Cloud providers such as S3 or Azure Blob can be added by implementing `IFileStorageService` and switching `FileStorage:Provider`. This build fails fast for unknown providers instead of silently storing files in the wrong backend.

## Phase 24 Payment And Subscription

```http
GET  /api/v1/subscriptions/plans
GET  /api/v1/subscriptions/me
POST /api/v1/subscriptions/checkout
POST /api/v1/subscriptions/cancel
POST /api/v1/subscriptions/resume
POST /api/v1/webhooks/payments
```

Payment is abstracted behind `IPaymentProvider`. The current provider is `MockPaymentProvider`, selected by `Payments:Provider=Mock`, with HMAC-signed webhooks using the `X-Payment-Signature` header. Checkout creates a pending transaction only; subscription state is activated or changed by verified webhook events, which are stored idempotently in `payment_webhook_events`.

Set `Payments:Provider=Stripe` to use Stripe Checkout through the official `Stripe.net` SDK. Required production settings are `Payments:ApiKey=sk_...`, `Payments:WebhookSecret=whsec_...`, and `Payments:CheckoutBaseUrl=https://your-frontend/billing/checkout`. Stripe webhooks are verified with the `Stripe-Signature` header and normalized into the internal subscription events used by this service.

The module adds `SubscriptionPlan`, `UserSubscription`, `PaymentTransaction`, `PaymentWebhookEvent`, and `UsageQuota`. Seeded plans are Free, Pro, Investor Pro, and Business, each with quota rows for resources such as AI requests, active projects, file storage, investor access, and advanced analytics.

The backend does not trust payment state from the frontend and does not store card data. Unknown payment providers fail fast until a real provider such as Stripe is implemented through `IPaymentProvider`.

## Phase 25 Background Jobs

```http
GET  /api/v1/admin/background-jobs
POST /api/v1/admin/background-jobs/run
```

Background maintenance runs through `StartupConnectBackgroundWorker` and `IBackgroundJobService`. The worker uses PostgreSQL advisory locks so multiple backend instances do not run the same maintenance batch at the same time. Each job writes a row to `background_job_executions` with status, attempt count, processed item count, lock key, and failure message when it reaches the failed state.

Current jobs clean expired email/password tokens, revoke expired refresh tokens, expire pending project invitations, clean old orphan files through `IFileStorageService`, expire subscriptions whose current period has ended, and record an analytics aggregate pass. Retry count, batch size, interval, enabled flag, and lock key are controlled by the `BackgroundJobs` config section.

Important production rule: critical work is persisted in the database, not kept in an in-memory queue. Email retry, async AI processing, notification digests, and separate search indexing can be added as persisted job tables or provider-backed queues using the same execution-log pattern.

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
