using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeP.Migrations
{
    public partial class AddIsProgrammingExamFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProgrammingExam",
                table: "ExamAppLaunchSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProgrammingExam",
                table: "ExamAppAuthorizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2866));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2869));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2870));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2870));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2871));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2872));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2872));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2873));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2873));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 6, 27, 873, DateTimeKind.Utc).AddTicks(2874));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProgrammingExam",
                table: "ExamAppLaunchSessions");

            migrationBuilder.DropColumn(
                name: "IsProgrammingExam",
                table: "ExamAppAuthorizations");

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4645));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4648));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4650));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4650));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4651));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4652));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4652));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4653));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4653));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 12, 25, 33, 993, DateTimeKind.Utc).AddTicks(4654));
        }
    }
}
