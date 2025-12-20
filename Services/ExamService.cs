using Microsoft.EntityFrameworkCore;
using PeP.Data;
using PeP.Models;
using System.Text.Json;

namespace PeP.Services
{
    public interface IExamService
    {
        Task<List<Exam>> GetExamsForTeacherAsync(string teacherId);
        Task<Exam?> GetExamByIdAsync(int examId);
        Task<ExamAttempt?> GetActiveExamAttemptAsync(string studentId, int examId);
        Task<ExamAttempt> StartExamAsync(string studentId, int examId, string examCode);
        Task<bool> ValidateExamCodeAsync(string code, int examId);
        Task SaveAnswerAsync(int examAttemptId, int questionId, List<int> selectedChoiceIds, bool isMarkedForReview);
        Task<ExamAttempt> SubmitExamAsync(int examAttemptId);
        Task<ExamAttempt> GetExamResultAsync(int examAttemptId, string studentId);
        Task<List<Question>> GetShuffledQuestionsAsync(int examId, bool shuffle);
        Task<decimal> CalculateScoreAsync(ExamAttempt examAttempt);
        Task<string> GenerateExamCodeAsync(int examId, DateTime expiresAt, int? maxUses, string createdByUserId, string? description);
        Task<List<ExamCode>> GetExamCodesAsync(int examId);
        Task<List<Exam>> GetAllExamsAsync();
        Task<List<ExamAttempt>> GetAllExamAttemptsAsync();
        Task<List<Exam>> GetAvailableExamsForStudentAsync(string studentId);
        Task<List<ExamAttempt>> GetCompletedExamAttemptsAsync(string studentId);
        Task<List<ExamAttempt>> GetInProgressExamAttemptsAsync(string studentId);
        Task<ExamAttempt?> GetExamAttemptDetailsAsync(int attemptId, string studentId);
        Task<List<ExamAttempt>> GetAllExamAttemptsForTeacherAsync(string teacherId);
        Task<List<ExamCode>> GetActiveExamCodesForTeacherAsync(string teacherId);
        Task<List<ExamCode>> GetExamCodesForTeacherAsync(string teacherId);
        Task<ExamCode?> GetValidExamCodeAsync(string code);
        Task<List<Course>> GetAllCoursesAsync();
        Task<bool> DeleteCourseAsync(int courseId);
        Task<bool> DeleteExamAsync(int examId);
        Task<bool> DeleteExamCodeAsync(int examCodeId);
        Task<bool> UpdateExamAsync(Exam exam);
    }

    public class ExamService : IExamService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ExamService> _logger;

