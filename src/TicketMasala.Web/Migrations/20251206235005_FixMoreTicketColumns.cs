using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixMoreTicketColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sanitize existing TicketStatus and TicketType to prevent read errors
            // Columns already exist (confirmed by duplicate column error), just need to ensure no NULLs
            migrationBuilder.Sql("UPDATE Tickets SET TicketStatus = 0 WHERE TicketStatus IS NULL;");
            migrationBuilder.Sql("UPDATE Tickets SET TicketType = 0 WHERE TicketType IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
