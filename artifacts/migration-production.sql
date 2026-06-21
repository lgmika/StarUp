START TRANSACTION;
ALTER TABLE ai_requests ADD "ResponseSnapshot" text;

CREATE TABLE email_outbox_messages (
    "Id" uuid NOT NULL,
    "UserId" uuid,
    "Recipient" character varying(255) NOT NULL,
    "Template" character varying(80) NOT NULL,
    "ProtectedPayload" character varying(12000) NOT NULL,
    "Attempts" integer NOT NULL,
    "NextAttemptAt" timestamp with time zone NOT NULL,
    "SentAt" timestamp with time zone,
    "LastError" character varying(1000),
    "LeaseId" uuid,
    "LockedUntil" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_email_outbox_messages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_email_outbox_messages_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE SET NULL
);

CREATE TABLE project_views (
    "Id" uuid NOT NULL,
    "ProjectId" uuid NOT NULL,
    "ViewerUserId" uuid,
    "VisitorId" uuid,
    "ViewedOn" date NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_project_views" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_project_views_viewer" CHECK (("ViewerUserId" IS NOT NULL AND "VisitorId" IS NULL) OR ("ViewerUserId" IS NULL AND "VisitorId" IS NOT NULL)),
    CONSTRAINT "FK_project_views_projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES projects ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_project_views_users_ViewerUserId" FOREIGN KEY ("ViewerUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_email_outbox_messages_SentAt_NextAttemptAt_LockedUntil" ON email_outbox_messages ("SentAt", "NextAttemptAt", "LockedUntil");

CREATE INDEX "IX_email_outbox_messages_UserId" ON email_outbox_messages ("UserId");

CREATE INDEX "IX_project_views_ProjectId_CreatedAt" ON project_views ("ProjectId", "CreatedAt");

CREATE UNIQUE INDEX "IX_project_views_ProjectId_ViewerUserId_ViewedOn" ON project_views ("ProjectId", "ViewerUserId", "ViewedOn");

CREATE UNIQUE INDEX "IX_project_views_ProjectId_VisitorId_ViewedOn" ON project_views ("ProjectId", "VisitorId", "ViewedOn");

CREATE INDEX "IX_project_views_ViewerUserId" ON project_views ("ViewerUserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260619074558_AddAIResponsesProjectViewsAndEmailOutbox', '10.0.4');

COMMIT;

START TRANSACTION;
DROP INDEX "IX_messages_SenderUserId";

DROP INDEX "IX_conversation_participants_UserId";

CREATE INDEX "IX_messages_SenderUserId_CreatedAt" ON messages ("SenderUserId", "CreatedAt");

CREATE INDEX "IX_conversation_participants_UserId_ConversationId" ON conversation_participants ("UserId", "ConversationId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260620181627_AddChatQueryIndexes', '10.0.4');

COMMIT;
