using Microsoft.EntityFrameworkCore;
using PeP.Data;
using PeP.Models;
using System.Text.Json;

namespace PeP.Services
{
    public interface IProgrammingExamService
    {
        // Exam Management
        Task<List<ProgrammingExam>> GetProgrammingExamsForTeacherAsync(string teacherId);
        Task<ProgrammingExam?> GetProgrammingExamByIdAsync(int examId);
        Task<ProgrammingExam> CreateProgrammingExamAsync(ProgrammingExam exam);
        Task<bool> UpdateProgrammingExamAsync(ProgrammingExam exam);
        Task<bool> DeleteProgrammingExamAsync(int examId);

        // Task Management
        Task<ProgrammingTask?> GetTaskByIdAsync(int taskId);
        Task<List<ProgrammingTask>> GetTasksForExamAsync(int examId);
        Task<ProgrammingTask> CreateTaskAsync(ProgrammingTask task);
        Task<bool> UpdateTaskAsync(ProgrammingTask task);
        Task<bool> DeleteTaskAsync(int taskId);

        // Test Case Management
        Task<TestCase> CreateTestCaseAsync(TestCase testCase);
        Task<bool> UpdateTestCaseAsync(TestCase testCase);
        Task<bool> DeleteTestCaseAsync(int testCaseId);

        // Project Files
        Task<ProjectFile> CreateProjectFileAsync(ProjectFile file);
        Task<bool> UpdateProjectFileAsync(ProjectFile file);
        Task<bool> DeleteProjectFileAsync(int fileId);
        Task<List<ProjectFile>> GetProjectFilesAsync(int examId);

        // Exam Codes
        Task<ProgrammingExamCode> CreateProgrammingExamCodeAsync(ProgrammingExamCode examCode);
        Task<string> GenerateProgrammingExamCodeAsync(int examId, DateTime expiresAt, int? maxUses, string createdByUserId, string? description);
        Task<List<ProgrammingExamCode>> GetProgrammingExamCodesAsync(int examId);
        Task<List<ProgrammingExamCode>> GetProgrammingExamCodesForTeacherAsync(string teacherId);
        Task<ProgrammingExamCode?> ValidateProgrammingExamCodeAsync(string code);
        Task<bool> DeleteProgrammingExamCodeAsync(int codeId);

        // Exam Attempts
        Task<ProgrammingExamAttempt> StartProgrammingExamAsync(string studentId, int examId, string examCode);
        Task<ProgrammingExamAttempt?> GetActiveAttemptAsync(string studentId, int examId);
        Task<ProgrammingExamAttempt?> GetAttemptByIdAsync(int attemptId);
        Task<List<ProgrammingExamAttempt>> GetAttemptsForExamAsync(int examId);
        Task<List<ProgrammingExamAttempt>> GetAttemptsForStudentAsync(string studentId);

        // File Operations during exam
        Task SaveStudentFileAsync(int attemptId, string filePath, string fileName, string content);
        Task DeleteStudentFileAsync(int attemptId, string filePath);
        Task<List<StudentProjectFile>> GetStudentFilesAsync(int attemptId);
        Task<StudentProjectFile?> GetStudentFileAsync(int attemptId, string filePath);
        Task ResetStudentFilesToStarterAsync(int attemptId, List<ProjectFile> starterFiles);

        // Task Progress
        Task UpdateTaskProgressAsync(int attemptId, int taskId, ProgrammingTaskStatus status, bool markedForReview = false, bool needsFeedback = false);
        Task<List<TaskProgress>> GetTaskProgressAsync(int attemptId);

        // Code Submissions
        Task SaveCodeSubmissionAsync(int attemptId, SubmissionType type, Dictionary<string, string> files);
        Task<ProgrammingExamAttempt> SubmitProgrammingExamAsync(int attemptId);

