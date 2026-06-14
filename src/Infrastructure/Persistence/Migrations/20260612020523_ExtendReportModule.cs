using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendReportModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedModeratorId",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "reports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "reports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReasonCode",
                table: "reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "reports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_AssignedModeratorId",
                table: "reports",
                column: "AssignedModeratorId");

            migrationBuilder.AddForeignKey(
                name: "FK_reports_users_AssignedModeratorId",
                table: "reports",
                column: "AssignedModeratorId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reports_users_AssignedModeratorId",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "IX_reports_AssignedModeratorId",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "AssignedModeratorId",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "ReasonCode",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "reports");
        }
    }
}
