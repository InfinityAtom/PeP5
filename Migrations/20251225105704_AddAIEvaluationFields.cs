using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeP.Migrations
{
    public partial class AddAIEvaluationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AIEvaluatedAt",
                table: "TaskProgresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIFeedback",
                table: "TaskProgresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AIScore",
                table: "TaskProgresses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CodeQualityScore",
                table: "TaskProgresses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CorrectnessScore",
                table: "TaskProgresses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EfficiencyScore",
                table: "TaskProgresses",
                type: "decimal(18,2)",
                nullable: true);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIEvaluatedAt",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "AIFeedback",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "AIScore",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "CodeQualityScore",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "CorrectnessScore",
                table: "TaskProgresses");

            migrationBuilder.DropColumn(
                name: "EfficiencyScore",
                table: "TaskProgresses");

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
        }
    }
}
