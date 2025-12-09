using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Project2526.Migrations
{
    /// <inheritdoc />
    public partial class AiSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectAiRoadmap",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "97a31515-3f8c-4695-a548-249909a195c3");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "ff8e47ca-e045-472a-a030-cec957bf256a");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "8557754b-f40a-41de-a78a-0646affdbbf8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ProjectAiRoadmap",
                table: "Projects");

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
}
