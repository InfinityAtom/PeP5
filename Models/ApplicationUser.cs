using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PeP.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}".Trim();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;

        // Student-specific properties
        public string? StudentId { get; set; }
        
        // Teacher-specific properties
        public string? Department { get; set; }
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Teacher = "Teacher";
        public const string Student = "Student";
    }
}