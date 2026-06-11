using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAIModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestType = table.Column<int>(type: "integer", nullable: false),
                    PromptSnapshot = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_requests_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AIRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Content = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    TargetField = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsApplied = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_recommendations_ai_requests_AIRequestId",
                        column: x => x.AIRequestId,
                        principalTable: "ai_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_recommendations_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_recommendations_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AIRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityScore = table.Column<int>(type: "integer", nullable: false),
                    MissingInformationJson = table.Column<string>(type: "jsonb", nullable: false),
                    RiskFlagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SuggestionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_reviews_ai_requests_AIRequestId",
                        column: x => x.AIRequestId,
                        principalTable: "ai_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_reviews_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_reviews_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_AIRequestId",
                table: "ai_recommendations",
                column: "AIRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_ProjectId",
                table: "ai_recommendations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_recommendations_RequestedByUserId",
                table: "ai_recommendations",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_requests_ProjectId",
                table: "ai_requests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_requests_UserId_CreatedAt",
                table: "ai_requests",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_reviews_AIRequestId",
                table: "ai_reviews",
                column: "AIRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_reviews_ProjectId_CreatedAt",
                table: "ai_reviews",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_reviews_RequestedByUserId",
                table: "ai_reviews",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_recommendations");

            migrationBuilder.DropTable(
                name: "ai_reviews");

            migrationBuilder.DropTable(
                name: "ai_requests");
        }
    }
}
