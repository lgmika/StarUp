# StartupConnect Frontend - Tai lieu trien khai tong the

## 1. Tong quan

Frontend StartupConnect duoc xay dung bang Next.js App Router, TypeScript va Tailwind CSS. Ung dung cung cap cac luong nghiep vu cho Guest, User, VerifiedUser, Business/Founder, Investor, Moderator va Admin.

Thu muc frontend:

```text
frontend/
  src/app/          Pages va layouts theo Next.js App Router
  src/components/   UI, auth, workspace va feature components
  src/hooks/        Session va reusable hooks
  src/lib/          API client, auth storage, permissions, validation
  src/services/     Lop ket noi backend API
  src/stores/       Zustand stores
  src/types/        TypeScript DTO va enum
  e2e/              Playwright end-to-end tests
```

URL local:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:8080/api/v1`
- PostgreSQL host port: `55432`

## 2. Cong nghe

- Next.js 15 App Router
- React 19
- TypeScript
- Tailwind CSS 3
- Axios
- TanStack React Query
- Zustand
- React Hook Form
- Zod
- SignalR
- Lucide React
- Sonner toast
- Playwright
- ESLint

## 3. He thong UI

### 3.1 Thanh phan dung chung

- `AppShell`: sidebar, top navigation, breadcrumb, notification menu va account menu.
- `Panel`: khu vuc noi dung co border, header va body thong nhat.
- `PageHeader`: tieu de, mo ta va action cua tung trang.
- `Badge` va cac status badge theo project, application, visibility, stage.
- `LoadingState`, `EmptyState`, error banner va skeleton.
- `ConfirmDialog` cho thao tac nhay cam.
- Button variants: primary, outline, ghost va danger.
- Responsive desktop/mobile va mobile drawer navigation.

### 3.2 Nguyen tac navigation theo role

Frontend chi hien thi nhom chuc nang chinh cua primary role, khong cong don menu cua tat ca role.

| Role | Trang mac dinh | Nhom chuc nang chinh |
|---|---|---|
| User | `/dashboard` | Dashboard, profile, notifications |
| VerifiedUser | `/dashboard` | Discovery, CV, applications, interviews, communication |
| Business | `/projects/me/owned` | Project management, applications, team, files, billing |
| Investor | `/investor` | Investment discovery, interests, investor profile |
| Moderator | `/moderator` | Pending projects, moderation reports |
| Admin | `/admin` | Users, roles, projects, reports, operations, configuration |

## 4. Authentication va authorization

### 4.1 Trang authentication

- `/auth/login`
- `/auth/register`
- `/auth/forgot-password`
- `/auth/reset-password`
- `/auth/verify-email`

### 4.2 Xu ly session

- Luu `accessToken` va `refreshToken` trong localStorage qua `src/lib/auth.ts`.
- Axios tu dong gan Bearer token.
- Tu dong refresh access token khi gap `401`.
- Public project APIs co anonymous fallback neu token cu khong con hop le.
- Chi xoa session khi backend xac nhan `401`.
- Loi mang, `429` va `5xx` khong lam mat phien dang nhap.
- Route bao ve hien trang retry neu tam thoi khong kiem tra duoc session.
- Sau login/register, user duoc redirect den home route dung voi role.
- Ho tro `next` query parameter an toan.
- `RoleGuard` chan truy cap UI khong dung quyen.

## 5. Danh sach pages

### 5.1 Public pages

| Route | Noi dung |
|---|---|
| `/` | Landing page StartupConnect |
| `/projects` | Project discovery, keyword search, pagination |
| `/projects/[id]` | Public project detail va NDA prompt |
| `/auth/*` | Authentication flows |
| `/forbidden` | Trang khong du quyen |

Header public nhan biet session: guest thay Sign in/Join, user da login thay ten va link vao workspace.

### 5.2 User va VerifiedUser

| Route | Noi dung |
|---|---|
| `/dashboard` | Dashboard ca nhan |
| `/feed` | Activity feed |
| `/search` | Tim project, member, investor va suggestion |
| `/recommendations` | Project/member recommendations |
| `/profile` | Profile, skills va portfolio |
| `/members/[id]` | Public member profile |
| `/cvs` | Tao, cap nhat, upload va xoa CV |
| `/files` | File manager va download URL |
| `/projects/saved` | Saved projects |
| `/projects/me/joined` | Joined projects |
| `/applications` | My applications |
| `/applications/[id]` | Application detail va status history |
| `/applications/[id]/interviews` | Lich va form dat interview |
| `/interviews` | Danh sach interview cua user |
| `/messages` | Conversations va realtime messages |
| `/nda-agreements` | NDA agreements cua user |
| `/reports` | Reports da gui |
| `/reports/[id]` | Report detail va action history |
| `/notifications` | Notification center |
| `/billing` | Subscription plans va current subscription |

### 5.3 Founder/Business

| Route | Noi dung |
|---|---|
| `/projects/create` | Multi-step project draft form va AI support |
| `/projects/[id]/edit` | Edit project |
| `/projects/me/owned` | Owned projects va lifecycle actions |
| `/applications/received` | Applications theo owned project |
| `/team` | Team, invitations va member roles |

#### Project workspace

Moi project founder co workspace rieng voi horizontal navigation:

| Route | Backend capability |
|---|---|
| `/projects/[id]/dashboard` | Project metrics, conversion, team, investor va NDA counts |
| `/projects/[id]/activity` | Project activity timeline |
| `/projects/[id]/versions` | Project version history |
| `/projects/[id]/members` | Project members va public profile links |
| `/projects/[id]/recommended-members` | Backend-ranked teammate recommendations |
| `/projects/[id]/applications` | Project-specific applicant management |
| `/projects/[id]/investor-interests` | Founder investor-interest decisions |
| `/projects/[id]/investor-summary` | AI-generated investor brief |
| `/projects/[id]/ai-reviews` | AI quality review history va run review |
| `/projects/[id]/nda-agreements` | Project NDA acceptance records |

Project lifecycle actions da co:

- Create draft
- Update
- Submit review
- Close
- Archive
- Save/unsave
- View versions
- AI suggestions
- AI review
- Apply AI recommendation

### 5.4 Application va interview

- Apply project voi CV selection.
- Verified-user required UX.
- My applications va status history.
- Withdraw application.
- Founder shortlist, interview, accept va reject.
- Schedule interview theo application.
- Update, cancel va complete interview.
- Interview meeting types: Online, InPerson, Phone.

### 5.5 Investor

| Route | Noi dung |
|---|---|
| `/investor` | Investor dashboard |
| `/investor/profile` | Create/update investor profile |
| `/investor/projects` | Investor project discovery |
| `/investor/interests` | My investor interests va withdraw |

Investor functionality:

- Project discovery.
- AI investor summary.
- Express interest.
- View interest states.
- Withdraw interest.
- Founder accept/reject/request-more-info.

### 5.6 Moderator

| Route | Noi dung |
|---|---|
| `/moderator` | Moderator dashboard |
| `/moderator/projects/pending` | Pending moderation queue |
| `/moderator/projects/[id]` | Project moderation detail |
| `/moderator/reports` | Report queue |
| `/moderator/reports/[id]` | Report detail va action history |

Moderation actions:

- Approve
- Request improvement
- Reject
- Hide
- Restore
- Assign report
- Investigate
- Resolve
- Dismiss

### 5.7 Admin

| Route | Noi dung |
|---|---|
| `/admin` | Admin dashboard |
| `/admin/users` | User management |
| `/admin/users/[id]` | User detail, roles va account timeline |
| `/admin/roles` | Role management |
| `/admin/projects` | Project administration |
| `/admin/reports` | Admin report overview |
| `/admin/audit-logs` | Audit log viewer |
| `/admin/settings` | System settings |
| `/admin/subscriptions` | Plans va quotas |
| `/admin/background-jobs` | Background job monitor/run |
| `/admin/email-outbox` | Email delivery queue va retry |
| `/admin/nda-templates` | NDA template va version management |

Admin actions:

- Suspend/unsuspend user.
- Ban/unban user.
- Add/remove roles.
- Hide/restore/archive/close project.
- Force project status.
- Create/update subscription plans va quotas.
- Update system settings.
- Retry email outbox.
- Run background jobs.

## 6. Backend services da tich hop

Frontend khong hardcode API URL trong components. Tat ca request di qua Axios client va cac service:

- `backendService`: auth va API information.
- `projectService`: discovery, detail, lifecycle, dashboard, versions, activity va AI.
- `profileService`: profile, skills, CV va portfolio.
- `applicationService`: application lifecycle.
- `interviewService`: schedule va interview lifecycle.
- `investorService`: investor profile, discovery va interests.
- `projectTeamService`: members, invitations va ownership.
- `ndaService`: templates va agreements.
- `searchService`: projects, members, investors va suggestions.
- `recommendationService`: project/member recommendations.
- `dashboardService`: current-user dashboard.
- `activityService`: feed.
- `chatService`: conversations va messages.
- `notificationService`: list, unread count, read va delete.
- `reportService`: user/moderator reports.
- `subscriptionService`: plans, checkout, cancel va resume.
- `fileService`: file list, download URL va delete.
- `moderatorService`: moderation workflows.
- `adminService`: users, roles, projects, settings, plans va operations.
- `backgroundJobService`: job monitoring va manual run.

## 7. Data fetching va state

- TanStack Query quan ly server state, cache, invalidate va mutation state.
- Zustand quan ly current user va authentication state.
- React Hook Form + Zod cho form validation.
- Axios interceptor xu ly token va API errors.
- SignalR provider cho realtime notifications/messages.
- Query cache duoc clear khi doi account de tranh hien du lieu cua user cu.

## 8. UX states

Tat ca luong chinh co cac trang thai:

- Loading
- Empty
- Error
- Retry
- Disabled action theo status
- Pending mutation/loading button
- Success/error toast
- Confirmation cho destructive actions
- Unauthorized redirect
- Forbidden state
- Responsive mobile/desktop

Project discovery co keyword search, badge theo status/stage/visibility va pagination 12 items/page.

## 9. Bao mat frontend

- Khong dua token vao component props.
- Khong hardcode backend URL trong page.
- Khong redirect ve URL ngoai domain qua `next`.
- Khong xoa token voi loi tam thoi `429/5xx`.
- Public endpoint co anonymous fallback an toan.
- Role navigation va RoleGuard giam kha nang truy cap nham chuc nang.
- Backend van la nguon xac thuc authorization cuoi cung.

## 10. Kiem thu

Playwright specs:

- `api-health.spec.ts`
- `authentication.spec.ts`
- `compliance-flows.spec.ts`
- `production-flows.spec.ts`
- `public-ui.spec.ts`
- `role-workflows.spec.ts`

Da kiem tra:

- Login va invalid credentials.
- Account switching khong stale session.
- Role-specific redirect va navigation.
- Public project page khong bat login lai.
- Token cu co anonymous fallback.
- Loi `429` khong sign out user.
- Desktop va mobile workflows.
- Backend project workspace endpoints tra HTTP `200`.
- ESLint pass.
- TypeScript type-check pass.
- Next.js production build pass.

## 11. Chay frontend

Tai thu muc `frontend`:

```powershell
$env:PATH = "$PWD\.node;$env:PATH"
.\.node\npm.cmd run dev
```

Production build:

```powershell
$env:PATH = "$PWD\.node;$env:PATH"
.\.node\npm.cmd run lint
.\.node\npm.cmd run type-check
.\.node\npm.cmd run build
.\.node\npm.cmd run start
```

E2E tests:

```powershell
$env:PATH = "$PWD\.node;$env:PATH"
.\.node\npx.cmd playwright test
```

## 12. Environment

`frontend/.env.local`:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:8080/api/v1
```

Components khong doc URL API truc tiep; gia tri nay duoc tap trung trong `src/lib/config.ts` va `src/lib/api.ts`.

## 13. Trang thai hien tai

- Frontend da co day du nen tang, authentication, role guards va app shell.
- Cac workflow chinh cua member, founder, investor, moderator va admin da co UI.
- Project workspace da bao phu dashboard, activity, versions, members, recommendations, applications, investor, AI va NDA.
- Cac API backend chinh da duoc ket noi qua typed services.
- Production build thanh cong va co the chay tai `http://localhost:3000`.

## 14. Huong phat trien tiep

- Bo sung visual regression screenshots vao CI.
- Them unit/component tests cho form va permission helpers.
- Chuan hoa toan bo noi dung giao dien ve mot ngon ngu duy nhat.
- Them cursor pagination cho messages/activity neu du lieu lon.
- Hoan thien accessibility audit va keyboard navigation.
- Them observability frontend, error reporting va performance metrics cho production.
