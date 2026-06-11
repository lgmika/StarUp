using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CvId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoverLetter = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FounderNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_applications_cvs_CvId",
                        column: x => x.CvId,
                        principalTable: "cvs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_applications_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_applications_users_ApplicantUserId",
                        column: x => x.ApplicantUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "application_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_application_attachments_files_FileId",
                        column: x => x.FileId,
                        principalTable: "files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_application_attachments_project_applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "project_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "application_status_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_status_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_application_status_histories_project_applications_Applicati~",
                        column: x => x.ApplicationId,
                        principalTable: "project_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_application_status_histories_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_attachments_ApplicationId",
                table: "application_attachments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_application_attachments_FileId",
                table: "application_attachments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_application_status_histories_ApplicationId_CreatedAt",
                table: "application_status_histories",
                columns: new[] { "ApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_application_status_histories_ChangedByUserId",
                table: "application_status_histories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_project_applications_ApplicantUserId_Status",
                table: "project_applications",
                columns: new[] { "ApplicantUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_project_applications_CvId",
                table: "project_applications",
                column: "CvId");

            migrationBuilder.CreateIndex(
                name: "IX_project_applications_ProjectId_ApplicantUserId",
                table: "project_applications",
                columns: new[] { "ProjectId", "ApplicantUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_attachments");

            migrationBuilder.DropTable(
                name: "application_status_histories");

            migrationBuilder.DropTable(
                name: "project_applications");
        }
    }
}
