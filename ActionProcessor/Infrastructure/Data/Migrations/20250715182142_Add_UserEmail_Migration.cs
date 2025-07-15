using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActionProcessor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_UserEmail_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "batch_uploads",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_batch_uploads_user_email",
                table: "batch_uploads",
                column: "user_email");

            migrationBuilder.CreateIndex(
                name: "ix_batch_uploads_user_email_created_at",
                table: "batch_uploads",
                columns: new[] { "user_email", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_batch_uploads_user_email",
                table: "batch_uploads");

            migrationBuilder.DropIndex(
                name: "ix_batch_uploads_user_email_created_at",
                table: "batch_uploads");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "batch_uploads");
        }
    }
}
