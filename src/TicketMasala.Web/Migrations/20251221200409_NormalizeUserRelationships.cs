using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FKs to shadow columns
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId1",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityReviews_AspNetUsers_ReviewerId1",
                table: "QualityReviews");

            // Drop indexes on shadow columns
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId1",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseArticles_AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropIndex(
                name: "IX_QualityReviews_ReviewerId1",
                table: "QualityReviews");

            // Drop the shadow columns
            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropColumn(
                name: "ReviewerId1",
                table: "QualityReviews");

            // Drop unused Resource table if it exists
            migrationBuilder.DropTable(
                name: "Resource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate Resource table
            migrationBuilder.CreateTable(
                name: "Resource",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatorGuid = table.Column<Guid>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectGuid = table.Column<Guid>(type: "TEXT", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resource", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_Resource_Projects_ProjectGuid",
                        column: x => x.ProjectGuid,
                        principalTable: "Projects",
                        principalColumn: "Guid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resource_ProjectGuid",
                table: "Resource",
                column: "ProjectGuid");

            // Recreate shadow columns
            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "AuditLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorId1",
                table: "KnowledgeBaseArticles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewerId1",
                table: "QualityReviews",
                type: "TEXT",
                nullable: true);

            // Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId1",
                table: "AuditLogs",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseArticles_AuthorId1",
                table: "KnowledgeBaseArticles",
                column: "AuthorId1");

            migrationBuilder.CreateIndex(
                name: "IX_QualityReviews_ReviewerId1",
                table: "QualityReviews",
                column: "ReviewerId1");

            // Recreate FKs
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
