using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeP.Migrations
{
    public partial class SeparateExamCodeForeignKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamAppAuthorizations_ExamCodes_ExamCodeId",
                table: "ExamAppAuthorizations");

            migrationBuilder.AlterColumn<int>(
                name: "ExamAttemptId",
                table: "ExamAppLaunchSessions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProgrammingExamAttemptId",
                table: "ExamAppLaunchSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ExamCodeId",
                table: "ExamAppAuthorizations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProgrammingExamCodeId",
                table: "ExamAppAuthorizations",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5001));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5005));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5007));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5008));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5010));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5010));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5011));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5012));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5012));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2025, 12, 23, 13, 14, 45, 920, DateTimeKind.Utc).AddTicks(5013));

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppLaunchSessions_ProgrammingExamAttemptId",
                table: "ExamAppLaunchSessions",
                column: "ProgrammingExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAppAuthorizations_ProgrammingExamCodeId",
                table: "ExamAppAuthorizations",
                column: "ProgrammingExamCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAppAuthorizations_ExamCodes_ExamCodeId",
                table: "ExamAppAuthorizations",
                column: "ExamCodeId",
                principalTable: "ExamCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAppAuthorizations_ProgrammingExamCodes_ProgrammingExamCodeId",
                table: "ExamAppAuthorizations",
                column: "ProgrammingExamCodeId",
                principalTable: "ProgrammingExamCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAppLaunchSessions_ProgrammingExamAttempts_ProgrammingExamAttemptId",
                table: "ExamAppLaunchSessions",
                column: "ProgrammingExamAttemptId",
                principalTable: "ProgrammingExamAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamAppAuthorizations_ExamCodes_ExamCodeId",
                table: "ExamAppAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAppAuthorizations_ProgrammingExamCodes_ProgrammingExamCodeId",
                table: "ExamAppAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAppLaunchSessions_ProgrammingExamAttempts_ProgrammingExamAttemptId",
                table: "ExamAppLaunchSessions");

            migrationBuilder.DropIndex(
                name: "IX_ExamAppLaunchSessions_ProgrammingExamAttemptId",
                table: "ExamAppLaunchSessions");

            migrationBuilder.DropIndex(
                name: "IX_ExamAppAuthorizations_ProgrammingExamCodeId",
                table: "ExamAppAuthorizations");

            migrationBuilder.DropColumn(
                name: "ProgrammingExamAttemptId",
                table: "ExamAppLaunchSessions");

            migrationBuilder.DropColumn(
                name: "ProgrammingExamCodeId",
                table: "ExamAppAuthorizations");

            migrationBuilder.AlterColumn<int>(
                name: "ExamAttemptId",
                table: "ExamAppLaunchSessions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ExamCodeId",
                table: "ExamAppAuthorizations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAppAuthorizations_ExamCodes_ExamCodeId",
                table: "ExamAppAuthorizations",
                column: "ExamCodeId",
                principalTable: "ExamCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
