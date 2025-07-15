using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ActionProcessor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Migration_BatchProcessor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingEvents_BatchUploads_BatchId",
                table: "ProcessingEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingEvents",
                table: "ProcessingEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BatchUploads",
                table: "BatchUploads");

            migrationBuilder.RenameTable(
                name: "ProcessingEvents",
                newName: "processing_events");

            migrationBuilder.RenameTable(
                name: "BatchUploads",
                newName: "batch_uploads");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "processing_events",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Document",
                table: "processing_events",
                newName: "document");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "processing_events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "processing_events",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "SideEffectsJson",
                table: "processing_events",
                newName: "side_effects_json");

            migrationBuilder.RenameColumn(
                name: "RetryCount",
                table: "processing_events",
                newName: "retry_count");

            migrationBuilder.RenameColumn(
                name: "ResponseData",
                table: "processing_events",
                newName: "response_data");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "processing_events",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "processing_events",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "processing_events",
                newName: "completed_at");

            migrationBuilder.RenameColumn(
                name: "ClientIdentifier",
                table: "processing_events",
                newName: "client_identifier");

            migrationBuilder.RenameColumn(
                name: "BatchId",
                table: "processing_events",
                newName: "batch_id");

            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "processing_events",
                newName: "action_type");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingEvents_Status_CreatedAt",
                table: "processing_events",
                newName: "ix_processing_events_status_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingEvents_Document_ClientIdentifier",
                table: "processing_events",
                newName: "ix_processing_events_document_client_identifier");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingEvents_BatchId",
                table: "processing_events",
                newName: "ix_processing_events_batch_id");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessingEvents_ActionType",
                table: "processing_events",
                newName: "ix_processing_events_action_type");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "batch_uploads",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "batch_uploads",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TotalEvents",
                table: "batch_uploads",
                newName: "total_events");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "batch_uploads",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "OriginalFileName",
                table: "batch_uploads",
                newName: "original_file_name");

            migrationBuilder.RenameColumn(
                name: "FileSizeBytes",
                table: "batch_uploads",
                newName: "file_size_bytes");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "batch_uploads",
                newName: "file_name");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "batch_uploads",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "batch_uploads",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "batch_uploads",
                newName: "completed_at");

            migrationBuilder.RenameIndex(
                name: "IX_BatchUploads_Status",
                table: "batch_uploads",
                newName: "ix_batch_uploads_status");

            migrationBuilder.RenameIndex(
                name: "IX_BatchUploads_CreatedAt",
                table: "batch_uploads",
                newName: "ix_batch_uploads_created_at");

            migrationBuilder.AddPrimaryKey(
                name: "pk_processing_events",
                table: "processing_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_batch_uploads",
                table: "batch_uploads",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_processing_events_batch_uploads_batch_id",
                table: "processing_events",
                column: "batch_id",
                principalTable: "batch_uploads",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_processing_events_batch_uploads_batch_id",
                table: "processing_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_processing_events",
                table: "processing_events");

            migrationBuilder.DropPrimaryKey(
                name: "pk_batch_uploads",
                table: "batch_uploads");

            migrationBuilder.RenameTable(
                name: "processing_events",
                newName: "ProcessingEvents");

            migrationBuilder.RenameTable(
                name: "batch_uploads",
                newName: "BatchUploads");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "ProcessingEvents",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "document",
                table: "ProcessingEvents",
                newName: "Document");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ProcessingEvents",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "started_at",
                table: "ProcessingEvents",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "side_effects_json",
                table: "ProcessingEvents",
                newName: "SideEffectsJson");

            migrationBuilder.RenameColumn(
                name: "retry_count",
                table: "ProcessingEvents",
                newName: "RetryCount");

            migrationBuilder.RenameColumn(
                name: "response_data",
                table: "ProcessingEvents",
                newName: "ResponseData");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "ProcessingEvents",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ProcessingEvents",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                table: "ProcessingEvents",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "client_identifier",
                table: "ProcessingEvents",
                newName: "ClientIdentifier");

            migrationBuilder.RenameColumn(
                name: "batch_id",
                table: "ProcessingEvents",
                newName: "BatchId");

            migrationBuilder.RenameColumn(
                name: "action_type",
                table: "ProcessingEvents",
                newName: "ActionType");

            migrationBuilder.RenameIndex(
                name: "ix_processing_events_status_created_at",
                table: "ProcessingEvents",
                newName: "IX_ProcessingEvents_Status_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_processing_events_document_client_identifier",
                table: "ProcessingEvents",
                newName: "IX_ProcessingEvents_Document_ClientIdentifier");

            migrationBuilder.RenameIndex(
                name: "ix_processing_events_batch_id",
                table: "ProcessingEvents",
                newName: "IX_ProcessingEvents_BatchId");

            migrationBuilder.RenameIndex(
                name: "ix_processing_events_action_type",
                table: "ProcessingEvents",
                newName: "IX_ProcessingEvents_ActionType");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "BatchUploads",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BatchUploads",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "total_events",
                table: "BatchUploads",
                newName: "TotalEvents");

            migrationBuilder.RenameColumn(
                name: "started_at",
                table: "BatchUploads",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "original_file_name",
                table: "BatchUploads",
                newName: "OriginalFileName");

            migrationBuilder.RenameColumn(
                name: "file_size_bytes",
                table: "BatchUploads",
                newName: "FileSizeBytes");

            migrationBuilder.RenameColumn(
                name: "file_name",
                table: "BatchUploads",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "BatchUploads",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "BatchUploads",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                table: "BatchUploads",
                newName: "CompletedAt");

            migrationBuilder.RenameIndex(
                name: "ix_batch_uploads_status",
                table: "BatchUploads",
                newName: "IX_BatchUploads_Status");

            migrationBuilder.RenameIndex(
                name: "ix_batch_uploads_created_at",
                table: "BatchUploads",
                newName: "IX_BatchUploads_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingEvents",
                table: "ProcessingEvents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BatchUploads",
                table: "BatchUploads",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingEvents_BatchUploads_BatchId",
                table: "ProcessingEvents",
                column: "BatchId",
                principalTable: "BatchUploads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
