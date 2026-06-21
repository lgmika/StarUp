# StartupConnect Backend - Tai lieu trien khai tong the

## 1. Muc dich tai lieu

Tai lieu nay tong hop toan bo backend StartupConnect da duoc xay dung tu khi khoi tao du an den hien tai. Noi dung bao gom kien truc, cac phase chuc nang, API, mo hinh du lieu, quy tac nghiep vu, bao mat, realtime, AI, file storage, payment, background jobs, database, kiem thu va van hanh production.

Trang thai tai thoi diem lap tai lieu:

- Backend build thanh cong voi 0 warning va 0 error.
- 103/103 automated tests thanh cong.
- Entity Framework model dong bo voi migration moi nhat.
- Docker image production cua API build thanh cong.
- Smoke test va health check thanh cong.
- Load test 500 request, concurrency 25: 100% HTTP 200, khong co request loi.
- NuGet dependency audit khong phat hien package co lo hong da biet.
- Cac tich hop Stripe, S3 va SMTP da co implementation/configuration, nhung viec xac minh tren dich vu production that van can credentials va ha tang cua chu du an.

## 2. Cong nghe va kien truc

### 2.1 Cong nghe chinh

- ASP.NET Core Minimal API tren .NET 10.
- Entity Framework Core va PostgreSQL 17.
- JWT Bearer Authentication va role-based authorization.
- SignalR cho realtime.
- Swagger/OpenAPI cho API documentation.
- xUnit cho automated tests.
- Docker, Docker Compose va EF migration bundle cho deployment.
- Caddy cho HTTPS reverse proxy trong production compose.
- Ollama cho mo hinh AI ma nguon mo chay local/private.
- AWS SDK for .NET cho S3.
- Stripe.net cho Stripe Checkout va webhook.

### 2.2 Cau truc solution

```text
src/
  Api/             HTTP endpoints, middleware, auth policies, SignalR, health checks
  Application/     DTO, interfaces, contracts va application policies
  Domain/          Entities, enums, role constants va domain state
  Infrastructure/  EF Core, service implementations, providers va workers
  Shared/          ApiResponse, error models va shared exceptions
tests/
  StartupConnect.Tests/
tools/
  Deployment, smoke test, load test, backup va restore scripts
```

Backend duoc to chuc theo modular monolith. HTTP layer chi phu trach binding, authorization va response; logic nghiep vu nam trong cac service cua Infrastructure thong qua interface cua Application. Domain khong phu thuoc vao API hoac provider ben ngoai.

### 2.3 Response va loi chuan

Tat ca REST response dung `ApiResponse<T>` voi cac truong thanh cong, thong diep va du lieu. Loi duoc chuyen thanh response nhat quan boi `ExceptionMiddleware`, gom validation error, authentication/authorization error, not found, conflict va internal error. Request logging co correlation id de truy vet.

## 3. Phase 1 - Project Setup

- Tao solution va cac project Api, Application, Domain, Infrastructure, Shared va Tests.
- Cau hinh dependency injection theo module.
- Ket noi PostgreSQL bang Npgsql/EF Core.
- Tao `AppDbContext`, design-time factory va migration workflow.
- Cau hinh Swagger, JSON serialization, CORS va health check.
- Tao Dockerfile, Docker Compose local va scripts khoi dong/kiem tra.
- API root: `GET /api/v1/`.
- Health endpoints:
  - `GET /api/v1/health`
  - `GET /api/v1/health/live`
  - `GET /api/v1/health/ready`
- Health endpoints duoc mien global rate limiter de monitoring khong bi tra 429.

## 4. Phase 2 - Authentication va Authorization

### API

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

### Da trien khai

