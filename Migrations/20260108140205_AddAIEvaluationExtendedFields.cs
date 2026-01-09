using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeP.Migrations
{
    public partial class AddAIEvaluationExtendedFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AICodeSnippets",
                table: "TaskProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AISolutionSuggestion",
                table: "TaskProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CompletionPercentage",
                table: "TaskProgresses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AIEvaluationCompleted",
                table: "ProgrammingExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AIEvaluationCompletedAt",
                table: "ProgrammingExamAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AIEvaluationStartedAt",
                table: "ProgrammingExamAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequestTeacherReevaluation",
                table: "ProgrammingExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TeacherReevaluationCompleted",
                table: "ProgrammingExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TeacherReevaluationCompletedAt",
                table: "ProgrammingExamAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeacherReevaluationNotes",
                table: "ProgrammingExamAttempts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TeacherReevaluationRequestedAt",
                table: "ProgrammingExamAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7324));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7327));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7328));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7329));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7330));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7330));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7331));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7372));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7374));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2026, 1, 8, 14, 2, 5, 165, DateTimeKind.Utc).AddTicks(7375));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AICodeSnippets",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "AISolutionSuggestion",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "CompletionPercentage",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "AIEvaluationCompleted",
                table: "ProgrammingExamAttempts");

            migrationBuilder.DropColumn(
                name: "AIEvaluationCompletedAt",
                table: "ProgrammingExamAttempts");

            migrationBuilder.DropColumn(
                name: "AIEvaluationStartedAt",
                table: "ProgrammingExamAttempts");

            migrationBuilder.DropColumn(
                name: "RequestTeacherReevaluation",
                table: "ProgrammingExamAttempts");

            migrationBuilder.DropColumn(
                name: "TeacherReevaluationCompleted",
                table: "ProgrammingExamAttempts");

            migrationBuilder.DropColumn(
                name: "TeacherReevaluationCompletedAt",
                table: "ProgrammingExamAttempts");

            migrationBuilder.DropColumn(
                name: "TeacherReevaluationNotes",
                table: "ProgrammingExamAttempts");

            migrationBuilder.DropColumn(
                name: "TeacherReevaluationRequestedAt",
                table: "ProgrammingExamAttempts");

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7971));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7976));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7977));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7978));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7979));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7980));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7980));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7981));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7982));

            migrationBuilder.UpdateData(
                table: "PlatformSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2025, 12, 25, 10, 57, 4, 619, DateTimeKind.Utc).AddTicks(7983));
        }
    }
}
