using System.ComponentModel.DataAnnotations;

namespace PeP.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;

        // Student-specific
        [Display(Name = "Student ID")]
        public string? StudentId { get; set; }

        // Teacher-specific
        [Display(Name = "Department")]
        public string? Department { get; set; }
    }

    public class ExamAccessViewModel
    {
        [Required]
        [Display(Name = "Exam Code")]
        [StringLength(20)]
        public string ExamCode { get; set; } = string.Empty;
    }

    public class CreateExamViewModel
    {
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

        [Display(Name = "Duration (Minutes)")]
        [Range(1, 600)]
        public int DurationMinutes { get; set; } = 120;

        [Display(Name = "Number of Questions")]
        [Range(1, 200)]
        public int NumberOfQuestions { get; set; } = 10;

        [Display(Name = "Points per Question")]
        [Range(0.1, 100)]
        public decimal PointsPerQuestion { get; set; } = 1.0m;

        [Display(Name = "Scoring Type")]
        public string ScoringType { get; set; } = "AllOrNothing";

        [Display(Name = "Penalty Factor")]
        [Range(0, 1)]
        public decimal PenaltyFactor { get; set; } = 0.25m;

        [Display(Name = "Shuffle Questions")]
        public bool ShuffleQuestions { get; set; } = true;

        [Display(Name = "Shuffle Choices")]
        public bool ShuffleChoices { get; set; } = true;

        [Display(Name = "Show Results Immediately")]
        public bool ShowResultsImmediately { get; set; } = true;

        // AI Generation properties
        [Display(Name = "Use AI to Generate Questions")]
        public bool UseAI { get; set; }

        [Display(Name = "AI Prompt")]
        [StringLength(2000)]
        public string? AIPrompt { get; set; }

        [Display(Name = "Difficulty Level")]
        public string DifficultyLevel { get; set; } = "Medium";

        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "MultipleChoice";

        // Manual Questions
        public List<ManualQuestionViewModel> ManualQuestions { get; set; } = new();
    }

    public class ManualQuestionViewModel
    {
        public int TempId { get; set; } // Temporary ID for UI tracking
        
        [Required]
        [Display(Name = "Question Text")]
        [StringLength(2000)]
        public string QuestionText { get; set; } = string.Empty;

        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "MultipleChoice";

        [Display(Name = "Points")]
        [Range(0.1, 100)]
        public decimal Points { get; set; } = 1.0m;

        public List<ManualChoiceViewModel> Choices { get; set; } = new()
        {
            new() { TempId = 1, ChoiceText = "", IsCorrect = true },
            new() { TempId = 2, ChoiceText = "", IsCorrect = false },
            new() { TempId = 3, ChoiceText = "", IsCorrect = false },
            new() { TempId = 4, ChoiceText = "", IsCorrect = false }
        };
    }

    public class ManualChoiceViewModel
    {
        public int TempId { get; set; }
        
        [Required]
        [Display(Name = "Choice Text")]
        [StringLength(500)]
        public string ChoiceText { get; set; } = string.Empty;

        [Display(Name = "Is Correct")]
        public bool IsCorrect { get; set; }
    }

    public class GenerateExamCodeViewModel
    {
        [Required]
        [Display(Name = "Exam")]
        public int ExamId { get; set; }

        [Required]
        [Display(Name = "Expires At")]
        [DataType(DataType.DateTime)]
        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Maximum Uses")]
        [Range(1, 1000)]
        public int? MaxUses { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class ExamResultViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Percentage { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<QuestionResultViewModel> Questions { get; set; } = new();
    }

    public class QuestionResultViewModel
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public decimal PointsEarned { get; set; }
        public List<ChoiceResultViewModel> Choices { get; set; } = new();
        public List<int> SelectedChoiceIds { get; set; } = new();
        public List<int> CorrectChoiceIds { get; set; } = new();
        public bool IsMarkedForReview { get; set; }
    }

    public class ChoiceResultViewModel
    {
        public int Id { get; set; }
        public string ChoiceText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public bool WasSelected { get; set; }
    }

    public class BulkStudentImportViewModel
    {
        [Required]
        [Display(Name = "Excel File")]
        public IFormFile ExcelFile { get; set; } = null!;

        [Display(Name = "Send Welcome Email")]
        public bool SendWelcomeEmail { get; set; } = true;
    }

    public class StudentImportModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentId { get; set; }
    }
}