- Dang ky, dang nhap, logout va lay current user.
- Password hashing an toan; khong luu plain-text password.
- JWT access token va refresh-token rotation.
- Refresh token duoc luu dang hash, co expiry, revoke time, replacement chain va IP metadata.
- Tuy chon gui refresh token bang secure httpOnly cookie trong production, van duy tri response contract cu cho client hien tai.
- Email verification token va password reset token chi luu hash, co expiry va chi su dung mot lan.
- PostgreSQL advisory lock ngan hai request dong thoi cung dung mot verify/reset token.
- Reset password revoke hang loat cac refresh token dang hoat dong trong cung transaction.
- Forgot-password va resend-verification khong lam lo tai khoan co ton tai hay khong.
- Role mac dinh va policies cho VerifiedUser, Business, Investor, Moderator va Admin.
- Tai khoan Suspended, Banned hoac Deleted khong the dang nhap.

## 5. Phase 3 - User Profile, Skill, CV va Portfolio

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

- Profile co visibility rules va contact visibility.
- User co the them/xoa skill va so nam kinh nghiem.
- CV metadata tach khoi file bytes.
- Upload PDF gioi han mac dinh 5 MB; kiem tra extension, MIME, filename traversal va magic signature `%PDF`.
- Portfolio duoc dung trong profile, search va report target.

## 6. Phase 4 - Project Core

```http
POST   /api/v1/projects
GET    /api/v1/projects
GET    /api/v1/projects/{projectId}
PUT    /api/v1/projects/{projectId}
POST   /api/v1/projects/{projectId}/submit-review
POST   /api/v1/projects/{projectId}/publish
POST   /api/v1/projects/{projectId}/close
GET    /api/v1/projects/me/owned
GET    /api/v1/projects/me/joined
GET    /api/v1/projects/{projectId}/versions
POST   /api/v1/projects/{projectId}/save
DELETE /api/v1/projects/{projectId}/save
GET    /api/v1/users/me/saved-projects
```

- Tao draft tu dong tao founder member va project version dau tien.
- Project co stage, status, visibility, recruiting state, required roles va required skills.
- State flow gom Draft, PendingReview, Approved/Published, NeedsImprovement, Rejected, Hidden, Closed va Archived theo action hop le.
- Cap nhat project da publish chuyen lai PendingReview khi noi dung can duyet lai.
- Public, Limited, Private va InvestorOnly duoc enforcement tai query/service, khong chi an tren UI.
- Project detail, list, search va recommendation deu ton trong visibility/NDA/access grant.
- Saved projects va project views phuc vu discovery/dashboard.

## 7. Phase 5 va Phase 11 - AI Service

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

- `IAIProvider` tach orchestration khoi model provider.
- `MockAIProvider` deterministic cho local/test.
- `OllamaAIProvider` goi mo hinh free/open-source qua Ollama HTTP API.
- Ho tro cac model nhu llama3.1, qwen2.5, mistral hoac model Ollama tuong thich.
- Daily quota theo user va system setting.
- Luu AI request, response, review va recommendation de audit/tai su dung.
- AI output chi mang tinh ho tro, khong tu dong phe duyet hoac thay doi domain state.
- Production tu choi Mock provider de tranh gia lap ket qua that.

## 8. Phase 6 - Moderator Review

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

- Chi Moderator/Admin co quyen.
- Moi decision bat buoc reason.
- Luu moderation review, audit log, notification va realtime event.
- Transaction va advisory lock ngan phe duyet/trang thai bi cap nhat trung lap khi request dong thoi.

## 9. Phase 7 - Application Flow

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

- Chi verified user co the ung tuyen.
- Kiem tra project recruiting, visibility, membership, duplicate active application va NDA/access.
- Founder/co-founder quan ly application.
- Luu day du `ApplicationStatusHistory`.
- Accept application tao dung mot active `ProjectMember`, ke ca khi co concurrent requests.
- Phat notification, activity va SignalR event khi status thay doi.

## 10. Phase 8 - Investor Flow

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

- Investor profile co ticket range, sectors va investment preferences.
- InvestorOnly project khong bi lo detail truoc khi investor co accepted interest/access grant.
- Accept interest tao project access grant; neu project bat buoc NDA thi chuyen `AcceptedPendingNda`.
- Transaction/advisory lock bao ve state transitions.
- Notification, audit va realtime cho cac thay doi interest.

