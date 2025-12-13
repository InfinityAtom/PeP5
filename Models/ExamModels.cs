using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PeP.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Course Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Course Code")]
        [StringLength(20)]
        public string? Code { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }

    public class Exam
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Exam Title")]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [Required]
        [Display(Name = "Created By")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [Display(Name = "Duration (Minutes)")]
        [Range(1, 600)]
        public int DurationMinutes { get; set; } = 120;

        [Display(Name = "Total Points")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPoints { get; set; }

        [Display(Name = "Number of Questions")]
        [Range(1, 200)]
        public int NumberOfQuestions { get; set; }

        [Display(Name = "Scoring Type")]
        public ScoringType ScoringType { get; set; } = ScoringType.AllOrNothing;

        [Display(Name = "Penalty Factor")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PenaltyFactor { get; set; } = 0.25m; // 25% penalty for wrong answers

        [Display(Name = "Shuffle Questions")]
        public bool ShuffleQuestions { get; set; } = true;

        [Display(Name = "Shuffle Choices")]
        public bool ShuffleChoices { get; set; } = true;

        [Display(Name = "Show Results Immediately")]
        public bool ShowResultsImmediately { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Course Course { get; set; } = null!;
        public ApplicationUser CreatedBy { get; set; } = null!;
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
        public ICollection<ExamCode> ExamCodes { get; set; } = new List<ExamCode>();
    }

    public enum ScoringType
    {
        [Display(Name = "All or Nothing")]
        AllOrNothing = 0,
        [Display(Name = "Partial Credit with Penalty")]
        PartialWithPenalty = 1,
        [Display(Name = "Single Correct Answer")]
        SingleCorrect = 2
    }

    public class Question
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Exam")]
        public int ExamId { get; set; }

        [Required]
        [Display(Name = "Question Text")]
        [StringLength(2000)]
        public string QuestionText { get; set; } = string.Empty;

        [Display(Name = "Question Type")]
        public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;

        [Display(Name = "Points")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.1, 100)]
        public decimal Points { get; set; } = 1.0m;

        [Display(Name = "Order")]
        public int Order { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Exam Exam { get; set; } = null!;
        public ICollection<QuestionChoice> Choices { get; set; } = new List<QuestionChoice>();
    }

    public enum QuestionType
    {
        [Display(Name = "Multiple Choice (Single Answer)")]
        MultipleChoice = 0,
        [Display(Name = "Multiple Choice (Multiple Answers)")]
        MultipleAnswer = 1
    }

    public class QuestionChoice
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Question")]
        public int QuestionId { get; set; }

        [Required]
        [Display(Name = "Choice Text")]
        [StringLength(1000)]
        public string ChoiceText { get; set; } = string.Empty;

        [Display(Name = "Is Correct")]
        public bool IsCorrect { get; set; }

        [Display(Name = "Order")]
        public int Order { get; set; }

        // Navigation properties
        public Question Question { get; set; } = null!;
    }

    public class ExamAttempt
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Exam")]
        public int ExamId { get; set; }

        [Display(Name = "Started At")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Submitted At")]
        public DateTime? SubmittedAt { get; set; }

        [Display(Name = "Total Score")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalScore { get; set; }

        [Display(Name = "Status")]
        public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;

        [Display(Name = "Exam Code Used")]
        [StringLength(20)]
        public string? ExamCodeUsed { get; set; }

        // Navigation properties
        public ApplicationUser Student { get; set; } = null!;
        public Exam Exam { get; set; } = null!;
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }

    public enum ExamAttemptStatus
    {
        [Display(Name = "In Progress")]
        InProgress = 0,
        [Display(Name = "Completed")]
        Completed = 1,
        [Display(Name = "Abandoned")]
        Abandoned = 2,
        [Display(Name = "Time Expired")]
        TimeExpired = 3
    }

    public class StudentAnswer
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Exam Attempt")]
        public int ExamAttemptId { get; set; }

        [Required]
        [Display(Name = "Question")]
        public int QuestionId { get; set; }

        [Display(Name = "Selected Choice")]
        public int? SelectedChoiceId { get; set; }

        [Display(Name = "Selected Choice IDs")]
        public string? SelectedChoiceIds { get; set; } // JSON array for multiple answers

        [Display(Name = "Points Earned")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PointsEarned { get; set; }

        [Display(Name = "Is Marked for Review")]
        public bool IsMarkedForReview { get; set; }

        [Display(Name = "Is Completed")]
        public bool IsCompleted { get; set; }

        [Display(Name = "Answered At")]
        public DateTime? AnsweredAt { get; set; }

        // Navigation properties
        public ExamAttempt ExamAttempt { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public QuestionChoice? SelectedChoice { get; set; }
    }

    public class ExamCode
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Code")]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Exam")]
        public int ExamId { get; set; }

        [Required]
        [Display(Name = "Created By")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Expires At")]
        public DateTime ExpiresAt { get; set; }

        [Display(Name = "Max Uses")]
        public int? MaxUses { get; set; }

        [Display(Name = "Times Used")]
        public int TimesUsed { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public Exam Exam { get; set; } = null!;
        public ApplicationUser CreatedBy { get; set; } = null!;
    }

    public class PlatformSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}