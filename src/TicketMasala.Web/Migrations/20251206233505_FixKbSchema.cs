using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixKbSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE AspNetUsers ADD COLUMN ProjectGuid TEXT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
