using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TicketMasala.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tickets_TicketGuid",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_UploaderId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_Departments_DepartmentId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_CustomerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityReviews_AspNetUsers_ReviewerId",
                table: "QualityReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Projects_ProjectGuid",
                table: "Resources");

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

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "ProjectCustomers");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_Projects_DepartmentId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseArticles_DepartmentId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Resources",
                table: "Resources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.RenameTable(
                name: "Resources",
                newName: "Resource");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "ApplicationUser");

            migrationBuilder.RenameIndex(
                name: "IX_Resources_ProjectGuid",
                table: "Resource",
                newName: "IX_Resource_ProjectGuid");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_TicketGuid",
                table: "ApplicationUser",
                newName: "IX_ApplicationUser_TicketGuid");

            migrationBuilder.AlterColumn<string>(
                name: "CustomFieldsJson",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfigVersionId",
                table: "Tickets",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Tickets",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentProjectName",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DomainCustomFieldsJson",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedProjectName",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Tickets",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewerId",
                table: "QualityReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "QualityReviews",
                type: "TEXT",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "QualityReviews",
                type: "TEXT",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "QualityReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectGuid",
                table: "ApplicationUser",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComputedCategory",
                table: "Tickets",
                type: "TEXT",
                nullable: true,
                computedColumnSql: "json_extract(CustomFieldsJson, '$.category')",
                stored: true);

            migrationBuilder.AddColumn<double>(
                name: "ComputedPriority",
                table: "Tickets",
                type: "REAL",
                nullable: true,
                computedColumnSql: "json_extract(CustomFieldsJson, '$.priority_score')",
                stored: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Resource",
                table: "Resource",
                column: "Guid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationUser",
                table: "ApplicationUser",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DomainConfigVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainConfigVersion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: true, computedColumnSql: "json_extract(Payload, '$.Status')", stored: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ComputedPriority",
                table: "Tickets",
                column: "ComputedPriority");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ContentHash",
                table: "Tickets",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DomainId",
                table: "Tickets",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_ProjectGuid",
                table: "ApplicationUser",
                column: "ProjectGuid");

            migrationBuilder.CreateIndex(
                name: "IX_DomainConfigVersion_Hash",
                table: "DomainConfigVersion",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Status",
                table: "WorkItems",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUser_Projects_ProjectGuid",
                table: "ApplicationUser",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUser_Tickets_TicketGuid",
                table: "ApplicationUser",
                column: "TicketGuid",
                principalTable: "Tickets",
                principalColumn: "Guid");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_ApplicationUser_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_ApplicationUser_UploaderId",
                table: "Documents",
                column: "UploaderId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_ApplicationUser_AuthorId",
                table: "KnowledgeBaseArticles",
                column: "AuthorId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_ApplicationUser_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ApplicationUser_CustomerId",
                table: "Projects",
                column: "CustomerId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ApplicationUser_ProjectManagerId",
                table: "Projects",
                column: "ProjectManagerId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QualityReviews_ApplicationUser_ReviewerId",
                table: "QualityReviews",
                column: "ReviewerId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_Projects_ProjectGuid",
                table: "Resource",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedFilters_ApplicationUser_UserId",
                table: "SavedFilters",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_ApplicationUser_AuthorId",
                table: "TicketComments",
                column: "AuthorId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_ApplicationUser_CustomerId",
                table: "Tickets",
                column: "CustomerId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_ApplicationUser_ResponsibleId",
                table: "Tickets",
                column: "ResponsibleId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Projects_ProjectGuid",
                table: "Tickets",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUser_Projects_ProjectGuid",
                table: "ApplicationUser");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUser_Tickets_TicketGuid",
                table: "ApplicationUser");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_ApplicationUser_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_ApplicationUser_UploaderId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_ApplicationUser_AuthorId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_ApplicationUser_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ApplicationUser_CustomerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ApplicationUser_ProjectManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_QualityReviews_ApplicationUser_ReviewerId",
                table: "QualityReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Resource_Projects_ProjectGuid",
                table: "Resource");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedFilters_ApplicationUser_UserId",
                table: "SavedFilters");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_ApplicationUser_AuthorId",
                table: "TicketComments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_ApplicationUser_CustomerId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_ApplicationUser_ResponsibleId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Projects_ProjectGuid",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "DomainConfigVersion");

            migrationBuilder.DropTable(
                name: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ComputedPriority",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ContentHash",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_DomainId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Resource",
                table: "Resource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationUser",
                table: "ApplicationUser");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUser_ProjectGuid",
                table: "ApplicationUser");

            migrationBuilder.DropColumn(
                name: "ComputedCategory",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ComputedPriority",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ConfigVersionId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CurrentProjectName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DomainCustomFieldsJson",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RecommendedProjectName",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "QualityReviews");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "QualityReviews");

            migrationBuilder.DropColumn(
                name: "ProjectGuid",
                table: "ApplicationUser");

            migrationBuilder.RenameTable(
                name: "Resource",
                newName: "Resources");

            migrationBuilder.RenameTable(
                name: "ApplicationUser",
                newName: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "IX_Resource_ProjectGuid",
                table: "Resources",
                newName: "IX_Resources_ProjectGuid");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationUser_TicketGuid",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_TicketGuid");

            migrationBuilder.AlterColumn<string>(
                name: "CustomFieldsJson",
                table: "Tickets",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewerId",
                table: "QualityReviews",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "QualityReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5000,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "KnowledgeBaseArticles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Resources",
                table: "Resources",
                column: "Guid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCustomers",
                columns: table => new
                {
                    CustomersId = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectsGuid = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCustomers", x => new { x.CustomersId, x.ProjectsGuid });
                    table.ForeignKey(
                        name: "FK_ProjectCustomers_AspNetUsers_CustomersId",
                        column: x => x.CustomersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectCustomers_Projects_ProjectsGuid",
                        column: x => x.ProjectsGuid,
                        principalTable: "Projects",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1", "c1084c6e-fb3d-4a72-848e-99e17d326284", "Admin", "ADMIN" },
                    { "2", "ebd56383-648c-405f-99ca-49a00cf6032f", "Employee", "EMPLOYEE" },
                    { "3", "06ac4722-84c3-4b57-a694-5a460264a74d", "Customer", "CUSTOMER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DepartmentId",
                table: "Projects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseArticles_DepartmentId",
                table: "KnowledgeBaseArticles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCustomers_ProjectsGuid",
                table: "ProjectCustomers",
                column: "ProjectsGuid");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

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
                name: "FK_Documents_AspNetUsers_UploaderId",
                table: "Documents",
                column: "UploaderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_AspNetUsers_AuthorId",
                table: "KnowledgeBaseArticles",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_Departments_DepartmentId",
                table: "KnowledgeBaseArticles",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QualityReviews_AspNetUsers_ReviewerId",
                table: "QualityReviews",
                column: "ReviewerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Projects_ProjectGuid",
                table: "Resources",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid");

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
                name: "FK_Tickets_Projects_ProjectGuid",
                table: "Tickets",
                column: "ProjectGuid",
                principalTable: "Projects",
                principalColumn: "Guid",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
