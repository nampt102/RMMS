using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rmms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M05_Attendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_schedule_shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    check_in_latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: false),
                    check_in_longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: false),
                    check_in_distance_meters = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    check_in_face_result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    check_in_face_confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    check_in_selfie_url = table.Column<string>(type: "text", nullable: true),
                    check_in_store_photo_url = table.Column<string>(type: "text", nullable: true),
                    check_in_fake_gps_detected = table.Column<bool>(type: "boolean", nullable: false),
                    is_late = table.Column<bool>(type: "boolean", nullable: false),
                    check_in_note = table.Column<string>(type: "text", nullable: true),
                    check_out_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    check_out_latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    check_out_longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    check_out_distance_meters = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    check_out_face_result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    check_out_face_confidence = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    check_out_selfie_url = table.Column<string>(type: "text", nullable: true),
                    check_out_store_photo_url = table.Column<string>(type: "text", nullable: true),
                    check_out_note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_shift",
                table: "attendance_records",
                column: "work_schedule_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_status_created",
                table: "attendance_records",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_store",
                table: "attendance_records",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_user_check_in",
                table: "attendance_records",
                columns: new[] { "user_id", "check_in_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records");
        }
    }
}
