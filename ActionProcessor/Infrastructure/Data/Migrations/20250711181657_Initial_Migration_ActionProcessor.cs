using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActionProcessor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Migration_ActionProcessor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatchUploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    TotalEvents = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchUploads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Document = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResponseData = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingEvents_BatchUploads_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BatchUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchUploads_CreatedAt",
                table: "BatchUploads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BatchUploads_Status",
                table: "BatchUploads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingEvents_ActionType",
                table: "ProcessingEvents",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingEvents_BatchId",
                table: "ProcessingEvents",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingEvents_Document_ClientIdentifier",
                table: "ProcessingEvents",
                columns: new[] { "Document", "ClientIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingEvents_Status_CreatedAt",
                table: "ProcessingEvents",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessingEvents");

            migrationBuilder.DropTable(
                name: "BatchUploads");
        }
    }
}
