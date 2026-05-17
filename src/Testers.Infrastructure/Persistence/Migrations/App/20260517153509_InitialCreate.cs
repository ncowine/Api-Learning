using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Testers.Infrastructure.Persistence.Migrations.App
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_key",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    key_hash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    prefix = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    owner_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    scopes = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_key", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    entity_type = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    entity_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    state_json = table.Column<string>(type: "TEXT", nullable: true),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    user_display_name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    correlation_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_message",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    consumer_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    processed_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_message", x => new { x.message_id, x.consumer_name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    routing_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "TEXT", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    processed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    attempt_count = table.Column<int>(type: "INTEGER", nullable: false),
                    last_error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    correlation_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_key_key_hash",
                table: "api_key",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_type_entity_key",
                table: "audit_log",
                columns: new[] { "entity_type", "entity_key" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_occurred_at",
                table: "audit_log",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_at_occurred_at",
                table: "outbox_message",
                columns: new[] { "processed_at", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_key");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "inbox_message");

            migrationBuilder.DropTable(
                name: "outbox_message");
        }
    }
}
