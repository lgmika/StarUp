using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestorFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "investor_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InvestmentFocus = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MinTicketSize = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxTicketSize = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investor_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_investor_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "investor_project_interests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvestorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FounderResponse = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investor_project_interests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_investor_project_interests_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_investor_project_interests_users_InvestorUserId",
                        column: x => x.InvestorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_investor_profiles_UserId",
                table: "investor_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_investor_project_interests_InvestorUserId_Status",
                table: "investor_project_interests",
                columns: new[] { "InvestorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_investor_project_interests_ProjectId_InvestorUserId",
                table: "investor_project_interests",
                columns: new[] { "ProjectId", "InvestorUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investor_profiles");

            migrationBuilder.DropTable(
                name: "investor_project_interests");
        }
    }
}