## 11. Phase 9 - NDA Module

```http
GET  /api/v1/nda/templates
POST /api/v1/nda/templates
POST /api/v1/nda/templates/{templateId}/versions
GET  /api/v1/projects/{projectId}/nda/current
POST /api/v1/projects/{projectId}/nda/accept
GET  /api/v1/projects/{projectId}/nda/agreements
GET  /api/v1/users/me/nda-agreements
```

- Admin quan ly template va immutable template versions.
- Project co the gan NDA version hien hanh.
- Agreement luu user, project, template version, accepted time va audit metadata.
- Accept NDA mo khoa access grant dang cho NDA cua investor.
- Query project, application va investor deu enforcement NDA tai backend.

## 12. Phase 12 - Notifications

```http
GET    /api/v1/notifications
GET    /api/v1/notifications/unread-count
POST   /api/v1/notifications/{notificationId}/read
POST   /api/v1/notifications/read-all
DELETE /api/v1/notifications/{notificationId}
```

- Pagination va filter theo unread/type/date range.
- NotificationType chuan hoa theo ProjectModeration, Application, InvestorInterest, Chat, Report, NDA, Interview, Billing va System.
- Moi notification co `actionUrl`, `resourceType`, `resourceId` khi co domain target.
- Chi owner duoc doc, mark-read hoac soft-delete notification cua minh.
- `read-all` dung bulk database update, khong load tat ca row vao memory.
- Realtime push cho create/read/read-all.

## 13. Phase 13 - Reports

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

- Target types: User, Project, Message, Portfolio va Application.
- Validate target ton tai va quyen nhin target.
- Khong cho user report chinh minh.
- Duplicate active report cung reporter/target duoc collapse.
- Reason codes chuan hoa: Spam, Scam, Harassment, HateSpeech, InappropriateContent, CopyrightViolation, FakeInformation, PrivacyViolation, Other.
- Moderator action co reason, ReportAction, audit log, notification va realtime.

## 14. Phase 14 - Admin va User Management

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

- Admin khong the suspend/ban chinh minh.
- He thong khong cho xoa role Admin cuoi cung.
- Suspend/ban revoke active refresh tokens.
- Audit logs co actor, action, resource, reason va timestamp.

### Admin Settings va Projects

- `/api/v1/admin/settings` doc/cap nhat setting theo key/group/value/type/isReadonly/updatedAt.
- Settings bao gom AI quota, upload limit, realtime, moderation, payment/subscription va email/notification flags.
- Admin project listing xem ca non-public projects, co filter/search/pagination/status/stage/visibility/owner email.
- Admin actions ho tro hide, restore, archive, close va force status update voi reason.
- Moi mutation deu ghi audit log.

### Admin Plans va Quotas

- Admin quan ly subscription plans va usage quotas qua cac endpoint admin subscription-plan.
- Ho tro create/update plan va create/update/delete quota.
- Thay doi plan/quota duoc audit.

## 15. Phase 15 - Project Team Management

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

- Invitation het han sau 7 ngay va co email notification.
- Ownership transfer dung one-time confirmation token.
- Member role/history va audit duoc luu.
- Founder khong the roi/bi xoa khi chua transfer ownership.

## 16. Phase 16 - Interview Scheduling

```http
POST /api/v1/applications/{applicationId}/interviews
GET  /api/v1/applications/{applicationId}/interviews
GET  /api/v1/users/me/interviews
PUT  /api/v1/interviews/{interviewId}
POST /api/v1/interviews/{interviewId}/cancel
POST /api/v1/interviews/{interviewId}/complete
```

- Luu thoi gian bang UTC `DateTimeOffset`.
- Validate start/end, lich trong qua khu, URL meeting, dia diem in-person va xung dot lich participant.
- Khong sinh meeting URL gia.
- Luu participants va status history.
- Gui email, notification va realtime khi interview thay doi.

## 17. Phase 17 - Chat va Messaging

