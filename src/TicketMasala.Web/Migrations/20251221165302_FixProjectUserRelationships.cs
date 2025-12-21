using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixProjectUserRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Projects_ProjectGuid",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tickets_TicketGuid",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Tickets_TicketId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_UploaderId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Tickets_TicketId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_CustomerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityReviews_Tickets_TicketId",
                table: "QualityReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedFilters_AspNetUsers_UserId",
                table: "SavedFilters");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_AspNetUsers_AuthorId",
                table: "TicketComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_CustomerId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_ResponsibleId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_KnowledgeBaseArticles_SolvedByArticleId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Projects_ProjectGuid",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeLogs_Tickets_TicketId",
                table: "TimeLogs");

            migrationBuilder.DropTable(
                name: "Resource");

            migrationBuilder.DropIndex(
                name: "IX_TimeLogs_TicketId",
                table: "TimeLogs");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SolvedByArticleId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_QualityReviews_TicketId",
                table: "QualityReviews");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TicketId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TicketId",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "TicketGuid",
                table: "AspNetUsers",
                newName: "StakeholderProjectId");

            migrationBuilder.RenameColumn(
                name: "ProjectGuid",
                table: "AspNetUsers",
                newName: "ResourceProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_TicketGuid",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_StakeholderProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_ProjectGuid",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_ResourceProjectId");

            migrationBuilder.AddColumn<string>(
                name: "WatcherIds",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ReviewerId1",
                table: "QualityReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerIds",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

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
                name: "FK_AspNetUsers_Projects_ResourceProjectId",
                table: "AspNetUsers",
                column: "ResourceProjectId",
                principalTable: "Projects",
                principalColumn: "Guid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Projects_StakeholderProjectId",
                table: "AspNetUsers",
                column: "StakeholderProjectId",
                principalTable: "Projects",
                principalColumn: "Guid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId1",
                table: "AuditLogs",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AspNetUsers_UploaderId",
                table: "Documents",
                column: "UploaderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId",
                table: "KnowledgeBaseArticles",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId1",
                table: "KnowledgeBaseArticles",
                column: "AuthorId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetUsers_CustomerId",
                table: "Projects",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetUsers_ProjectManagerId",
                table: "Projects",
                column: "ProjectManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityReviews_AspNetUsers_ReviewerId1",
                table: "QualityReviews",
                column: "ReviewerId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedFilters_AspNetUsers_UserId",
                table: "SavedFilters",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_AspNetUsers_AuthorId",
                table: "TicketComments",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_CustomerId",
                table: "Tickets",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_ResponsibleId",
                table: "Tickets",
                column: "ResponsibleId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Projects_ProjectGuid",
                table: "Tickets",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Projects_ResourceProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Projects_StakeholderProjectId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId1",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_UploaderId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_CustomerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityReviews_AspNetUsers_ReviewerId1",
                table: "QualityReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedFilters_AspNetUsers_UserId",
                table: "SavedFilters");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_AspNetUsers_AuthorId",
                table: "TicketComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_CustomerId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_ResponsibleId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Projects_ProjectGuid",
                table: "Tickets");

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
                name: "WatcherIds",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ReviewerId1",
                table: "QualityReviews");

            migrationBuilder.DropColumn(
                name: "CustomerIds",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AuthorId1",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "StakeholderProjectId",
                table: "AspNetUsers",
                newName: "TicketGuid");

            migrationBuilder.RenameColumn(
                name: "ResourceProjectId",
                table: "AspNetUsers",
                newName: "ProjectGuid");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_StakeholderProjectId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_TicketGuid");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_ResourceProjectId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_ProjectGuid");

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
                name: "IX_TimeLogs_TicketId",
                table: "TimeLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SolvedByArticleId",
                table: "Tickets",
                column: "SolvedByArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityReviews_TicketId",
                table: "QualityReviews",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TicketId",
                table: "Documents",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TicketId",
                table: "AuditLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Resource_ProjectGuid",
                table: "Resource",
                column: "ProjectGuid");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Projects_ProjectGuid",
                table: "AspNetUsers",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tickets_TicketGuid",
                table: "AspNetUsers",
                column: "TicketGuid",
                principalTable: "Tickets",
                principalColumn: "Guid");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Tickets_TicketId",
                table: "AuditLogs",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AspNetUsers_UploaderId",
                table: "Documents",
                column: "UploaderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Tickets_TicketId",
                table: "Documents",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId",
                table: "KnowledgeBaseArticles",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetUsers_CustomerId",
                table: "Projects",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetUsers_ProjectManagerId",
                table: "Projects",
                column: "ProjectManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QualityReviews_Tickets_TicketId",
                table: "QualityReviews",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedFilters_AspNetUsers_UserId",
                table: "SavedFilters",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_AspNetUsers_AuthorId",
                table: "TicketComments",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_CustomerId",
                table: "Tickets",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_ResponsibleId",
                table: "Tickets",
                column: "ResponsibleId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_KnowledgeBaseArticles_SolvedByArticleId",
                table: "Tickets",
                column: "SolvedByArticleId",
                principalTable: "KnowledgeBaseArticles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Projects_ProjectGuid",
                table: "Tickets",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeLogs_Tickets_TicketId",
                table: "TimeLogs",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
