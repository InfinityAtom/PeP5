using System.ComponentModel.DataAnnotations;

namespace PeP.Models
{
    public class ExamAppAuthorization
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        /// <summary>
        /// The regular exam code ID (nullable for programming exams)
        /// </summary>
        public int? ExamCodeId { get; set; }

        /// <summary>
        /// The programming exam code ID (nullable for regular exams)
        /// </summary>
        public int? ProgrammingExamCodeId { get; set; }

        [Required]
        [StringLength(200)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        [Required]
        public string AuthorizedByTeacherId { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this authorization is for a programming exam
        /// </summary>
        public bool IsProgrammingExam { get; set; }

        public ApplicationUser Student { get; set; } = null!;

        public ExamCode? ExamCode { get; set; }
        
        public ProgrammingExamCode? ProgrammingExamCode { get; set; }
    }

    public class ExamAppLaunchSession
    {
        public int Id { get; set; }

        /// <summary>
        /// The regular exam attempt ID (nullable for programming exams)
        /// </summary>
        public int? ExamAttemptId { get; set; }

        /// <summary>
        /// The programming exam attempt ID (nullable for regular exams)
        /// </summary>
        public int? ProgrammingExamAttemptId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Indicates whether this session is for a programming exam
        /// </summary>
        public bool IsProgrammingExam { get; set; }

        public ExamAttempt? ExamAttempt { get; set; }

        public ProgrammingExamAttempt? ProgrammingExamAttempt { get; set; }

        public ApplicationUser Student { get; set; } = null!;
    }
}

