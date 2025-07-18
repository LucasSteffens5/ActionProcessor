using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActionProcessor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BatchUpload_Optimistic_Locking_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "batch_uploads",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "row_version",
                table: "batch_uploads");
        }
    }
}
