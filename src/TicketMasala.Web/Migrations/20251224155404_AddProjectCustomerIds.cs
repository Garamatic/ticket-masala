using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCustomerIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DomainConfigVersion",
                table: "DomainConfigVersion");

            migrationBuilder.RenameTable(
                name: "DomainConfigVersion",
                newName: "DomainConfigVersions");

            migrationBuilder.RenameIndex(
                name: "IX_DomainConfigVersion_Hash",
                table: "DomainConfigVersions",
                newName: "IX_DomainConfigVersions_Hash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomainConfigVersions",
                table: "DomainConfigVersions",
                column: "Id");

            migrationBuilder.AddColumn<string>(
                name: "CustomerIds",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DomainConfigVersions",
                table: "DomainConfigVersions");

            migrationBuilder.RenameTable(
                name: "DomainConfigVersions",
                newName: "DomainConfigVersion");

            migrationBuilder.RenameIndex(
                name: "IX_DomainConfigVersions_Hash",
                table: "DomainConfigVersion",
                newName: "IX_DomainConfigVersion_Hash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomainConfigVersion",
                table: "DomainConfigVersion",
                column: "Id");

            migrationBuilder.DropColumn(
                name: "CustomerIds",
                table: "Projects");
        }
    }
}
