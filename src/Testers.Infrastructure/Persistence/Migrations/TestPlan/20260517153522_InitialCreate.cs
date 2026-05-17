using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Testers.Infrastructure.Persistence.Migrations.TestPlan
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    task_definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    build_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tester_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    actioned_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    modified_by = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_run", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "task_run_bug_link",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    external_bug_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    bug_tracker_url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    linked_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    task_run_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_run_bug_link", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_run_bug_link_task_run_task_run_id",
                        column: x => x.task_run_id,
                        principalTable: "task_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_run_comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    body = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    author_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    added_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    task_run_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_run_comment", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_run_comment_task_run_task_run_id",
                        column: x => x.task_run_id,
                        principalTable: "task_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_run_build_id_task_definition_id",
                table: "task_run",
                columns: new[] { "build_id", "task_definition_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_run_tester_id_actioned_at",
                table: "task_run",
                columns: new[] { "tester_id", "actioned_at" });

            migrationBuilder.CreateIndex(
                name: "ix_task_run_bug_link_task_run_id",
                table: "task_run_bug_link",
                column: "task_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_run_comment_task_run_id",
                table: "task_run_comment",
                column: "task_run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_run_bug_link");

            migrationBuilder.DropTable(
                name: "task_run_comment");

            migrationBuilder.DropTable(
                name: "task_run");
        }
    }
}
