using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TicketMasala.Web.Data;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    [DbContext(typeof(MasalaDbContext))]
    [Migration("20251207080500_AddFts5Search")]
    public partial class AddFts5Search : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the Virtual Table
            migrationBuilder.Sql(@"
                CREATE VIRTUAL TABLE IF NOT EXISTS Tickets_Search USING fts5(
                    Guid UNINDEXED,
                    Description,
                    CustomFieldsJson,
                    content='Tickets',
                    content_rowid='RowId'
                );
            ");

            // 2. Create Triggers to Sync INSERTs
            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS Tickets_AI AFTER INSERT ON Tickets BEGIN
                    INSERT INTO Tickets_Search(rowid, Guid, Description, CustomFieldsJson) 
                    VALUES (new.RowId, new.Guid, new.Description, new.CustomFieldsJson);
                END;
            ");

            // 3. Create Triggers to Sync DELETEs
            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS Tickets_AD AFTER DELETE ON Tickets BEGIN
                    INSERT INTO Tickets_Search(Tickets_Search, rowid, Guid, Description, CustomFieldsJson) 
                    VALUES('delete', old.RowId, old.Guid, old.Description, old.CustomFieldsJson);
                END;
            ");

            // 4. Create Triggers to Sync UPDATEs
            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS Tickets_AU AFTER UPDATE ON Tickets BEGIN
                    INSERT INTO Tickets_Search(Tickets_Search, rowid, Guid, Description, CustomFieldsJson) 
                    VALUES('delete', old.RowId, old.Guid, old.Description, old.CustomFieldsJson);
                    INSERT INTO Tickets_Search(rowid, Guid, Description, CustomFieldsJson) 
                    VALUES (new.RowId, new.Guid, new.Description, new.CustomFieldsJson);
                END;
            ");

            // 5. Initial Population (Backfill)
            migrationBuilder.Sql(@"
                INSERT INTO Tickets_Search(rowid, Guid, Description, CustomFieldsJson)
                SELECT RowId, Guid, Description, CustomFieldsJson FROM Tickets;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Tickets_AI;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Tickets_AD;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Tickets_AU;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS Tickets_Search;");
        }
    }
}
