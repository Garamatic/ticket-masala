using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBaseSnippet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeBaseSnippets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseSnippets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseSnippets_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseSnippets_AuthorId",
                table: "KnowledgeBaseSnippets",
                column: "AuthorId");

            // FTS5 Virtual Table and Triggers
            migrationBuilder.Sql(@"
                CREATE VIRTUAL TABLE IF NOT EXISTS KnowledgeBaseSnippets_Search USING fts5(
                    Content,
                    Tags,
                    content='KnowledgeBaseSnippets',
                    content_rowid='rowid'
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS KnowledgeBaseSnippets_AI AFTER INSERT ON KnowledgeBaseSnippets BEGIN
                    INSERT INTO KnowledgeBaseSnippets_Search(rowid, Content, Tags) 
                    VALUES (new.rowid, new.Content, new.Tags);
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS KnowledgeBaseSnippets_AD AFTER DELETE ON KnowledgeBaseSnippets BEGIN
                    INSERT INTO KnowledgeBaseSnippets_Search(KnowledgeBaseSnippets_Search, rowid, Content, Tags) 
                    VALUES('delete', old.rowid, old.Content, old.Tags);
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS KnowledgeBaseSnippets_AU AFTER UPDATE ON KnowledgeBaseSnippets BEGIN
                    INSERT INTO KnowledgeBaseSnippets_Search(KnowledgeBaseSnippets_Search, rowid, Content, Tags) 
                    VALUES('delete', old.rowid, old.Content, old.Tags);
                    INSERT INTO KnowledgeBaseSnippets_Search(rowid, Content, Tags) 
                    VALUES (new.rowid, new.Content, new.Tags);
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS KnowledgeBaseSnippets_AI;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS KnowledgeBaseSnippets_AD;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS KnowledgeBaseSnippets_AU;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS KnowledgeBaseSnippets_Search;");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseSnippets");
        }
    }
}
