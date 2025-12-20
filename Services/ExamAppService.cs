using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PeP.Data;
using PeP.Models;
using System.Security.Cryptography;
using System.Text;

namespace PeP.Services
{
    public record ExamAppExamInfo(
        int ExamId,
        int ExamCodeId,
        string ExamTitle,
        string? CourseName,
        int DurationMinutes,
        string TeacherName);

    public record ExamAppAuthorizeResult(
        bool Success,
        string? Error,
        string? AuthorizationToken,
        DateTime? ExpiresAtUtc,
        ExamAppExamInfo? Exam);

    public record ExamAppStartResult(
        bool Success,
        string? Error,
        int? AttemptId,
        string? LaunchToken,
        DateTime? ExpiresAtUtc);

    public interface IExamAppService
    {
        Task<ExamAppExamInfo?> GetExamInfoForCodeAsync(string code);
        Task<ExamAppAuthorizeResult> AuthorizeAsync(string studentId, string code, string teacherPassword);
        Task<ExamAppStartResult> StartAsync(string studentId, string authorizationToken);
        Task<bool> ValidateLaunchTokenAsync(int attemptId, string studentId, string launchToken);
    }

    public class ExamAppService : IExamAppService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IExamService _examService;
        private readonly ILogger<ExamAppService> _logger;

