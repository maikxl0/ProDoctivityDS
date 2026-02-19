using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProDoctivityDS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigratiom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedFilePath = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalFilePath = table.Column<string>(type: "TEXT", nullable: true),
                    PagesRemoved = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiUpdated = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiBaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    ApiSecret = table.Column<string>(type: "TEXT", nullable: false),
                    BearerToken = table.Column<string>(type: "TEXT", nullable: false),
                    CookieSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessingOptionsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    AnalysisRulesJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_DocumentId",
                table: "ActivityLogs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Timestamp",
                table: "ActivityLogs",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_StoredConfigurations_LastModified",
                table: "StoredConfigurations",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_StoredConfigurations_Singleton",
                table: "StoredConfigurations",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "ProcessedDocuments");

            migrationBuilder.DropTable(
                name: "StoredConfigurations");
        }
    }
}
