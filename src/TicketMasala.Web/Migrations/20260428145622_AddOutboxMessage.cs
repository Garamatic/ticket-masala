using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId1",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityReviews_AspNetUsers_ReviewerId1",
                table: "QualityReviews");

            migrationBuilder.DropIndex(
                name: "IX_QualityReviews_ReviewerId1",
                table: "QualityReviews");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseArticles_AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId1",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ReviewerId1",
                table: "QualityReviews");

            migrationBuilder.DropColumn(
                name: "AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "AuditLogs");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "TimeLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "TemplateTickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "ProjectTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    RoutingKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduledRetryAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CreatedAt",
                table: "OutboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_EventType",
                table: "OutboxMessages",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "ScheduledRetryAt", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "TimeLogs");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "TemplateTickets");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "ProjectTemplates");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "ReviewerId1",
                table: "QualityReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorId1",
                table: "KnowledgeBaseArticles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "AuditLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityReviews_ReviewerId1",
                table: "QualityReviews",
                column: "ReviewerId1");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseArticles_AuthorId1",
                table: "KnowledgeBaseArticles",
                column: "AuthorId1");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId1",
                table: "AuditLogs",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId1",
                table: "AuditLogs",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId1",
                table: "KnowledgeBaseArticles",
                column: "AuthorId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QualityReviews_AspNetUsers_ReviewerId1",
                table: "QualityReviews",
                column: "ReviewerId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
