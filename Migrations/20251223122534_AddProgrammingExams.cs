using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeP.Migrations
{
    public partial class AddProgrammingExams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgrammingExams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<int>(type: "int", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalPoints = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllowConsoleInput = table.Column<bool>(type: "bit", nullable: false),
                    AllowFileUpload = table.Column<bool>(type: "bit", nullable: false),
                    ShowSolutionExplorer = table.Column<bool>(type: "bit", nullable: false),
                    AutoSaveIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammingExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammingExams_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammingExams_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammingExamAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProgrammingExamId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExamCodeUsed = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammingExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammingExamAttempts_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgrammingExamAttempts_ProgrammingExams_ProgrammingExamId",
                        column: x => x.ProgrammingExamId,
                        principalTable: "ProgrammingExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammingExamCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProgrammingExamId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammingExamCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammingExamCodes_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammingExamCodes_ProgrammingExams_ProgrammingExamId",
                        column: x => x.ProgrammingExamId,
                        principalTable: "ProgrammingExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammingTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgrammingExamId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TargetFiles = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammingTasks_ProgrammingExams_ProgrammingExamId",
                        column: x => x.ProgrammingExamId,
                        principalTable: "ProgrammingExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgrammingExamId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsEntryPoint = table.Column<bool>(type: "bit", nullable: false),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_ProgrammingExams_ProgrammingExamId",
                        column: x => x.ProgrammingExamId,
                        principalTable: "ProgrammingExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CodeSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgrammingExamAttemptId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FilesSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeSubmissions_ProgrammingExamAttempts_ProgrammingExamAttemptId",
                        column: x => x.ProgrammingExamAttemptId,
                        principalTable: "ProgrammingExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsoleHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgrammingExamAttemptId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsoleHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsoleHistories_ProgrammingExamAttempts_ProgrammingExamAttemptId",
                        column: x => x.ProgrammingExamAttemptId,
                        principalTable: "ProgrammingExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentProjectFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgrammingExamAttemptId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentProjectFiles_ProgrammingExamAttempts_ProgrammingExamAttemptId",
                        column: x => x.ProgrammingExamAttemptId,
                        principalTable: "ProgrammingExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgrammingExamAttemptId = table.Column<int>(type: "int", nullable: false),
                    ProgrammingTaskId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsMarkedForReview = table.Column<bool>(type: "bit", nullable: false),
                    RequiresFeedback = table.Column<bool>(type: "bit", nullable: false),
                    PointsEarned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TeacherNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskProgresses_ProgrammingExamAttempts_ProgrammingExamAttemptId",
                        column: x => x.ProgrammingExamAttemptId,
                        principalTable: "ProgrammingExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskProgresses_ProgrammingTasks_ProgrammingTaskId",
                        column: x => x.ProgrammingTaskId,
                        principalTable: "ProgrammingTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgrammingTaskId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Input = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCases_ProgrammingTasks_ProgrammingTaskId",
                        column: x => x.ProgrammingTaskId,
                        principalTable: "ProgrammingTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestCaseResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskProgressId = table.Column<int>(type: "int", nullable: false),
                    TestCaseId = table.Column<int>(type: "int", nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    ActualOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionTimeMs = table.Column<int>(type: "int", nullable: false),
                    RunAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCaseResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCaseResults_TaskProgresses_TaskProgressId",
                        column: x => x.TaskProgressId,
                        principalTable: "TaskProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestCaseResults_TestCases_TestCaseId",
                        column: x => x.TestCaseId,
                        principalTable: "TestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_CodeSubmissions_ProgrammingExamAttemptId",
                table: "CodeSubmissions",
                column: "ProgrammingExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleHistories_ProgrammingExamAttemptId",
                table: "ConsoleHistories",
                column: "ProgrammingExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingExamAttempts_ProgrammingExamId",
                table: "ProgrammingExamAttempts",
                column: "ProgrammingExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingExamAttempts_StudentId",
                table: "ProgrammingExamAttempts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingExamCodes_Code",
                table: "ProgrammingExamCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingExamCodes_CreatedByUserId",
                table: "ProgrammingExamCodes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingExamCodes_ProgrammingExamId",
                table: "ProgrammingExamCodes",
                column: "ProgrammingExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingExams_CourseId",
                table: "ProgrammingExams",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingExams_CreatedByUserId",
                table: "ProgrammingExams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingTasks_ProgrammingExamId",
                table: "ProgrammingTasks",
                column: "ProgrammingExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProgrammingExamId",
                table: "ProjectFiles",
                column: "ProgrammingExamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentProjectFiles_ProgrammingExamAttemptId",
                table: "StudentProjectFiles",
                column: "ProgrammingExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskProgresses_ProgrammingExamAttemptId",
                table: "TaskProgresses",
                column: "ProgrammingExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskProgresses_ProgrammingTaskId",
                table: "TaskProgresses",
                column: "ProgrammingTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseResults_TaskProgressId",
                table: "TestCaseResults",
                column: "TaskProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseResults_TestCaseId",
                table: "TestCaseResults",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_ProgrammingTaskId",
                table: "TestCases",
                column: "ProgrammingTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeSubmissions");

            migrationBuilder.DropTable(
                name: "ConsoleHistories");

            migrationBuilder.DropTable(
                name: "ProgrammingExamCodes");

            migrationBuilder.DropTable(
                name: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "StudentProjectFiles");

            migrationBuilder.DropTable(
                name: "TestCaseResults");

            migrationBuilder.DropTable(
                name: "TaskProgresses");

            migrationBuilder.DropTable(
                name: "TestCases");

            migrationBuilder.DropTable(
                name: "ProgrammingExamAttempts");

            migrationBuilder.DropTable(
                name: "ProgrammingTasks");

            migrationBuilder.DropTable(
                name: "ProgrammingExams");

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
        }
    }
}
