# Backend gaps found during frontend integration

The frontend only calls contracts currently exposed by the ASP.NET Core API and Swagger.

## Blocking missing contracts

- **General file upload:** only `POST /cvs/upload` exists. There is no authenticated upload endpoint for pitch decks, portfolio attachments, project attachments, or chat attachments. `GET /files/me`, download URL, and delete cannot create those file types.
- **Project attachments:** project detail has no API that lists or associates files with a project, so a real attachments section cannot be implemented.
- **NDA lifecycle:** the API supports template/version creation, current NDA lookup, acceptance, and agreement history. There is no send/request or reject NDA endpoint, so those actions cannot be exposed honestly.
- **NDA access grant:** `POST /projects/{projectId}/nda/accept` records the agreement but does not create a project access grant. `GET /projects/{projectId}` therefore remains `403` for the signer because project authorization only checks owner, active membership, or `ProjectAccessGrant`. The protected-detail UI can accept and display the agreement, but cannot reveal project content afterward.
- **AI usage quota:** AI generation/history endpoints exist, but there is no user-facing endpoint returning daily allowance, consumed count, and reset time. The UI can handle `429`, but cannot show an accurate quota meter before a request.

## Contract limitations

- Upload progress is only meaningful for CV upload because it is the only upload contract. File metadata does not expose a backend-declared category that distinguishes CV, pitch deck, portfolio attachment, and project attachment.
- Role dashboards are uneven: Admin, Moderator, and Investor have dedicated aggregate endpoints; Business and Verified User dashboards must compose multiple list endpoints instead of using a single statistics contract.
- Billing checkout can return a non-production/mock provider state. The frontend surfaces this as “not configured” and does not simulate payment success.

## Recommended backend prompt

Implement the missing StartupConnect API contracts without changing existing response shapes. Keep `ApiResponse<T>`, current authorization policies, pagination conventions, validation/error format, audit logging, and SignalR event conventions. Add: (1) authenticated multipart file upload supporting explicit file categories `Cv`, `PitchDeck`, `PortfolioAttachment`, `ProjectAttachment`, and `ChatAttachment`, with server-side MIME/size validation and ownership; (2) project attachment list/attach/detach endpoints with project access checks; (3) NDA send/request and reject endpoints with reason, agreement status history, authorization, notifications, and realtime events; (4) an authenticated AI usage endpoint returning daily limit, used, remaining, and reset timestamp; and (5) dedicated Business/Founder and Verified User dashboard aggregate endpoints. Update Swagger DTOs/enums and add integration tests. Do not break or rename existing endpoints.
