using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixTicketSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.Sql("ALTER TABLE Tickets ADD COLUMN ConfigVersionId TEXT;");
            // migrationBuilder.Sql("ALTER TABLE Tickets ADD COLUMN Status TEXT DEFAULT 'New' NOT NULL;");
            // migrationBuilder.Sql("ALTER TABLE Tickets ADD COLUMN Title TEXT DEFAULT 'Untitled' NOT NULL;");
            // migrationBuilder.Sql("ALTER TABLE Tickets ADD COLUMN ContentHash TEXT;");
            // migrationBuilder.Sql("ALTER TABLE Tickets ADD COLUMN RecommendedProjectName TEXT;");
            // migrationBuilder.Sql("ALTER TABLE Tickets ADD COLUMN CurrentProjectName TEXT;");
            // migrationBuilder.Sql("ALTER TABLE Tickets ADD COLUMN DomainCustomFieldsJson TEXT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
