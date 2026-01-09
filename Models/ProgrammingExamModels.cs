using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PeP.Models
{
    /// <summary>
    /// Represents a programming exam with project-based tasks
    /// </summary>
    public class ProgrammingExam
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Exam Title")]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(2000)]
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

        [Display(Name = "Programming Language")]
        public ProgrammingLanguage Language { get; set; } = ProgrammingLanguage.Java;

        [Display(Name = "Project Name")]
        [StringLength(100)]
        public string ProjectName { get; set; } = string.Empty;

        [Display(Name = "Total Points")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPoints { get; set; }

        [Display(Name = "Allow Console Input")]
        public bool AllowConsoleInput { get; set; } = true;

        [Display(Name = "Allow File Upload")]
        public bool AllowFileUpload { get; set; } = false;

        [Display(Name = "Show Solution Explorer")]
        public bool ShowSolutionExplorer { get; set; } = true;

        [Display(Name = "Auto Save Interval (Seconds)")]
        public int AutoSaveIntervalSeconds { get; set; } = 30;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Course Course { get; set; } = null!;
        public ApplicationUser CreatedBy { get; set; } = null!;
        public ICollection<ProgrammingTask> Tasks { get; set; } = new List<ProgrammingTask>();
        public ICollection<ProjectFile> StarterFiles { get; set; } = new List<ProjectFile>();
        public ICollection<ProgrammingExamAttempt> Attempts { get; set; } = new List<ProgrammingExamAttempt>();
        public ICollection<ProgrammingExamCode> ExamCodes { get; set; } = new List<ProgrammingExamCode>();
    }

    public enum ProgrammingLanguage
    {
        [Display(Name = "Java")]
        Java = 0,
        [Display(Name = "Python")]
        Python = 1,
        [Display(Name = "C#")]
        CSharp = 2,
        [Display(Name = "C++")]
        CPlusPlus = 3,
        [Display(Name = "JavaScript")]
        JavaScript = 4,
        [Display(Name = "TypeScript")]
        TypeScript = 5,
        [Display(Name = "C")]
        C = 6,
        [Display(Name = "SQL")]
        SQL = 7
    }

    /// <summary>
    /// Represents a task/problem within a programming exam
    /// </summary>
    public class ProgrammingTask
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammingExamId { get; set; }

        [Required]
        [Display(Name = "Task Title")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Instructions")]
        public string Instructions { get; set; } = string.Empty;

        [Display(Name = "Points")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Points { get; set; } = 10.0m;

        [Display(Name = "Order")]
        public int Order { get; set; }

        [Display(Name = "Hint")]
        [StringLength(1000)]
        public string? Hint { get; set; }

        [Display(Name = "Target Files")]
        [StringLength(500)]
        public string? TargetFiles { get; set; } // Comma-separated list of files to focus on for this task

        // Navigation properties
        public ProgrammingExam ProgrammingExam { get; set; } = null!;
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    }

    /// <summary>
    /// Test cases for automated code evaluation
    /// </summary>
    public class TestCase
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammingTaskId { get; set; }

        [Required]
        [Display(Name = "Test Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Input")]
        public string? Input { get; set; }

        [Display(Name = "Expected Output")]
        public string? ExpectedOutput { get; set; }

        [Display(Name = "Is Hidden")]
        public bool IsHidden { get; set; } = false; // Hidden tests not shown to students

        [Display(Name = "Points")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Points { get; set; } = 1.0m;

        [Display(Name = "Timeout (Seconds)")]
        public int TimeoutSeconds { get; set; } = 10;

        [Display(Name = "Order")]
        public int Order { get; set; }

        // Navigation properties
        public ProgrammingTask ProgrammingTask { get; set; } = null!;
    }

    /// <summary>
    /// Starter/template files for a programming exam project
    /// </summary>
    public class ProjectFile
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammingExamId { get; set; }

        [Required]
        [Display(Name = "File Path")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty; // e.g., "src/Main.java", "data/matches.csv"

        [Required]
        [Display(Name = "File Name")]
        [StringLength(100)]
        public string FileName { get; set; } = string.Empty;

        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Is Read Only")]
        public bool IsReadOnly { get; set; } = false;

        [Display(Name = "Is Entry Point")]
        public bool IsEntryPoint { get; set; } = false; // Main file to run

        [Display(Name = "File Type")]
        public ProjectFileType FileType { get; set; } = ProjectFileType.Source;

        [Display(Name = "Order")]
        public int Order { get; set; }

        // Navigation properties
        public ProgrammingExam ProgrammingExam { get; set; } = null!;
    }

    public enum ProjectFileType
    {
        [Display(Name = "Source Code")]
        Source = 0,
        [Display(Name = "Data File")]
        Data = 1,
        [Display(Name = "Configuration")]
        Config = 2,
        [Display(Name = "Resource")]
        Resource = 3,
        [Display(Name = "Test")]
        Test = 4
    }

    /// <summary>
    /// Student attempt at a programming exam
    /// </summary>
    public class ProgrammingExamAttempt
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public int ProgrammingExamId { get; set; }

        [Display(Name = "Started At")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Submitted At")]
        public DateTime? SubmittedAt { get; set; }

        [Display(Name = "Last Activity")]
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        [Display(Name = "Total Score")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalScore { get; set; }

        [Display(Name = "Status")]
        public ProgrammingExamStatus Status { get; set; } = ProgrammingExamStatus.InProgress;

        [Display(Name = "Exam Code Used")]
        [StringLength(20)]
        public string? ExamCodeUsed { get; set; }

        // AI Evaluation fields
        [Display(Name = "AI Evaluation Completed")]
        public bool AIEvaluationCompleted { get; set; } = false;

        [Display(Name = "AI Evaluation Started At")]
        public DateTime? AIEvaluationStartedAt { get; set; }

        [Display(Name = "AI Evaluation Completed At")]
        public DateTime? AIEvaluationCompletedAt { get; set; }

        [Display(Name = "Request Teacher Reevaluation")]
        public bool RequestTeacherReevaluation { get; set; } = false;

        [Display(Name = "Teacher Reevaluation Requested At")]
        public DateTime? TeacherReevaluationRequestedAt { get; set; }

        [Display(Name = "Teacher Reevaluation Completed")]
        public bool TeacherReevaluationCompleted { get; set; } = false;

        [Display(Name = "Teacher Reevaluation Completed At")]
        public DateTime? TeacherReevaluationCompletedAt { get; set; }

        [Display(Name = "Teacher Reevaluation Notes")]
        [StringLength(2000)]
        public string? TeacherReevaluationNotes { get; set; }

        // Navigation properties
        public ApplicationUser Student { get; set; } = null!;
        public ProgrammingExam ProgrammingExam { get; set; } = null!;
        public ICollection<StudentProjectFile> StudentFiles { get; set; } = new List<StudentProjectFile>();
        public ICollection<TaskProgress> TaskProgress { get; set; } = new List<TaskProgress>();
        public ICollection<CodeSubmission> Submissions { get; set; } = new List<CodeSubmission>();
        public ICollection<ConsoleHistory> ConsoleHistory { get; set; } = new List<ConsoleHistory>();
    }

    public enum ProgrammingExamStatus
    {
        [Display(Name = "In Progress")]
        InProgress = 0,
        [Display(Name = "Completed")]
        Completed = 1,
        [Display(Name = "Abandoned")]
        Abandoned = 2,
        [Display(Name = "Time Expired")]
        TimeExpired = 3,
        [Display(Name = "Grading")]
        Grading = 4
    }

    /// <summary>
    /// Student's modified version of project files
    /// </summary>
    public class StudentProjectFile
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammingExamAttemptId { get; set; }

        [Required]
        [Display(Name = "File Path")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File Name")]
        [StringLength(100)]
        public string FileName { get; set; } = string.Empty;

        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ProgrammingExamAttempt ProgrammingExamAttempt { get; set; } = null!;
    }

    /// <summary>
    /// Progress on individual tasks within an attempt
    /// </summary>
    public class TaskProgress
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammingExamAttemptId { get; set; }

        [Required]
        public int ProgrammingTaskId { get; set; }

        [Display(Name = "Status")]
        public ProgrammingTaskStatus Status { get; set; } = ProgrammingTaskStatus.NotStarted;

        [Display(Name = "Is Marked for Review")]
        public bool IsMarkedForReview { get; set; }

        [Display(Name = "Requires Feedback")]
        public bool RequiresFeedback { get; set; }

        [Display(Name = "Points Earned")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PointsEarned { get; set; }

        [Display(Name = "Teacher Notes")]
        [StringLength(2000)]
        public string? TeacherNotes { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // AI Evaluation Fields
        [Display(Name = "AI Feedback")]
        public string? AIFeedback { get; set; }

        [Display(Name = "AI Score")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AIScore { get; set; }

        [Display(Name = "AI Evaluated At")]
        public DateTime? AIEvaluatedAt { get; set; }

        [Display(Name = "Code Quality Score")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CodeQualityScore { get; set; }

        [Display(Name = "Correctness Score")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CorrectnessScore { get; set; }

        [Display(Name = "Efficiency Score")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EfficiencyScore { get; set; }

        [Display(Name = "Completion Percentage")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CompletionPercentage { get; set; }

        [Display(Name = "AI Code Snippets")]
        public string? AICodeSnippets { get; set; }

        [Display(Name = "AI Solution Suggestion")]
        public string? AISolutionSuggestion { get; set; }

        // Navigation properties
        public ProgrammingExamAttempt ProgrammingExamAttempt { get; set; } = null!;
        public ProgrammingTask ProgrammingTask { get; set; } = null!;
        public ICollection<TestCaseResult> TestCaseResults { get; set; } = new List<TestCaseResult>();
    }

    public enum ProgrammingTaskStatus
    {
        [Display(Name = "Not Started")]
        NotStarted = 0,
        [Display(Name = "In Progress")]
        InProgress = 1,
        [Display(Name = "Completed")]
        Completed = 2,
        [Display(Name = "Marked for Review")]
        MarkedForReview = 3,
        [Display(Name = "Needs Feedback")]
        NeedsFeedback = 4
    }

    /// <summary>
    /// Results of running test cases
    /// </summary>
    public class TestCaseResult
    {
        public int Id { get; set; }

        [Required]
        public int TaskProgressId { get; set; }

        [Required]
        public int TestCaseId { get; set; }

        [Display(Name = "Passed")]
        public bool Passed { get; set; }

        [Display(Name = "Actual Output")]
        public string? ActualOutput { get; set; }

        [Display(Name = "Error Message")]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Execution Time (ms)")]
        public int ExecutionTimeMs { get; set; }

        [Display(Name = "Run At")]
        public DateTime RunAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public TaskProgress TaskProgress { get; set; } = null!;
        public TestCase TestCase { get; set; } = null!;
    }

    /// <summary>
    /// Code submissions/snapshots during the exam
    /// </summary>
    public class CodeSubmission
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammingExamAttemptId { get; set; }

        [Display(Name = "Submission Type")]
        public SubmissionType Type { get; set; } = SubmissionType.AutoSave;

        [Display(Name = "Files Snapshot")]
        public string FilesSnapshot { get; set; } = string.Empty; // JSON of all files

        [Display(Name = "Submitted At")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ProgrammingExamAttempt ProgrammingExamAttempt { get; set; } = null!;
    }

    public enum SubmissionType
    {
        [Display(Name = "Auto Save")]
        AutoSave = 0,
        [Display(Name = "Manual Save")]
        ManualSave = 1,
        [Display(Name = "Run Code")]
        RunCode = 2,
        [Display(Name = "Final Submission")]
        FinalSubmission = 3
    }

    /// <summary>
    /// Console input/output history
    /// </summary>
    public class ConsoleHistory
    {
        public int Id { get; set; }

        [Required]
        public int ProgrammingExamAttemptId { get; set; }

        [Display(Name = "Type")]
        public ConsoleEntryType Type { get; set; }

        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ProgrammingExamAttempt ProgrammingExamAttempt { get; set; } = null!;
    }

    public enum ConsoleEntryType
    {
        [Display(Name = "Input")]
        Input = 0,
        [Display(Name = "Output")]
        Output = 1,
        [Display(Name = "Error")]
        Error = 2,
        [Display(Name = "System")]
        System = 3
    }

    /// <summary>
    /// Exam codes specific to programming exams
    /// </summary>
    public class ProgrammingExamCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public int ProgrammingExamId { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public int? MaxUses { get; set; }

        public int TimesUsed { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ProgrammingExam ProgrammingExam { get; set; } = null!;
        public ApplicationUser CreatedBy { get; set; } = null!;
    }
}
