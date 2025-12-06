using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketMasala.Web.Migrations;
    /// <inheritdoc />
    public partial class AddDomainExtensibilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomFieldsJson",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DomainId",
                table: "Tickets",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "IT");

            migrationBuilder.AddColumn<string>(
                name: "WorkItemTypeCode",
                table: "Tickets",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "c1084c6e-fb3d-4a72-848e-99e17d326284");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "ebd56383-648c-405f-99ca-49a00cf6032f");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "06ac4722-84c3-4b57-a694-5a460264a74d");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomFieldsJson",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DomainId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "WorkItemTypeCode",
                table: "Tickets");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "3598c83b-2265-4d1b-939a-23b9bf81f6cf");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "03537bd2-c547-4c06-859d-bdb8f3f4e117");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "ddfbf5a3-f6a5-438f-a747-9708972c3d43");
        }
}