        public ExamService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ExamService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<Exam>> GetExamsForTeacherAsync(string teacherId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Questions)
                .Include(e => e.ExamCodes)
                .Include(e => e.ExamAttempts)
                .Where(e => e.CreatedByUserId == teacherId && e.IsActive)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Exam>> GetAllExamsAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Questions)
                .Include(e => e.ExamCodes)
                .Include(e => e.ExamAttempts)
                .Where(e => e.IsActive)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ExamAttempt>> GetAllExamAttemptsAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.Student)
                .OrderByDescending(ea => ea.StartedAt)
                .ToListAsync();
        }

        public async Task<List<Exam>> GetAvailableExamsForStudentAsync(string studentId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            // Get exams that have active exam codes and student hasn't completed
            var completedExamIds = await _context.ExamAttempts
                .Where(ea => ea.StudentId == studentId && ea.Status == ExamAttemptStatus.Completed)
                .Select(ea => ea.ExamId)
                .ToListAsync();

            return await _context.Exams
                .Include(e => e.Course)
                .Where(e => e.IsActive && 
                           !completedExamIds.Contains(e.Id) &&
                           e.ExamCodes.Any(ec => ec.IsActive && ec.ExpiresAt > DateTime.UtcNow))
                .OrderBy(e => e.Title)
                .ToListAsync();
        }

        public async Task<List<ExamAttempt>> GetCompletedExamAttemptsAsync(string studentId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.Student)
                .Where(ea => ea.StudentId == studentId && ea.Status == ExamAttemptStatus.Completed)
                .OrderByDescending(ea => ea.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<ExamAttempt>> GetInProgressExamAttemptsAsync(string studentId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.Student)
                .Where(ea => ea.StudentId == studentId && ea.Status == ExamAttemptStatus.InProgress)
                .OrderByDescending(ea => ea.StartedAt)
                .ToListAsync();
        }

        public async Task<List<ExamAttempt>> GetAllExamAttemptsForTeacherAsync(string teacherId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.Student)
                .Where(ea => ea.Exam.CreatedByUserId == teacherId)
                .OrderByDescending(ea => ea.StartedAt)
                .ToListAsync();
        }

        public async Task<ExamAttempt?> GetExamAttemptDetailsAsync(int attemptId, string studentId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Choices)
                .Include(ea => ea.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                        .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(ea => ea.Id == attemptId && ea.StudentId == studentId);
        }

        public async Task<List<ExamCode>> GetActiveExamCodesForTeacherAsync(string teacherId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamCodes
                .Include(ec => ec.Exam)
                .Include(ec => ec.CreatedBy)
                .Where(ec => ec.Exam.CreatedByUserId == teacherId && ec.IsActive)
                .OrderByDescending(ec => ec.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ExamCode>> GetExamCodesForTeacherAsync(string teacherId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamCodes
                .Include(ec => ec.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ec => ec.CreatedBy)
                .Where(ec => ec.Exam.CreatedByUserId == teacherId)
                .OrderByDescending(ec => ec.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Course>> GetAllCoursesAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.Courses
                .Include(c => c.Exams)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var course = await _context.Courses.FindAsync(courseId);
                if (course != null)
                {
                    course.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", courseId);
                return false;
            }
        }

        public async Task<bool> DeleteExamAsync(int examId)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var exam = await _context.Exams.FindAsync(examId);
                if (exam != null)
                {
                    exam.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exam {ExamId}", examId);
                return false;
            }
        }

        public async Task<bool> DeleteExamCodeAsync(int examCodeId)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                var examCode = await _context.ExamCodes.FindAsync(examCodeId);
                if (examCode != null)
                {
                    examCode.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exam code {ExamCodeId}", examCodeId);
                return false;
            }
        }

        public async Task<Exam?> GetExamByIdAsync(int examId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                .Include(e => e.CreatedBy)
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive);
        }

        public async Task<ExamAttempt?> GetActiveExamAttemptAsync(string studentId, int examId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.StudentAnswers)
                .FirstOrDefaultAsync(ea => ea.StudentId == studentId && 
                                         ea.ExamId == examId && 
                                         ea.Status == ExamAttemptStatus.InProgress);
        }

        public async Task<ExamAttempt> StartExamAsync(string studentId, int examId, string examCode)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Validate exam code
            var isValidCode = await ValidateExamCodeAsync(code: examCode, examId: examId);
            if (!isValidCode)
            {
                throw new UnauthorizedAccessException("Invalid or expired exam code.");
            }

            // Check if student already has an active attempt
            var existingAttempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(ea => ea.StudentId == studentId && ea.ExamId == examId && ea.Status == ExamAttemptStatus.InProgress);
            if (existingAttempt != null)
            {
                return await _context.ExamAttempts
                    .Include(ea => ea.Exam)
                        .ThenInclude(e => e.Course)
                    .Include(ea => ea.Exam)
                        .ThenInclude(e => e.Questions)
                            .ThenInclude(q => q.Choices)
                    .Include(ea => ea.StudentAnswers)
                        .ThenInclude(sa => sa.Question)
                            .ThenInclude(q => q.Choices)
                    .FirstAsync(ea => ea.Id == existingAttempt.Id);
            }

            // Create new attempt
            var examAttempt = new ExamAttempt
            {
                StudentId = studentId,
                ExamId = examId,
                StartedAt = DateTime.UtcNow,
                Status = ExamAttemptStatus.InProgress,
                ExamCodeUsed = examCode
            };

            _context.ExamAttempts.Add(examAttempt);

            // Update exam code usage
            var codeEntity = await _context.ExamCodes
                .FirstOrDefaultAsync(ec => ec.Code == examCode && ec.ExamId == examId);
            if (codeEntity != null)
            {
                codeEntity.TimesUsed++;
            }

            await _context.SaveChangesAsync();

            // Initialize student answers
            var exam = await _context.Exams
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(e => e.Id == examId);
            var questions = await GetShuffledQuestionsAsyncInternal(_context, examId, exam?.ShuffleQuestions ?? true);

            foreach (var question in questions)
            {
                var studentAnswer = new StudentAnswer
                {
                    ExamAttemptId = examAttempt.Id,
                    QuestionId = question.Id,
                    IsCompleted = false,
                    IsMarkedForReview = false
                };
                _context.StudentAnswers.Add(studentAnswer);
            }

            await _context.SaveChangesAsync();

            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                        .ThenInclude(q => q.Choices)
                .FirstAsync(ea => ea.Id == examAttempt.Id);
        }

        public async Task<bool> ValidateExamCodeAsync(string code, int examId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var examCode = await _context.ExamCodes
                .FirstOrDefaultAsync(ec => ec.Code == code && 
                                         ec.ExamId == examId && 
                                         ec.IsActive);

            if (examCode == null) return false;
            if (examCode.ExpiresAt < DateTime.UtcNow) return false;
            if (examCode.MaxUses.HasValue && examCode.TimesUsed >= examCode.MaxUses.Value) return false;

            return true;
        }

        public async Task SaveAnswerAsync(int examAttemptId, int questionId, List<int> selectedChoiceIds, bool isMarkedForReview)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var studentAnswer = await _context.StudentAnswers
                .FirstOrDefaultAsync(sa => sa.ExamAttemptId == examAttemptId && sa.QuestionId == questionId);

            if (studentAnswer != null)
            {
                if (selectedChoiceIds.Count == 1)
                {
                    studentAnswer.SelectedChoiceId = selectedChoiceIds.First();
                    studentAnswer.SelectedChoiceIds = null;
                }
                else if (selectedChoiceIds.Count > 1)
                {
                    studentAnswer.SelectedChoiceId = null;
                    studentAnswer.SelectedChoiceIds = JsonSerializer.Serialize(selectedChoiceIds);
                }
                else
                {
                    studentAnswer.SelectedChoiceId = null;
                    studentAnswer.SelectedChoiceIds = null;
                }

                studentAnswer.IsMarkedForReview = isMarkedForReview;
                studentAnswer.IsCompleted = selectedChoiceIds.Any();
                studentAnswer.AnsweredAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        public async Task<ExamAttempt> SubmitExamAsync(int examAttemptId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var examAttempt = await _context.ExamAttempts
                .Include(ea => ea.Exam)
                .Include(ea => ea.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                        .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(ea => ea.Id == examAttemptId);

            if (examAttempt == null)
                throw new ArgumentException("Exam attempt not found.");

            if (examAttempt.Status != ExamAttemptStatus.InProgress)
                throw new InvalidOperationException("Exam attempt is not in progress.");

            // Calculate score
            examAttempt.TotalScore = await CalculateScoreAsync(examAttempt);
            examAttempt.SubmittedAt = DateTime.UtcNow;
            examAttempt.Status = ExamAttemptStatus.Completed;

            await _context.SaveChangesAsync();

            return examAttempt;
        }

        public async Task<ExamAttempt> GetExamResultAsync(int examAttemptId, string studentId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamAttempts
                .Include(ea => ea.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ea => ea.Student)
                .Include(ea => ea.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                        .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(ea => ea.Id == examAttemptId && 
                                         ea.StudentId == studentId && 
                                         ea.Status == ExamAttemptStatus.Completed);
        }

        public async Task<List<Question>> GetShuffledQuestionsAsync(int examId, bool shuffle)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await GetShuffledQuestionsAsyncInternal(_context, examId, shuffle);
        }

        private async Task<List<Question>> GetShuffledQuestionsAsyncInternal(ApplicationDbContext _context, int examId, bool shuffle)
        {
            var questions = await _context.Questions
                .Include(q => q.Choices)
                .Where(q => q.ExamId == examId)
                .OrderBy(q => q.Order)
                .ToListAsync();

            if (shuffle)
            {
                var random = new Random();
                questions = questions.OrderBy(q => random.Next()).ToList();
            }

            return questions;
        }

        public async Task<decimal> CalculateScoreAsync(ExamAttempt examAttempt)
        {
            decimal totalPointsEarned = 0;
            decimal totalPointsPossible = 0;

            foreach (var studentAnswer in examAttempt.StudentAnswers)
            {
                var question = studentAnswer.Question;
                totalPointsPossible += question.Points;

                if (!studentAnswer.IsCompleted) continue;

                var correctChoices = question.Choices.Where(c => c.IsCorrect).ToList();
                var selectedChoiceIds = GetSelectedChoiceIds(studentAnswer);

                decimal pointsEarned = 0;

                switch (examAttempt.Exam.ScoringType)
                {
                    case ScoringType.AllOrNothing:
                        var correctIds = correctChoices.Select(c => c.Id).OrderBy(id => id).ToList();
                        var selectedIds = selectedChoiceIds.OrderBy(id => id).ToList();
                        
                        if (correctIds.SequenceEqual(selectedIds))
                        {
                            pointsEarned = question.Points;
                        }
                        break;

                    case ScoringType.PartialWithPenalty:
                        var correctSelected = selectedChoiceIds.Count(id => correctChoices.Any(c => c.Id == id));
                        var incorrectSelected = selectedChoiceIds.Count(id => !correctChoices.Any(c => c.Id == id));
                        
                        var correctRatio = correctChoices.Count > 0 ? (decimal)correctSelected / correctChoices.Count : 0;
                        var incorrectRatio = question.Choices.Count(c => !c.IsCorrect) > 0 ? 
                            (decimal)incorrectSelected / question.Choices.Count(c => !c.IsCorrect) : 0;
                        
                        pointsEarned = question.Points * (correctRatio - (incorrectRatio * examAttempt.Exam.PenaltyFactor));
                        pointsEarned = Math.Max(0, pointsEarned); // Don't allow negative points
                        break;

                    case ScoringType.SingleCorrect:
                        if (selectedChoiceIds.Count == 1 && correctChoices.Any(c => c.Id == selectedChoiceIds.First()))
                        {
                            pointsEarned = question.Points;
                        }
                        break;
                }

                studentAnswer.PointsEarned = pointsEarned;
                totalPointsEarned += pointsEarned;
            }

            // Return percentage (0-100) instead of raw points
            return totalPointsPossible > 0 ? (totalPointsEarned / totalPointsPossible) * 100 : 0;
        }

        public async Task<string> GenerateExamCodeAsync(int examId, DateTime expiresAt, int? maxUses, string createdByUserId, string? description)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            string code;
            bool codeExists;

            do
            {
                code = GenerateRandomCode();
                codeExists = await _context.ExamCodes.AnyAsync(ec => ec.Code == code);
            } while (codeExists);

            var examCode = new ExamCode
            {
                Code = code,
                ExamId = examId,
                CreatedByUserId = createdByUserId,
                ExpiresAt = expiresAt,
                MaxUses = maxUses,
                Description = description,
                IsActive = true,
                TimesUsed = 0
            };

            _context.ExamCodes.Add(examCode);
            await _context.SaveChangesAsync();

            return code;
        }

        public async Task<List<ExamCode>> GetExamCodesAsync(int examId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            return await _context.ExamCodes
                .Include(ec => ec.CreatedBy)
                .Where(ec => ec.ExamId == examId)
                .OrderByDescending(ec => ec.CreatedAt)
                .ToListAsync();
        }

        public async Task<ExamCode?> GetValidExamCodeAsync(string code)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var examCode = await _context.ExamCodes
                .Include(ec => ec.Exam)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(ec => ec.Code == code);

            if (examCode == null) return null;
            if (!examCode.IsActive) return null;
            if (examCode.ExpiresAt < DateTime.UtcNow) return null;
            if (examCode.MaxUses.HasValue && examCode.TimesUsed >= examCode.MaxUses.Value) return null;

            return examCode;
        }

        public async Task<bool> UpdateExamAsync(Exam exam)
        {
            try
            {
                await using var _context = await _contextFactory.CreateDbContextAsync();
                _context.Exams.Update(exam);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exam {ExamId}", exam.Id);
                return false;
            }
        }

        private List<int> GetSelectedChoiceIds(StudentAnswer studentAnswer)
        {
            if (studentAnswer.SelectedChoiceId.HasValue)
            {
                return new List<int> { studentAnswer.SelectedChoiceId.Value };
            }

            if (!string.IsNullOrEmpty(studentAnswer.SelectedChoiceIds))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<int>>(studentAnswer.SelectedChoiceIds) ?? new List<int>();
                }
                catch
                {
                    return new List<int>();
                }
            }

            return new List<int>();
        }

        private static string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new char[8];

            for (int i = 0; i < code.Length; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }

            return new string(code);
        }
    }
}
