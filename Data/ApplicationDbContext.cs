using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PeP.Models;

namespace PeP.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionChoice> QuestionChoices { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<ExamCode> ExamCodes { get; set; }
        public DbSet<ExamAppAuthorization> ExamAppAuthorizations { get; set; }
        public DbSet<ExamAppLaunchSession> ExamAppLaunchSessions { get; set; }
        public DbSet<PlatformSetting> PlatformSettings { get; set; }

        // Programming Exam entities
        public DbSet<ProgrammingExam> ProgrammingExams { get; set; }
        public DbSet<ProgrammingTask> ProgrammingTasks { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<ProjectFile> ProjectFiles { get; set; }
        public DbSet<ProgrammingExamAttempt> ProgrammingExamAttempts { get; set; }
        public DbSet<StudentProjectFile> StudentProjectFiles { get; set; }
        public DbSet<TaskProgress> TaskProgresses { get; set; }
        public DbSet<TestCaseResult> TestCaseResults { get; set; }
        public DbSet<CodeSubmission> CodeSubmissions { get; set; }
        public DbSet<ConsoleHistory> ConsoleHistories { get; set; }
        public DbSet<ProgrammingExamCode> ProgrammingExamCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Exams)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionChoice>()
                .HasOne(qc => qc.Question)
                .WithMany(q => q.Choices)
                .HasForeignKey(qc => qc.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(ea => ea.Student)
                .WithMany()
                .HasForeignKey(ea => ea.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(ea => ea.Exam)
                .WithMany(e => e.ExamAttempts)
                .HasForeignKey(ea => ea.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.ExamAttempt)
                .WithMany(ea => ea.StudentAnswers)
                .HasForeignKey(sa => sa.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany()
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.SelectedChoice)
                .WithMany()
                .HasForeignKey(sa => sa.SelectedChoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamCode>()
                .HasOne(ec => ec.Exam)
                .WithMany(e => e.ExamCodes)
                .HasForeignKey(ec => ec.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamCode>()
                .HasOne(ec => ec.CreatedBy)
                .WithMany()
                .HasForeignKey(ec => ec.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAppAuthorization>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAppAuthorization>()
                .HasOne(a => a.ExamCode)
                .WithMany()
                .HasForeignKey(a => a.ExamCodeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<ExamAppAuthorization>()
                .HasOne(a => a.ProgrammingExamCode)
                .WithMany()
                .HasForeignKey(a => a.ProgrammingExamCodeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<ExamAppLaunchSession>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAppLaunchSession>()
                .HasOne(s => s.ExamAttempt)
                .WithMany()
                .HasForeignKey(s => s.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<ExamAppLaunchSession>()
                .HasOne(s => s.ProgrammingExamAttempt)
                .WithMany()
                .HasForeignKey(s => s.ProgrammingExamAttemptId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Configure decimal precision
            modelBuilder.Entity<Exam>()
                .Property(e => e.TotalPoints)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Question>()
                .Property(q => q.Points)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ExamAttempt>()
                .Property(ea => ea.TotalScore)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StudentAnswer>()
                .Property(sa => sa.PointsEarned)
                .HasColumnType("decimal(18,2)");

            // Configure indexes
            modelBuilder.Entity<ExamCode>()
                .HasIndex(ec => ec.Code)
                .IsUnique();

            modelBuilder.Entity<ExamCode>()
                .HasIndex(ec => new { ec.Code, ec.ExpiresAt });

            modelBuilder.Entity<ExamAppAuthorization>()
                .HasIndex(a => a.TokenHash)
                .IsUnique();

            modelBuilder.Entity<ExamAppAuthorization>()
                .HasIndex(a => new { a.StudentId, a.ExpiresAt });

            modelBuilder.Entity<ExamAppLaunchSession>()
                .HasIndex(s => s.TokenHash)
                .IsUnique();

            modelBuilder.Entity<ExamAppLaunchSession>()
                .HasIndex(s => new { s.ExamAttemptId, s.ExpiresAt });

            // Seed default platform settings
            modelBuilder.Entity<PlatformSetting>().HasData(
                new PlatformSetting { Id = 1, Key = "PlatformName", Value = "PeP - Programming Examination Platform" },
                new PlatformSetting { Id = 2, Key = "Version", Value = "5.0" },
                new PlatformSetting { Id = 3, Key = "Company", Value = "Infinity Atom" },
                new PlatformSetting { Id = 4, Key = "Copyright", Value = "Copyright 2021 - 2025" },
                new PlatformSetting { Id = 5, Key = "OpenAIApiKey", Value = "" },
                new PlatformSetting { Id = 6, Key = "DefaultExamDurationMinutes", Value = "120" },
                new PlatformSetting { Id = 7, Key = "EmailServer", Value = "" },
                new PlatformSetting { Id = 8, Key = "EmailPort", Value = "587" },
                new PlatformSetting { Id = 9, Key = "EmailUsername", Value = "" },
                new PlatformSetting { Id = 10, Key = "EmailPassword", Value = "" }
            );

            // ===== Programming Exam Configurations =====

            // ProgrammingExam
            modelBuilder.Entity<ProgrammingExam>()
                .HasOne(pe => pe.Course)
                .WithMany()
                .HasForeignKey(pe => pe.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgrammingExam>()
                .HasOne(pe => pe.CreatedBy)
                .WithMany()
                .HasForeignKey(pe => pe.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProgrammingExam>()
                .Property(pe => pe.TotalPoints)
                .HasColumnType("decimal(18,2)");

            // ProgrammingTask
            modelBuilder.Entity<ProgrammingTask>()
                .HasOne(pt => pt.ProgrammingExam)
                .WithMany(pe => pe.Tasks)
                .HasForeignKey(pt => pt.ProgrammingExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgrammingTask>()
                .Property(pt => pt.Points)
                .HasColumnType("decimal(18,2)");

            // TestCase
            modelBuilder.Entity<TestCase>()
                .HasOne(tc => tc.ProgrammingTask)
                .WithMany(pt => pt.TestCases)
                .HasForeignKey(tc => tc.ProgrammingTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestCase>()
                .Property(tc => tc.Points)
                .HasColumnType("decimal(18,2)");

            // ProjectFile
            modelBuilder.Entity<ProjectFile>()
                .HasOne(pf => pf.ProgrammingExam)
                .WithMany(pe => pe.StarterFiles)
                .HasForeignKey(pf => pf.ProgrammingExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProgrammingExamAttempt
            modelBuilder.Entity<ProgrammingExamAttempt>()
                .HasOne(pea => pea.Student)
                .WithMany()
                .HasForeignKey(pea => pea.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgrammingExamAttempt>()
                .HasOne(pea => pea.ProgrammingExam)
                .WithMany(pe => pe.Attempts)
                .HasForeignKey(pea => pea.ProgrammingExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgrammingExamAttempt>()
                .Property(pea => pea.TotalScore)
                .HasColumnType("decimal(18,2)");

            // StudentProjectFile
            modelBuilder.Entity<StudentProjectFile>()
                .HasOne(spf => spf.ProgrammingExamAttempt)
                .WithMany(pea => pea.StudentFiles)
                .HasForeignKey(spf => spf.ProgrammingExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskProgress
            modelBuilder.Entity<TaskProgress>()
                .HasOne(tp => tp.ProgrammingExamAttempt)
                .WithMany(pea => pea.TaskProgress)
                .HasForeignKey(tp => tp.ProgrammingExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskProgress>()
                .HasOne(tp => tp.ProgrammingTask)
                .WithMany()
                .HasForeignKey(tp => tp.ProgrammingTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskProgress>()
                .Property(tp => tp.PointsEarned)
                .HasColumnType("decimal(18,2)");

            // TestCaseResult
            modelBuilder.Entity<TestCaseResult>()
                .HasOne(tcr => tcr.TaskProgress)
                .WithMany(tp => tp.TestCaseResults)
                .HasForeignKey(tcr => tcr.TaskProgressId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestCaseResult>()
                .HasOne(tcr => tcr.TestCase)
                .WithMany()
                .HasForeignKey(tcr => tcr.TestCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // CodeSubmission
            modelBuilder.Entity<CodeSubmission>()
                .HasOne(cs => cs.ProgrammingExamAttempt)
                .WithMany(pea => pea.Submissions)
                .HasForeignKey(cs => cs.ProgrammingExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // ConsoleHistory
            modelBuilder.Entity<ConsoleHistory>()
                .HasOne(ch => ch.ProgrammingExamAttempt)
                .WithMany(pea => pea.ConsoleHistory)
                .HasForeignKey(ch => ch.ProgrammingExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProgrammingExamCode
            modelBuilder.Entity<ProgrammingExamCode>()
                .HasOne(pec => pec.ProgrammingExam)
                .WithMany(pe => pe.ExamCodes)
                .HasForeignKey(pec => pec.ProgrammingExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgrammingExamCode>()
                .HasOne(pec => pec.CreatedBy)
                .WithMany()
                .HasForeignKey(pec => pec.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProgrammingExamCode>()
                .HasIndex(pec => pec.Code)
                .IsUnique();
        }
    }
}
