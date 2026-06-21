using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartupConnect.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_messages_SenderUserId",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_conversation_participants_UserId",
                table: "conversation_participants");

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderUserId_CreatedAt",
                table: "messages",
                columns: new[] { "SenderUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_participants_UserId_ConversationId",
                table: "conversation_participants",
                columns: new[] { "UserId", "ConversationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_messages_SenderUserId_CreatedAt",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_conversation_participants_UserId_ConversationId",
                table: "conversation_participants");

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderUserId",
                table: "messages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_participants_UserId",
                table: "conversation_participants",
                column: "UserId");
        }
    }
}