        // AI Evaluation
        Task<bool> EvaluateAttemptWithAIAsync(int attemptId);
        Task<ProgrammingExamAttempt?> GetAttemptWithEvaluationAsync(int attemptId);
        Task<bool> SaveTaskGradesAsync(int attemptId, List<TaskGradeUpdate> grades);
        Task<bool> RequestTeacherReevaluationAsync(int attemptId);
        Task<bool> CompleteTeacherReevaluationAsync(int attemptId, string? notes);
        Task<bool> CheckEvaluationStatusAsync(int attemptId);
        Task<byte[]> GenerateCodeArchiveWithCommentsAsync(int attemptId);

        // Console History
        Task AddConsoleEntryAsync(int attemptId, ConsoleEntryType type, string content);
        Task<List<ConsoleHistory>> GetConsoleHistoryAsync(int attemptId);
        Task ClearConsoleHistoryAsync(int attemptId);
    }

    public class TaskGradeUpdate
    {
        public int TaskProgressId { get; set; }
        public decimal PointsEarned { get; set; }
        public string? TeacherNotes { get; set; }
    }

    public class ProgrammingExamService : IProgrammingExamService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ProgrammingExamService> _logger;
        private readonly IOpenAIService _openAIService;

        public ProgrammingExamService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<ProgrammingExamService> logger,
            IOpenAIService openAIService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _openAIService = openAIService;
        }

        #region Exam Management

        public async Task<List<ProgrammingExam>> GetProgrammingExamsForTeacherAsync(string teacherId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExams
                .Include(pe => pe.Course)
                .Include(pe => pe.Tasks)
                .Include(pe => pe.StarterFiles)
                .Include(pe => pe.Attempts)
                    .ThenInclude(a => a.Student)
                .Include(pe => pe.ExamCodes)
                .Where(pe => pe.CreatedByUserId == teacherId && pe.IsActive)
                .OrderByDescending(pe => pe.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProgrammingExam?> GetProgrammingExamByIdAsync(int examId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExams
                .Include(pe => pe.Course)
                .Include(pe => pe.Tasks)
                    .ThenInclude(t => t.TestCases)
                .Include(pe => pe.StarterFiles)
                .Include(pe => pe.CreatedBy)
                .FirstOrDefaultAsync(pe => pe.Id == examId);
        }

        public async Task<ProgrammingExam> CreateProgrammingExamAsync(ProgrammingExam exam)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            exam.CreatedAt = DateTime.UtcNow;
            context.ProgrammingExams.Add(exam);
            await context.SaveChangesAsync();
            return exam;
        }

        public async Task<bool> UpdateProgrammingExamAsync(ProgrammingExam exam)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.ProgrammingExams.Update(exam);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating programming exam {ExamId}", exam.Id);
                return false;
            }
        }

        public async Task<bool> DeleteProgrammingExamAsync(int examId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var exam = await context.ProgrammingExams.FindAsync(examId);
                if (exam == null) return false;

                exam.IsActive = false;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting programming exam {ExamId}", examId);
                return false;
            }
        }

        #endregion

        #region Task Management

        public async Task<ProgrammingTask?> GetTaskByIdAsync(int taskId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingTasks
                .Include(t => t.TestCases)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<List<ProgrammingTask>> GetTasksForExamAsync(int examId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingTasks
                .Include(t => t.TestCases)
                .Where(t => t.ProgrammingExamId == examId)
                .OrderBy(t => t.Order)
                .ToListAsync();
        }

        public async Task<ProgrammingTask> CreateTaskAsync(ProgrammingTask task)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.ProgrammingTasks.Add(task);
            await context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> UpdateTaskAsync(ProgrammingTask task)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.ProgrammingTasks.Update(task);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", task.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var task = await context.ProgrammingTasks.FindAsync(taskId);
                if (task == null) return false;

                context.ProgrammingTasks.Remove(task);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", taskId);
                return false;
            }
        }

        #endregion

        #region Test Case Management

        public async Task<TestCase> CreateTestCaseAsync(TestCase testCase)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.TestCases.Add(testCase);
            await context.SaveChangesAsync();
            return testCase;
        }

        public async Task<bool> UpdateTestCaseAsync(TestCase testCase)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.TestCases.Update(testCase);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test case {TestCaseId}", testCase.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTestCaseAsync(int testCaseId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var testCase = await context.TestCases.FindAsync(testCaseId);
                if (testCase == null) return false;

                context.TestCases.Remove(testCase);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting test case {TestCaseId}", testCaseId);
                return false;
            }
        }

        #endregion

        #region Project Files

        public async Task<ProjectFile> CreateProjectFileAsync(ProjectFile file)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.ProjectFiles.Add(file);
            await context.SaveChangesAsync();
            return file;
        }

        public async Task<bool> UpdateProjectFileAsync(ProjectFile file)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.ProjectFiles.Update(file);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project file {FileId}", file.Id);
                return false;
            }
        }

        public async Task<bool> DeleteProjectFileAsync(int fileId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var file = await context.ProjectFiles.FindAsync(fileId);
                if (file == null) return false;

                context.ProjectFiles.Remove(file);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project file {FileId}", fileId);
                return false;
            }
        }

        public async Task<List<ProjectFile>> GetProjectFilesAsync(int examId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProjectFiles
                .Where(pf => pf.ProgrammingExamId == examId)
                .OrderBy(pf => pf.Order)
                .ToListAsync();
        }

        #endregion

        #region Exam Codes

        public async Task<ProgrammingExamCode> CreateProgrammingExamCodeAsync(ProgrammingExamCode examCode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Ensure unique code
            if (await context.ProgrammingExamCodes.AnyAsync(ec => ec.Code == examCode.Code))
            {
                // Generate a new unique code
                do
                {
                    examCode.Code = GenerateRandomCode(6);
                } while (await context.ProgrammingExamCodes.AnyAsync(ec => ec.Code == examCode.Code));
            }

            context.ProgrammingExamCodes.Add(examCode);
            await context.SaveChangesAsync();

            return examCode;
        }

        public async Task<string> GenerateProgrammingExamCodeAsync(int examId, DateTime expiresAt, int? maxUses, string createdByUserId, string? description)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            string code;
            do
            {
                code = GenerateRandomCode(8);
            } while (await context.ProgrammingExamCodes.AnyAsync(ec => ec.Code == code));

            var examCode = new ProgrammingExamCode
            {
                Code = code,
                ProgrammingExamId = examId,
                CreatedByUserId = createdByUserId,
                ExpiresAt = expiresAt,
                MaxUses = maxUses,
                Description = description
            };

            context.ProgrammingExamCodes.Add(examCode);
            await context.SaveChangesAsync();

            return code;
        }

        public async Task<List<ProgrammingExamCode>> GetProgrammingExamCodesAsync(int examId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExamCodes
                .Include(ec => ec.CreatedBy)
                .Where(ec => ec.ProgrammingExamId == examId)
                .OrderByDescending(ec => ec.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ProgrammingExamCode>> GetProgrammingExamCodesForTeacherAsync(string teacherId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExamCodes
                .Include(ec => ec.ProgrammingExam)
                    .ThenInclude(pe => pe.Course)
                .Include(ec => ec.CreatedBy)
                .Where(ec => ec.ProgrammingExam.CreatedByUserId == teacherId)
                .OrderByDescending(ec => ec.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProgrammingExamCode?> ValidateProgrammingExamCodeAsync(string code)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var examCode = await context.ProgrammingExamCodes
                .Include(ec => ec.ProgrammingExam)
                    .ThenInclude(pe => pe.Course)
                .FirstOrDefaultAsync(ec => 
                    ec.Code == code && 
                    ec.IsActive && 
                    ec.ExpiresAt > DateTime.UtcNow &&
                    (ec.MaxUses == null || ec.TimesUsed < ec.MaxUses));

            return examCode;
        }

        public async Task<bool> DeleteProgrammingExamCodeAsync(int codeId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var code = await context.ProgrammingExamCodes.FindAsync(codeId);
                if (code == null) return false;

                code.IsActive = false;
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting programming exam code {CodeId}", codeId);
                return false;
            }
        }

        #endregion

        #region Exam Attempts

        public async Task<ProgrammingExamAttempt> StartProgrammingExamAsync(string studentId, int examId, string examCode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Validate exam code
            var code = await context.ProgrammingExamCodes
                .FirstOrDefaultAsync(ec => ec.Code == examCode && ec.ProgrammingExamId == examId);

            if (code != null)
            {
                code.TimesUsed++;
            }

            // Get exam with starter files
            var exam = await context.ProgrammingExams
                .Include(pe => pe.StarterFiles)
                .Include(pe => pe.Tasks)
                .FirstAsync(pe => pe.Id == examId);

            // Create attempt
            var attempt = new ProgrammingExamAttempt
            {
                StudentId = studentId,
                ProgrammingExamId = examId,
                ExamCodeUsed = examCode,
                Status = ProgrammingExamStatus.InProgress
            };

            context.ProgrammingExamAttempts.Add(attempt);
            await context.SaveChangesAsync();

            // Copy starter files to student files
            foreach (var starterFile in exam.StarterFiles)
            {
                var studentFile = new StudentProjectFile
                {
                    ProgrammingExamAttemptId = attempt.Id,
                    FilePath = starterFile.FilePath,
                    FileName = starterFile.FileName,
                    Content = starterFile.Content
                };
                context.StudentProjectFiles.Add(studentFile);
            }

            // Initialize task progress
            foreach (var task in exam.Tasks.OrderBy(t => t.Order))
            {
                var progress = new TaskProgress
                {
                    ProgrammingExamAttemptId = attempt.Id,
                    ProgrammingTaskId = task.Id,
                    Status = ProgrammingTaskStatus.NotStarted
                };
                context.TaskProgresses.Add(progress);
            }

            await context.SaveChangesAsync();

            return attempt;
        }

        public async Task<ProgrammingExamAttempt?> GetActiveAttemptAsync(string studentId, int examId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExamAttempts
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.Tasks)
                .Include(a => a.StudentFiles)
                .Include(a => a.TaskProgress)
                .FirstOrDefaultAsync(a => 
                    a.StudentId == studentId && 
                    a.ProgrammingExamId == examId && 
                    a.Status == ProgrammingExamStatus.InProgress);
        }

        public async Task<ProgrammingExamAttempt?> GetAttemptByIdAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExamAttempts
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.Tasks)
                        .ThenInclude(t => t.TestCases)
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.Course)
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.StarterFiles)
                .Include(a => a.Student)
                .Include(a => a.StudentFiles)
                .Include(a => a.TaskProgress)
                    .ThenInclude(tp => tp.TestCaseResults)
                .Include(a => a.ConsoleHistory.OrderBy(ch => ch.Timestamp))
                .FirstOrDefaultAsync(a => a.Id == attemptId);
        }

        public async Task<List<ProgrammingExamAttempt>> GetAttemptsForExamAsync(int examId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExamAttempts
                .Include(a => a.Student)
                .Include(a => a.TaskProgress)
                .Where(a => a.ProgrammingExamId == examId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }

        public async Task<List<ProgrammingExamAttempt>> GetAttemptsForStudentAsync(string studentId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ProgrammingExamAttempts
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.Course)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }

        #endregion

        #region File Operations

        public async Task SaveStudentFileAsync(int attemptId, string filePath, string fileName, string content)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existingFile = await context.StudentProjectFiles
                .FirstOrDefaultAsync(f => f.ProgrammingExamAttemptId == attemptId && f.FilePath == filePath);

            if (existingFile != null)
            {
                existingFile.Content = content;
                existingFile.LastModified = DateTime.UtcNow;
            }
            else
            {
                var newFile = new StudentProjectFile
                {
                    ProgrammingExamAttemptId = attemptId,
                    FilePath = filePath,
                    FileName = fileName,
                    Content = content
                };
                context.StudentProjectFiles.Add(newFile);
            }

            // Update last activity
            var attempt = await context.ProgrammingExamAttempts.FindAsync(attemptId);
            if (attempt != null)
            {
                attempt.LastActivity = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        public async Task DeleteStudentFileAsync(int attemptId, string filePath)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var file = await context.StudentProjectFiles
                .FirstOrDefaultAsync(f => f.ProgrammingExamAttemptId == attemptId && f.FilePath == filePath);

            if (file != null)
            {
                context.StudentProjectFiles.Remove(file);

                // Update last activity
                var attempt = await context.ProgrammingExamAttempts.FindAsync(attemptId);
                if (attempt != null)
                {
                    attempt.LastActivity = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task<List<StudentProjectFile>> GetStudentFilesAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.StudentProjectFiles
                .Where(f => f.ProgrammingExamAttemptId == attemptId)
                .OrderBy(f => f.FilePath)
                .ToListAsync();
        }

        public async Task<StudentProjectFile?> GetStudentFileAsync(int attemptId, string filePath)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.StudentProjectFiles
                .FirstOrDefaultAsync(f => f.ProgrammingExamAttemptId == attemptId && f.FilePath == filePath);
        }

        public async Task ResetStudentFilesToStarterAsync(int attemptId, List<ProjectFile> starterFiles)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Delete all existing student files for this attempt
            var existingFiles = await context.StudentProjectFiles
                .Where(f => f.ProgrammingExamAttemptId == attemptId)
                .ToListAsync();
            
            context.StudentProjectFiles.RemoveRange(existingFiles);

            // Create new student files from starter files
            foreach (var starterFile in starterFiles)
            {
                var newFile = new StudentProjectFile
                {
                    ProgrammingExamAttemptId = attemptId,
                    FilePath = starterFile.FilePath,
                    FileName = starterFile.FileName,
                    Content = starterFile.Content,
                    LastModified = DateTime.UtcNow
                };
                context.StudentProjectFiles.Add(newFile);
            }

            // Update last activity
            var attempt = await context.ProgrammingExamAttempts.FindAsync(attemptId);
            if (attempt != null)
            {
                attempt.LastActivity = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        #endregion

        #region Task Progress

        public async Task UpdateTaskProgressAsync(int attemptId, int taskId, ProgrammingTaskStatus status, bool markedForReview = false, bool needsFeedback = false)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var progress = await context.TaskProgresses
                .FirstOrDefaultAsync(tp => tp.ProgrammingExamAttemptId == attemptId && tp.ProgrammingTaskId == taskId);

            if (progress != null)
            {
                progress.Status = status;
                progress.IsMarkedForReview = markedForReview;
                progress.RequiresFeedback = needsFeedback;
                progress.LastUpdated = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<TaskProgress>> GetTaskProgressAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskProgresses
                .Include(tp => tp.ProgrammingTask)
                .Include(tp => tp.TestCaseResults)
                    .ThenInclude(tcr => tcr.TestCase)
                .Where(tp => tp.ProgrammingExamAttemptId == attemptId)
                .OrderBy(tp => tp.ProgrammingTask.Order)
                .ToListAsync();
        }

        #endregion

        #region Code Submissions

        public async Task SaveCodeSubmissionAsync(int attemptId, SubmissionType type, Dictionary<string, string> files)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var submission = new CodeSubmission
            {
                ProgrammingExamAttemptId = attemptId,
                Type = type,
                FilesSnapshot = JsonSerializer.Serialize(files)
            };

            context.CodeSubmissions.Add(submission);
            await context.SaveChangesAsync();
        }

        public async Task<ProgrammingExamAttempt> SubmitProgrammingExamAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var attempt = await context.ProgrammingExamAttempts
                .Include(a => a.StudentFiles)
                .Include(a => a.TaskProgress)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                throw new InvalidOperationException("Attempt not found");

            // Save final submission
            var filesDict = attempt.StudentFiles.ToDictionary(f => f.FilePath, f => f.Content);
            var finalSubmission = new CodeSubmission
            {
                ProgrammingExamAttemptId = attemptId,
                Type = SubmissionType.FinalSubmission,
                FilesSnapshot = JsonSerializer.Serialize(filesDict)
            };
            context.CodeSubmissions.Add(finalSubmission);

            attempt.Status = ProgrammingExamStatus.Completed;
            attempt.SubmittedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return attempt;
        }

        #endregion

        #region Console History

        public async Task AddConsoleEntryAsync(int attemptId, ConsoleEntryType type, string content)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entry = new ConsoleHistory
            {
                ProgrammingExamAttemptId = attemptId,
                Type = type,
                Content = content
            };

            context.ConsoleHistories.Add(entry);
            await context.SaveChangesAsync();
        }

        public async Task<List<ConsoleHistory>> GetConsoleHistoryAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ConsoleHistories
                .Where(ch => ch.ProgrammingExamAttemptId == attemptId)
                .OrderBy(ch => ch.Timestamp)
                .ToListAsync();
        }

        public async Task ClearConsoleHistoryAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entries = await context.ConsoleHistories
                .Where(ch => ch.ProgrammingExamAttemptId == attemptId)
                .ToListAsync();

            context.ConsoleHistories.RemoveRange(entries);
            await context.SaveChangesAsync();
        }

        #endregion

        #region AI Evaluation

        public async Task<bool> EvaluateAttemptWithAIAsync(int attemptId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var attempt = await context.ProgrammingExamAttempts
                    .Include(a => a.ProgrammingExam)
                        .ThenInclude(pe => pe.Tasks)
                    .Include(a => a.StudentFiles)
                    .Include(a => a.TaskProgress)
                    .FirstOrDefaultAsync(a => a.Id == attemptId);

                if (attempt == null)
                {
                    _logger.LogWarning("Attempt {AttemptId} not found for AI evaluation", attemptId);
                    return false;
                }

                // Mark evaluation as started
                attempt.AIEvaluationStartedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                // Evaluate all tasks with AI
                var evaluations = await _openAIService.EvaluateAllTasksAsync(attempt);

                decimal totalScore = 0;

                foreach (var evaluation in evaluations)
                {
                    var taskProgress = attempt.TaskProgress.FirstOrDefault(tp => tp.ProgrammingTaskId == evaluation.TaskId);
                    
                    if (taskProgress != null && evaluation.Success)
                    {
                        taskProgress.AIScore = evaluation.Score;
                        taskProgress.AIFeedback = $"**Feedback:**\n{evaluation.Feedback}\n\n**Strengths:**\n{evaluation.Strengths}\n\n**Areas for Improvement:**\n{evaluation.AreasForImprovement}";
                        taskProgress.AIEvaluatedAt = DateTime.UtcNow;
                        taskProgress.CodeQualityScore = evaluation.CodeQualityScore;
                        taskProgress.CorrectnessScore = evaluation.CorrectnessScore;
                        taskProgress.EfficiencyScore = evaluation.EfficiencyScore;
                        taskProgress.CompletionPercentage = evaluation.CompletionPercentage;
                        taskProgress.AISolutionSuggestion = evaluation.SolutionSuggestion;
                        taskProgress.AICodeSnippets = evaluation.CodeSnippets?.Any() == true 
                            ? System.Text.Json.JsonSerializer.Serialize(evaluation.CodeSnippets) 
                            : null;
                        taskProgress.PointsEarned = evaluation.Score;
                        taskProgress.LastUpdated = DateTime.UtcNow;

                        totalScore += evaluation.Score;
                    }
                    else if (taskProgress != null && !evaluation.Success)
                    {
                        taskProgress.AIFeedback = $"AI Evaluation failed: {evaluation.ErrorMessage}";
                        taskProgress.AIEvaluatedAt = DateTime.UtcNow;
                        taskProgress.RequiresFeedback = true; // Flag for manual review
                    }
                }

                // Update total score on attempt and mark evaluation as completed
                attempt.TotalScore = totalScore;
                attempt.AIEvaluationCompleted = true;
                attempt.AIEvaluationCompletedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation("AI evaluation completed for attempt {AttemptId}, total score: {Score}", attemptId, totalScore);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI evaluation for attempt {AttemptId}", attemptId);
                return false;
            }
        }

        public async Task<ProgrammingExamAttempt?> GetAttemptWithEvaluationAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ProgrammingExamAttempts
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.Course)
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.Tasks)
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.StarterFiles)
                .Include(a => a.Student)
                .Include(a => a.StudentFiles)
                .Include(a => a.TaskProgress)
                    .ThenInclude(tp => tp.ProgrammingTask)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == attemptId);
        }

        public async Task<bool> SaveTaskGradesAsync(int attemptId, List<TaskGradeUpdate> grades)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var attempt = await context.ProgrammingExamAttempts
                    .Include(a => a.TaskProgress)
                    .FirstOrDefaultAsync(a => a.Id == attemptId);

                if (attempt == null)
                {
                    _logger.LogWarning("Attempt {AttemptId} not found for saving grades", attemptId);
                    return false;
                }

                decimal totalScore = 0;

                foreach (var grade in grades)
                {
                    var taskProgress = attempt.TaskProgress.FirstOrDefault(tp => tp.Id == grade.TaskProgressId);
                    if (taskProgress != null)
                    {
                        taskProgress.PointsEarned = grade.PointsEarned;
                        taskProgress.TeacherNotes = grade.TeacherNotes;
                        taskProgress.LastUpdated = DateTime.UtcNow;
                        totalScore += grade.PointsEarned;
                    }
                }

                attempt.TotalScore = totalScore;
                await context.SaveChangesAsync();

                _logger.LogInformation("Grades saved for attempt {AttemptId}, total score: {Score}", attemptId, totalScore);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving grades for attempt {AttemptId}", attemptId);
                return false;
            }
        }

        public async Task<bool> RequestTeacherReevaluationAsync(int attemptId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var attempt = await context.ProgrammingExamAttempts
                    .FirstOrDefaultAsync(a => a.Id == attemptId);

                if (attempt == null)
                {
                    _logger.LogWarning("Attempt {AttemptId} not found for reevaluation request", attemptId);
                    return false;
                }

                attempt.RequestTeacherReevaluation = true;
                attempt.TeacherReevaluationRequestedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation("Teacher reevaluation requested for attempt {AttemptId}", attemptId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting teacher reevaluation for attempt {AttemptId}", attemptId);
                return false;
            }
        }

        public async Task<bool> CompleteTeacherReevaluationAsync(int attemptId, string? notes)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var attempt = await context.ProgrammingExamAttempts
                    .FirstOrDefaultAsync(a => a.Id == attemptId);

                if (attempt == null)
                {
                    _logger.LogWarning("Attempt {AttemptId} not found for reevaluation completion", attemptId);
                    return false;
                }

                attempt.TeacherReevaluationCompleted = true;
                attempt.TeacherReevaluationCompletedAt = DateTime.UtcNow;
                attempt.TeacherReevaluationNotes = notes;
                await context.SaveChangesAsync();

                _logger.LogInformation("Teacher reevaluation completed for attempt {AttemptId}", attemptId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing teacher reevaluation for attempt {AttemptId}", attemptId);
                return false;
            }
        }

        public async Task<bool> CheckEvaluationStatusAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var attempt = await context.ProgrammingExamAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            return attempt?.AIEvaluationCompleted ?? false;
        }

        public async Task<byte[]> GenerateCodeArchiveWithCommentsAsync(int attemptId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var attempt = await context.ProgrammingExamAttempts
                .Include(a => a.ProgrammingExam)
                    .ThenInclude(pe => pe.Tasks)
                .Include(a => a.StudentFiles)
                .Include(a => a.TaskProgress)
                    .ThenInclude(tp => tp.ProgrammingTask)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
            {
                throw new InvalidOperationException("Attempt not found");
            }

            using var memoryStream = new System.IO.MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                // Add student files with AI comments
                foreach (var file in attempt.StudentFiles)
                {
                    var entry = archive.CreateEntry(file.FilePath);
                    using var entryStream = entry.Open();
                    using var writer = new System.IO.StreamWriter(entryStream);

                    // Get task progress for this file if there's associated feedback
                    var relatedTaskProgress = attempt.TaskProgress
                        .Where(tp => tp.ProgrammingTask?.TargetFiles != null &&
                               tp.ProgrammingTask.TargetFiles.Contains(file.FileName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    // Write AI feedback header if available
                    if (relatedTaskProgress?.AIFeedback != null)
                    {
                        var ext = System.IO.Path.GetExtension(file.FileName).ToLower();
                        var commentStart = ext switch
                        {
                            ".py" => "# ",
                            ".java" or ".cs" or ".cpp" or ".c" or ".js" or ".ts" => "// ",
                            _ => "# "
                        };
                        var blockStart = ext switch
                        {
                            ".py" => "\"\"\"",
                            ".java" or ".cs" or ".cpp" or ".c" or ".js" or ".ts" => "/*",
                            _ => "/*"
                        };
                        var blockEnd = ext switch
                        {
                            ".py" => "\"\"\"",
                            _ => "*/"
                        };

                        await writer.WriteLineAsync(blockStart);
                        await writer.WriteLineAsync($" AI EVALUATION FEEDBACK");
                        await writer.WriteLineAsync($" Task: {relatedTaskProgress.ProgrammingTask?.Title}");
                        await writer.WriteLineAsync($" Score: {relatedTaskProgress.AIScore}/{relatedTaskProgress.ProgrammingTask?.Points}");
                        await writer.WriteLineAsync($" Completion: {relatedTaskProgress.CompletionPercentage}%");
                        await writer.WriteLineAsync();
                        
                        foreach (var line in (relatedTaskProgress.AIFeedback ?? "").Split('\n'))
                        {
                            await writer.WriteLineAsync($" {line}");
                        }
                        
                        if (!string.IsNullOrEmpty(relatedTaskProgress.AISolutionSuggestion))
                        {
                            await writer.WriteLineAsync();
                            await writer.WriteLineAsync(" SUGGESTED APPROACH:");
                            await writer.WriteLineAsync($" {relatedTaskProgress.AISolutionSuggestion}");
                        }
                        
                        await writer.WriteLineAsync(blockEnd);
                        await writer.WriteLineAsync();
                    }

                    await writer.WriteAsync(file.Content);
                }

                // Add a summary file
                var summaryEntry = archive.CreateEntry("AI_EVALUATION_SUMMARY.txt");
                using var summaryStream = summaryEntry.Open();
                using var summaryWriter = new System.IO.StreamWriter(summaryStream);

                await summaryWriter.WriteLineAsync("=== AI EVALUATION SUMMARY ===");
                await summaryWriter.WriteLineAsync($"Exam: {attempt.ProgrammingExam.Title}");
                await summaryWriter.WriteLineAsync($"Total Score: {attempt.TotalScore}/{attempt.ProgrammingExam.TotalPoints}");
                await summaryWriter.WriteLineAsync($"Evaluated At: {attempt.AIEvaluationCompletedAt:g}");
                await summaryWriter.WriteLineAsync();

                foreach (var taskProgress in attempt.TaskProgress.OrderBy(tp => tp.ProgrammingTask?.Order))
                {
                    await summaryWriter.WriteLineAsync($"--- Task: {taskProgress.ProgrammingTask?.Title} ---");
                    await summaryWriter.WriteLineAsync($"Score: {taskProgress.AIScore}/{taskProgress.ProgrammingTask?.Points}");
                    await summaryWriter.WriteLineAsync($"Completion: {taskProgress.CompletionPercentage}%");
                    await summaryWriter.WriteLineAsync($"Code Quality: {taskProgress.CodeQualityScore}%");
                    await summaryWriter.WriteLineAsync($"Correctness: {taskProgress.CorrectnessScore}%");
                    await summaryWriter.WriteLineAsync($"Efficiency: {taskProgress.EfficiencyScore}%");
                    await summaryWriter.WriteLineAsync();
                    
                    if (!string.IsNullOrEmpty(taskProgress.AIFeedback))
                    {
                        await summaryWriter.WriteLineAsync(taskProgress.AIFeedback);
                    }
                    
                    await summaryWriter.WriteLineAsync();
                }
            }

            return memoryStream.ToArray();
        }

        #endregion

        #region Helpers

        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}
