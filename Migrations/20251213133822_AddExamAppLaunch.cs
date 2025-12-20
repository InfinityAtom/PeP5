using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeP.Migrations
{
    public partial class AddExamAppLaunch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamAppAuthorizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExamCodeId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuthorizedByTeacherId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAppAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAppAuthorizations_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAppAuthorizations_ExamCodes_ExamCodeId",
                        column: x => x.ExamCodeId,
                        principalTable: "ExamCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAppLaunchSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamAttemptId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAppLaunchSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAppLaunchSessions_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAppLaunchSessions_ExamAttempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "ExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4505));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4508));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4509));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4510));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4510));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4511));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4511));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4512));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4513));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2025, 12, 13, 13, 38, 21, 890, DateTimeKind.Utc).AddTicks(4513));

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppAuthorizations_ExamCodeId",
                table: "ExamAppAuthorizations",
                column: "ExamCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppAuthorizations_StudentId_ExpiresAt",
                table: "ExamAppAuthorizations",
                columns: new[] { "StudentId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppAuthorizations_TokenHash",
                table: "ExamAppAuthorizations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppLaunchSessions_ExamAttemptId_ExpiresAt",
                table: "ExamAppLaunchSessions",
                columns: new[] { "ExamAttemptId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppLaunchSessions_StudentId",
                table: "ExamAppLaunchSessions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppLaunchSessions_TokenHash",
                table: "ExamAppLaunchSessions",
                column: "TokenHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamAppAuthorizations");

            migrationBuilder.DropTable(
                name: "ExamAppLaunchSessions");

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2842));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2846));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2846));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2847));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2848));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2848));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2849));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2849));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2850));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2025, 11, 30, 10, 0, 8, 174, DateTimeKind.Utc).AddTicks(2850));
        }
    }
}
