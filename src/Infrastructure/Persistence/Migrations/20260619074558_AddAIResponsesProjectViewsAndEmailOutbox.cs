using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAIResponsesProjectViewsAndEmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseSnapshot",
                table: "ai_requests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Recipient = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Template = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_outbox_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_outbox_messages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "project_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_views", x => x.Id);
                    table.CheckConstraint("CK_project_views_viewer", "(\"ViewerUserId\" IS NOT NULL AND \"VisitorId\" IS NULL) OR (\"ViewerUserId\" IS NULL AND \"VisitorId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_project_views_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_views_users_ViewerUserId",
                        column: x => x.ViewerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_messages_SentAt_NextAttemptAt_LockedUntil",
                table: "email_outbox_messages",
                columns: new[] { "SentAt", "NextAttemptAt", "LockedUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_email_outbox_messages_UserId",
                table: "email_outbox_messages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_views_ProjectId_CreatedAt",
                table: "project_views",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_project_views_ProjectId_ViewerUserId_ViewedOn",
                table: "project_views",
                columns: new[] { "ProjectId", "ViewerUserId", "ViewedOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_views_ProjectId_VisitorId_ViewedOn",
                table: "project_views",
                columns: new[] { "ProjectId", "VisitorId", "ViewedOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_views_ViewerUserId",
                table: "project_views",
                column: "ViewerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_outbox_messages");

            migrationBuilder.DropTable(
                name: "project_views");

            migrationBuilder.DropColumn(
                name: "ResponseSnapshot",
                table: "ai_requests");
        }
    }
}