```http
POST   /api/v1/conversations
GET    /api/v1/conversations
GET    /api/v1/conversations/{conversationId}
GET    /api/v1/conversations/{conversationId}/messages
POST   /api/v1/conversations/{conversationId}/messages
POST   /api/v1/conversations/{conversationId}/read
DELETE /api/v1/messages/{messageId}
```

- Chi participant co quyen truy cap conversation.
- Direct conversation duoc de-duplicate bang database advisory lock.
- Message list dung cursor `before` va page size gioi han.
- Message soft-delete.
- Read receipts duoc bulk insert/update hieu qua.
- Attachment phai la active file thuoc user; chan dangerous executable content types.
- New message tao notification va realtime event.
- Chua trien khai end-to-end encryption; day la access-controlled messaging.

## 18. Phase 18 va Production-5 - SignalR Realtime

```http
GET /hubs/startupconnect
GET /hubs/realtime
```

- Hub bat buoc JWT authentication.
- Connection tu dong join `user:{userId}`.
- `JoinProject` chi thanh cong neu caller co project access.
- `JoinConversation` chi thanh cong neu caller la participant.
- Domain service chi phu thuoc `IRealtimeNotifier`, implementation SignalR nam tai API layer.
- Events gom notification, chat message, project status, application status, investor interest, interview va report changes.
- Realtime setting co the tat/bat tu system settings.
- Hien tai phu hop single API instance; multi-instance can Redis SignalR backplane.

## 19. Phase 19 - Activity Feed

```http
GET /api/v1/feed
GET /api/v1/projects/{projectId}/activities
```

- Activity entity rieng, khong dung audit log lam feed.
- Ho tro project created/published/updated, member joined/role changed, application received/accepted, investor interest va milestones.
- Visibility: Public, MembersOnly, Private.
- Anonymous chi thay public activities cua public/limited published projects.
- Private application/investor events khong lo qua public feed.

## 20. Phase 20 - Advanced Search

```http
GET /api/v1/search/projects
GET /api/v1/search/members
GET /api/v1/search/investors
GET /api/v1/search/suggestions
```

- PostgreSQL full-text search qua `to_tsvector`, `plainto_tsquery` va relevance ranking.
- GIN indexes cho project text, profile, investor profile, roles, skills va user names.
- Filter project theo status, stage, role, skill, location, remote, date va sort.
- Filter member theo keyword, skill, experience, location va verified.
- Filter investor theo keyword va ticket range.
- Tat ca list co pagination va page-size cap.
- Search ton trong visibility, NDA va role access.

## 21. Phase 21 - Recommendation Engine

```http
GET  /api/v1/recommendations/projects
GET  /api/v1/recommendations/members
GET  /api/v1/projects/{projectId}/recommended-members
POST /api/v1/recommendations/{recommendationId}/dismiss
```

- Rule-based deterministic scoring co breakdown de UI giai thich.
- Project scoring dua tren skill overlap, open roles, saved behavior va application history.
- Member scoring dua tren required skills, experience, open roles, membership history va profile completeness.
- Khong dung protected/sensitive attributes.
- Dismissal duoc luu bang stable recommendation id.

## 22. Phase 22 - Dashboard Analytics

```http
GET /api/v1/dashboard/me
GET /api/v1/projects/{projectId}/dashboard
GET /api/v1/investors/me/dashboard
```

- Date range va timezone offset duoc ho tro; database van luu UTC.
- User dashboard: applications by status, interviews, joined/saved projects va profile completion.
- Founder dashboard: views, saves, applications, conversion, team, investor interests, NDA va status history.
- Investor dashboard: interest statuses, NDA pending, access grants va saved projects.
- Project dashboard chi founder/co-founder; investor dashboard chi Investor.
- Aggregate co short in-memory cache.
- Project views duoc daily de-duplicate; owner views bi loai.

## 23. Phase 23 va Production-4 - File Storage

```http
GET    /api/v1/files/me
DELETE /api/v1/files/{fileId}
GET    /api/v1/files/{fileId}/download-url
GET    /api/v1/files/download
POST   /api/v1/cvs/upload
```