        public ExamAppService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IExamService examService,
            ILogger<ExamAppService> logger)
        {
            _contextFactory = contextFactory;
            _userManager = userManager;
            _signInManager = signInManager;
            _examService = examService;
            _logger = logger;
        }

        public async Task<ExamAppExamInfo?> GetExamInfoForCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var normalizedCode = code.Trim().ToUpperInvariant();

            await using var context = await _contextFactory.CreateDbContextAsync();

            var examCode = await context.ExamCodes
                .Include(ec => ec.Exam)
                    .ThenInclude(e => e.Course)
                .Include(ec => ec.Exam)
                    .ThenInclude(e => e.CreatedBy)
                .FirstOrDefaultAsync(ec => ec.Code == normalizedCode);

            if (examCode == null) return null;
            if (!examCode.IsActive) return null;
            if (!examCode.Exam.IsActive) return null;
            if (examCode.ExpiresAt < DateTime.UtcNow) return null;
            if (examCode.MaxUses.HasValue && examCode.TimesUsed >= examCode.MaxUses.Value) return null;

            var teacherName = examCode.Exam.CreatedBy?.FullName;
            if (string.IsNullOrWhiteSpace(teacherName))
            {
                teacherName = examCode.Exam.CreatedBy?.Email ?? "Teacher";
            }

            return new ExamAppExamInfo(
                ExamId: examCode.ExamId,
                ExamCodeId: examCode.Id,
                ExamTitle: examCode.Exam.Title,
                CourseName: examCode.Exam.Course?.Name,
                DurationMinutes: examCode.Exam.DurationMinutes,
                TeacherName: teacherName);
        }

        public async Task<ExamAppAuthorizeResult> AuthorizeAsync(string studentId, string code, string teacherPassword)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return new ExamAppAuthorizeResult(false, "Student is not identified.", null, null, null);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return new ExamAppAuthorizeResult(false, "Exam code is required.", null, null, null);
            }

            if (string.IsNullOrWhiteSpace(teacherPassword))
            {
                return new ExamAppAuthorizeResult(false, "Teacher password is required.", null, null, null);
            }

            var examInfo = await GetExamInfoForCodeAsync(code);
            if (examInfo == null)
            {
                return new ExamAppAuthorizeResult(false, "Invalid or expired exam code.", null, null, null);
            }

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var exam = await context.Exams.FirstOrDefaultAsync(e => e.Id == examInfo.ExamId);
                if (exam == null || !exam.IsActive)
                {
                    return new ExamAppAuthorizeResult(false, "Exam is not available.", null, null, null);
                }

                var teacher = await _userManager.FindByIdAsync(exam.CreatedByUserId);
                if (teacher == null)
                {
                    return new ExamAppAuthorizeResult(false, "Teacher account was not found.", null, null, null);
                }

                var passwordResult = await _signInManager.CheckPasswordSignInAsync(teacher, teacherPassword, lockoutOnFailure: true);
                if (!passwordResult.Succeeded)
                {
                    return new ExamAppAuthorizeResult(false, "Teacher authorization failed.", null, null, null);
                }

                var authorizationToken = GenerateToken();
                var authorizationTokenHash = HashToken(authorizationToken);

                var expiresAt = DateTime.UtcNow.AddMinutes(15);

                var authorization = new ExamAppAuthorization
                {
                    StudentId = studentId,
                    ExamCodeId = examInfo.ExamCodeId,
                    TokenHash = authorizationTokenHash,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    AuthorizedByTeacherId = teacher.Id
                };

                context.ExamAppAuthorizations.Add(authorization);
                await context.SaveChangesAsync();

                return new ExamAppAuthorizeResult(true, null, authorizationToken, expiresAt, examInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExamApp authorize failed for student {StudentId}", studentId);
                return new ExamAppAuthorizeResult(false, "Authorization failed due to a server error.", null, null, null);
            }
        }

        public async Task<ExamAppStartResult> StartAsync(string studentId, string authorizationToken)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return new ExamAppStartResult(false, "Student is not identified.", null, null, null);
            }

            if (string.IsNullOrWhiteSpace(authorizationToken))
            {
                return new ExamAppStartResult(false, "Authorization token is required.", null, null, null);
            }

            var tokenHash = HashToken(authorizationToken);

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var authorization = await context.ExamAppAuthorizations
                    .Include(a => a.ExamCode)
                        .ThenInclude(ec => ec.Exam)
                    .FirstOrDefaultAsync(a =>
                        a.StudentId == studentId &&
                        a.TokenHash == tokenHash &&
                        a.UsedAt == null &&
                        a.ExpiresAt > DateTime.UtcNow);

                if (authorization == null)
                {
                    return new ExamAppStartResult(false, "Authorization token is invalid or expired.", null, null, null);
                }

                authorization.UsedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                var examCode = authorization.ExamCode;
                var attempt = await _examService.StartExamAsync(studentId, examCode.ExamId, examCode.Code);

                // Revoke any existing active sessions for this attempt
                var now = DateTime.UtcNow;
                var existingSessions = await context.ExamAppLaunchSessions
                    .Where(s => s.ExamAttemptId == attempt.Id && s.StudentId == studentId && s.RevokedAt == null && s.ExpiresAt > now)
                    .ToListAsync();

                foreach (var session in existingSessions)
                {
                    session.RevokedAt = now;
                }

                var launchToken = GenerateToken();
                var launchTokenHash = HashToken(launchToken);

                var attemptExpiresAt = attempt.StartedAt.AddMinutes(attempt.Exam.DurationMinutes).AddMinutes(10);
                if (attemptExpiresAt < now.AddMinutes(5))
                {
                    attemptExpiresAt = now.AddMinutes(5);
                }

                var launchSession = new ExamAppLaunchSession
                {
                    ExamAttemptId = attempt.Id,
                    StudentId = studentId,
                    TokenHash = launchTokenHash,
                    CreatedAt = now,
                    ExpiresAt = attemptExpiresAt,
                    RevokedAt = null
                };

                context.ExamAppLaunchSessions.Add(launchSession);
                await context.SaveChangesAsync();

                return new ExamAppStartResult(true, null, attempt.Id, launchToken, attemptExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExamApp start failed for student {StudentId}", studentId);
                return new ExamAppStartResult(false, "Start failed due to a server error.", null, null, null);
            }
        }

        public async Task<bool> ValidateLaunchTokenAsync(int attemptId, string studentId, string launchToken)
        {
            if (attemptId <= 0) return false;
            if (string.IsNullOrWhiteSpace(studentId)) return false;
            if (string.IsNullOrWhiteSpace(launchToken)) return false;

            var tokenHash = HashToken(launchToken);

            await using var context = await _contextFactory.CreateDbContextAsync();

            var now = DateTime.UtcNow;
            var session = await context.ExamAppLaunchSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.ExamAttemptId == attemptId &&
                    s.StudentId == studentId &&
                    s.TokenHash == tokenHash &&
                    s.RevokedAt == null &&
                    s.ExpiresAt > now);

            if (session == null) return false;

            var attemptStatus = await context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.Id == attemptId && a.StudentId == studentId)
                .Select(a => a.Status)
                .FirstOrDefaultAsync();

            return attemptStatus == ExamAttemptStatus.InProgress;
        }

        private static string GenerateToken(int numBytes = 32)
        {
            var bytes = RandomNumberGenerator.GetBytes(numBytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }
    }
}

