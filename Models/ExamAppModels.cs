using System.ComponentModel.DataAnnotations;

namespace PeP.Models
{
    public class ExamAppAuthorization
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public int ExamCodeId { get; set; }

        [Required]
        [StringLength(200)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        [Required]
        public string AuthorizedByTeacherId { get; set; } = string.Empty;

        public ApplicationUser Student { get; set; } = null!;

        public ExamCode ExamCode { get; set; } = null!;
    }

    public class ExamAppLaunchSession
    {
        public int Id { get; set; }

        [Required]
        public int ExamAttemptId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public ExamAttempt ExamAttempt { get; set; } = null!;

        public ApplicationUser Student { get; set; } = null!;
    }
}

