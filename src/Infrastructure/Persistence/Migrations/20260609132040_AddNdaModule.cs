using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNdaModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nda_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nda_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nda_template_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nda_template_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nda_template_versions_nda_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "nda_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nda_agreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    AgreementSnapshot = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nda_agreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nda_agreements_nda_template_versions_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "nda_template_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_nda_agreements_nda_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "nda_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_nda_agreements_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nda_agreements_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nda_agreements_ProjectId_UserId_TemplateVersionId",
                table: "nda_agreements",
                columns: new[] { "ProjectId", "UserId", "TemplateVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nda_agreements_TemplateId",
                table: "nda_agreements",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_nda_agreements_TemplateVersionId",
                table: "nda_agreements",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_nda_agreements_UserId",
                table: "nda_agreements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_nda_template_versions_TemplateId_VersionNumber",
                table: "nda_template_versions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nda_agreements");

            migrationBuilder.DropTable(
                name: "nda_template_versions");

            migrationBuilder.DropTable(
                name: "nda_templates");
        }
    }
}
