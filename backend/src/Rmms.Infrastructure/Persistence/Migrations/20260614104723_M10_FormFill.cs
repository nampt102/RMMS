using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rmms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M10_FormFill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "form_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answers = table.Column<string>(type: "jsonb", nullable: false),
                    attachments = table.Column<string>(type: "jsonb", nullable: true),
                    score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    time_spent_seconds = table.Column<int>(type: "integer", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    client_idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_submissions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_form_assignments_category",
                table: "form_assignments",
                column: "assigned_to_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_assignments_form_id",
                table: "form_assignments",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_assignments_role",
                table: "form_assignments",
                column: "assigned_to_role");

            migrationBuilder.CreateIndex(
                name: "ix_form_assignments_store",
                table: "form_assignments",
                column: "assigned_to_store_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_assignments_user",
                table: "form_assignments",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_form_user",
                table: "form_submissions",
                columns: new[] { "form_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_user_idem_unique",
                table: "form_submissions",
                columns: new[] { "user_id", "client_idempotency_key" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_assignments");

            migrationBuilder.DropTable(
                name: "form_submissions");
        }
    }
}