- `IFileStorageService` tach file storage khoi business logic.
- Local provider cho development.
- S3 provider dung AWS SDK, private bucket va presigned download URLs.
- Ho tro AWS S3 va S3-compatible service endpoint/path-style.
- Object key duoc generate, khong dung user filename lam storage path.
- Metadata luu trong `files`; file bytes khong luu PostgreSQL.
- Server-side encryption SSE-S3 co the bat.
- Neu metadata transaction that bai, object vua upload duoc cleanup.
- Production fail-fast neu van chon Local provider.

## 24. Phase 24 va Production-2 - Payment/Subscription

```http
GET  /api/v1/subscriptions/plans
GET  /api/v1/subscriptions/me
POST /api/v1/subscriptions/checkout
POST /api/v1/subscriptions/cancel
POST /api/v1/subscriptions/resume
POST /api/v1/webhooks/payments
```

- `IPaymentProvider` tach domain khoi payment vendor.
- Mock provider co HMAC webhook cho local/test.
- Stripe provider that dung Stripe Checkout va Stripe webhook signature verification.
- Checkout URL chi duoc chap nhan theo configured return URL policy.
- Frontend khong duoc tu khai bao thanh toan thanh cong; chi verified webhook thay doi subscription.
- Webhook event luu idempotently, tranh xu ly lai event cung provider id.
- Payment transaction va subscription state duoc audit.
- Khong luu thong tin the.
- Seed plans: Free, Pro, Investor Pro va Business, kem usage quotas.

## 25. Phase 25 - Background Jobs

```http
GET  /api/v1/admin/background-jobs
POST /api/v1/admin/background-jobs/run
GET  /api/v1/admin/email-outbox
POST /api/v1/admin/email-outbox/{messageId}/retry
```

- Hosted `StartupConnectBackgroundWorker` chay maintenance jobs.
- PostgreSQL advisory locks dam bao nhieu API instance khong chay cung job batch.
- `BackgroundJobExecution` luu status, attempts, processed count, lock key va failure.
- Jobs hien co:
  - Xoa verification/password-reset tokens het han.
  - Revoke refresh token het han va prune revoked token cu.
  - Het han project invitations.
  - Cleanup orphan files qua storage provider.
  - Het han subscriptions.
  - Prune email outbox va execution history theo retention.
- Critical jobs duoc persisted trong database, khong dung in-memory queue de tranh mat viec khi restart.

## 26. Email va Transactional Outbox

- `IEmailService` co Development provider va SMTP provider.
- Development ghi `.eml` vao `App_Data/emails`.
- SMTP ho tro SSL, credential, timeout va retry.
- Email verification, reset password, project invitation va interview notification dung template renderer.
- Email outbox message duoc commit cung transaction voi domain change.
- Payload outbox ma hoa AES-GCM.
- Dispatcher claim batch bang PostgreSQL `FOR UPDATE SKIP LOCKED` va lease.
- Retry co attempts/next-attempt; failed terminal messages co admin retry endpoint.
- Production validation bat buoc SMTP, HTTPS app URL, verified sender domain va outbox encryption key.

## 27. Database va migrations

### 27.1 Cac migration theo thu tu

1. `InitialCreate`
2. `AddAuthentication`
3. `AddUserProfiles`
4. `AddProjectCore`
5. `AddAIModule`
6. `AddModeratorReview`
7. `AddApplicationFlow`
8. `AddInvestorFlow`
9. `AddNdaModule`
10. `AddNotificationActionUrl`
11. `ExtendReportModule`
12. `AddAdminUserManagement`
13. `AddProjectTeamManagement`
14. `AddInterviewScheduling`
15. `AddChatMessaging`
16. `AddActivityFeed`
17. `AddAdvancedSearchIndexes`
18. `AddRecommendationEngine`
19. `AddPaymentSubscriptionModule`
20. `AddBackgroundJobs`
21. `AddSystemSettingsAndProductionReadinessApis`
22. `AddAIResponsesProjectViewsAndEmailOutbox`
23. `AddChatQueryIndexes`

### 27.2 Nhom bang chinh

- Identity: users, roles, user_roles, refresh_tokens, verification/reset tokens.
- Profiles: user_profiles, skills, user_skills, cvs, portfolios, files.
- Projects: projects, versions, visibility settings, roles, skills, members, invitations, ownership transfers, histories, access grants, saves, views.
- Applications/interviews: project_applications, attachments, status histories, interviews, participants, histories.
- Investors/NDA: investor_profiles, investor_interests, nda_templates, versions, agreements.
- Communication: notifications, conversations, participants, messages, attachments, read receipts, email outbox.
- Governance: moderation reviews, reports, report actions, audit logs, system settings.
- AI/discovery: AI requests/reviews/recommendations, recommendation dismissals, activities.
- Billing/operations: plans, subscriptions, payment transactions, webhook events, quotas, background job executions.

EF Core model da duoc kiem tra bang `has-pending-model-changes`; khong co model change chua duoc migrate tai thoi diem lap tai lieu.

## 28. Security hardening

- JWT signing key duoc validate do dai va reject placeholder trong production.
- Role/policy enforcement tai endpoint va object-level access tai service/query.
- Refresh-token cookie co HttpOnly, Secure va SameSite config.
- Password/token khong duoc log hoac luu plain text.
- CORS chi cho configured origins; local cho ports 3000/5173.
- Forwarded headers chi tin known proxy.
- HTTPS redirect, HSTS va security headers trong production.
- Global request body limit va upload-specific limit.
- Rate limiting rieng cho general API, authentication va payment webhook.
- Health endpoint khong bi rate limit.
- Webhook body gioi han 256 KB va bat buoc signature.
- File path traversal, executable attachment va unsafe public storage bi chan.
- Database transactions/advisory locks cho cac state transition co nguy co race condition.
- Production startup fail-fast neu secret/provider/config khong an toan.
- Docker production: read-only API filesystem, tmpfs, drop all capabilities va `no-new-privileges`.

## 29. Monitoring, logging va health

- Structured request logging voi method, path, status, duration va correlation id.
- Tuy chon JSON console logs cho log collector production.
- Slow-request threshold cau hinh duoc.
- Liveness chi xac nhan process; readiness kiem tra PostgreSQL va dependency can thiet.
- Health endpoints on dinh cho Docker, reverse proxy va monitoring probes.
- Audit logs rieng cho admin/moderator/security-sensitive actions.
- Background execution records cho phep quan sat maintenance failures.

## 30. Backup, restore va deployment

- `tools/backup-postgres.ps1`: tao PostgreSQL custom-format dump.
- `tools/restore-postgres.ps1`: restore vao database test hoac replace co explicit confirmation.
- Backup/restore da duoc test bang temp database va kiem tra lai du lieu.
- `tools/smoke-test.ps1` va `.sh`: kiem tra liveness, readiness, search projects, members va suggestions.
- `tools/load-test.mjs`: load test khong can dependency ngoai.
- `tools/deploy-production.ps1/.sh`: pull image, migrate, start stack va verify.
- `tools/rollback-production.ps1`: rollback ve API/frontend image tags truoc.
- Production compose co PostgreSQL, one-shot migration, API, frontend va Caddy HTTPS.
- EF migration bundle nam trong API image va chay truoc API.
- Database nam tren internal Docker network, khong expose public.

## 31. Automated tests

Tong cong hien tai: 103 tests.

Test suites bao gom:

- API response va exception middleware.
- Authentication password hashing va token behavior.
- Profile, project, team, moderation va application rules.
- Investor, NDA, interview, chat va notification.
- Report, admin, activity, search va recommendation.
- Dashboard, AI, file storage va subscriptions.
- Stripe/payment return URL/webhook behavior.
- Background jobs, email, email outbox va realtime.
- Pagination va production readiness/security configuration.

Lenh kiem tra backend:

```powershell
dotnet restore
dotnet build StartupConnect.slnx --no-restore
dotnet test StartupConnect.slnx --no-restore --no-build
dotnet format StartupConnect.slnx --no-restore --verify-no-changes
dotnet ef migrations has-pending-model-changes `
  --project src/Infrastructure/StartupConnect.Infrastructure.csproj `
  --startup-project src/Api/StartupConnect.Api.csproj `
  --no-build
dotnet list StartupConnect.slnx package --vulnerable --include-transitive
```

Ket qua gan nhat:

- Build: thanh cong, 0 warning, 0 error.
- Tests: 103 passed, 0 failed, 0 skipped.
- Format: khong co thay doi can format.
- Migration: model da dong bo.
- Vulnerability audit: khong co NuGet package bi canh bao.
- Load: 500/500 HTTP 200, failure rate 0, p95 duoi 100 ms trong lan kiem tra local gan nhat.

## 32. Development seed data

`DevelopmentDataSeeder` tao du lieu review cho cac role va domain chinh, gom Admin, Moderator, Investor, Business va verified user. Seeder tao cac ban ghi mau can thiet cho project, profile va cac luong nghiep vu. Seeder chi chay trong Development va khong duoc dung lam production data.

## 33. Cau hinh local

```powershell
docker compose up -d postgres
dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Host=localhost;Port=55432;Database=startupconnect;Username=startupconnect;Password=<password>" `
  --project src/Api/StartupConnect.Api.csproj
dotnet tool restore
dotnet ef database update `
  --project src/Infrastructure/StartupConnect.Infrastructure.csproj `
  --startup-project src/Api/StartupConnect.Api.csproj
dotnet run --project src/Api/StartupConnect.Api.csproj --urls http://localhost:8080
```

Swagger: `http://localhost:8080/swagger`  
API: `http://localhost:8080/api/v1`  
Health: `http://localhost:8080/api/v1/health/ready`  
SignalR: `http://localhost:8080/hubs/realtime`

## 34. Cau hinh production bat buoc

Code va container da san sang nhan cac bien sau, nhung gia tri that phai do chu ha tang cung cap:

- PostgreSQL database/user/password.
- JWT signing key ngau nhien manh.
- Allowed hosts, app origin, API URL va DNS domains.
- SMTP host/credential, from email va verified SPF/DKIM/DMARC domain.
- Email outbox AES encryption key.
- Ollama internal URL va model da pull.
- S3 bucket/region/IAM role hoac access key.
- File signing key.
- Stripe API key va webhook secret.
- Caddy/ACME email va server ports 80/443.

Khong dua secret that vao Git. Dung secret manager, CI environment secrets, Docker secrets hoac bien moi truong tren server.

## 35. Gioi han va cong viec phu thuoc ha tang

Backend code da hoan thien cho pham vi hien tai. Cac muc sau khong the xac nhan end-to-end neu chua co tai khoan/credentials production:

- Giao dich Stripe that va webhook tu Stripe production.
- Upload/download object tren S3 bucket that va IAM policy production.
- Gui email qua SMTP/domain da xac minh.
- Cap HTTPS certificate tren domain that.
- Monitoring alert, log retention va backup schedule tren server/cloud that.
- Multi-instance SignalR; can Redis backplane neu scale ngang.

Khi co credentials va server, khong can thay doi contract frontend. Chi can dua secrets vao environment, deploy stack, chay migration, smoke test va provider-specific verification.

## 36. Ket luan

StartupConnect backend hien la mot modular monolith day du cho cac luong Authentication, Profile, Project, Moderation, Application, Investor, NDA, Admin, Reports, Team, Interviews, Chat, Realtime, Activity, Search, Recommendations, Dashboard, AI, File Storage, Billing, Email Outbox va Background Jobs. Cac boundary quan trong deu co authorization, visibility enforcement, audit, transaction/concurrency protection va automated tests. He thong da san sang cho frontend tich hop va san sang deployment sau khi duoc cung cap credentials/ha tang production that.
