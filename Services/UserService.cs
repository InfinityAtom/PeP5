using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeP.Data;
using PeP.Models;
using PeP.ViewModels;
using System.Net.Mail;
using System.Net;

namespace PeP.Services
{
    public interface IUserService
    {
        Task<List<ApplicationUser>> GetStudentsAsync();
        Task<List<ApplicationUser>> GetTeachersAsync();
        Task<ApplicationUser?> CreateStudentAsync(RegisterViewModel model, string? generatedPassword = null);
        Task<ApplicationUser?> CreateTeacherAsync(RegisterViewModel model);
        Task<List<StudentImportModel>> ImportStudentsFromExcelAsync(IFormFile file);
        Task<List<ApplicationUser>> BulkCreateStudentsAsync(List<StudentImportModel> students, bool sendWelcomeEmail = true);
        Task<bool> SendWelcomeEmailAsync(ApplicationUser user, string password);
        Task<bool> DeactivateUserAsync(string userId);
        Task<bool> ActivateUserAsync(string userId);
        Task<bool> ToggleUserStatusAsync(string userId);
        Task<List<ImportResult>> ImportStudentsAsync(List<StudentImportData> students);
    }

    public class UserService : IUserService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            UserManager<ApplicationUser> userManager,
            ILogger<UserService> logger)
        {
            _contextFactory = contextFactory;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<List<ApplicationUser>> GetStudentsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var studentIds = await context.UserRoles
                .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == UserRoles.Student)
                .Select(x => x.UserId)
                .ToListAsync();

            return await context.Users
                .Where(u => studentIds.Contains(u.Id))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetTeachersAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var teacherIds = await context.UserRoles
                .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == UserRoles.Teacher)
                .Select(x => x.UserId)
                .ToListAsync();

            return await context.Users
                .Where(u => teacherIds.Contains(u.Id))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<bool> ToggleUserStatusAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.IsActive = !user.IsActive;
                    var result = await _userManager.UpdateAsync(user);
                    return result.Succeeded;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for {UserId}", userId);
                return false;
            }
        }

        public async Task<List<ImportResult>> ImportStudentsAsync(List<StudentImportData> students)
        {
            var results = new List<ImportResult>();

            await using var context = await _contextFactory.CreateDbContextAsync();

            foreach (var student in students)
            {
                var result = new ImportResult { StudentData = student };

                try
                {
                    // Check if user already exists
                    var existingUser = await _userManager.FindByEmailAsync(student.Email);
                    if (existingUser != null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "User with this email already exists";
                        results.Add(result);
                        continue;
                    }

                    // Check if StudentId already exists (if provided)
                    if (!string.IsNullOrEmpty(student.StudentId))
                    {
                        var existingStudentId = await context.Users
                            .AnyAsync(u => u.StudentId == student.StudentId);
                        if (existingStudentId)
                        {
                            result.Success = false;
                            result.ErrorMessage = "Student ID already exists";
                            results.Add(result);
                            continue;
                        }
                    }

                    // Create the user
                    var user = new ApplicationUser
                    {
                        UserName = student.Email,
                        Email = student.Email,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        StudentId = student.StudentId,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var password = "Student123!"; // Default password
                    var createResult = await _userManager.CreateAsync(user, password);

                    if (createResult.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, UserRoles.Student);
                        result.Success = true;
                        
                        // Optionally send welcome email
                        try
                        {
                            await SendWelcomeEmailAsync(user, password);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "Failed to send welcome email to {Email}", user.Email);
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing student {Email}", student.Email);
                    result.Success = false;
                    result.ErrorMessage = "An error occurred during import";
                }

                results.Add(result);
            }

            return results;
        }

        public async Task<ApplicationUser?> CreateStudentAsync(RegisterViewModel model, string? generatedPassword = null)
        {
            var password = generatedPassword ?? model.Password;

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                StudentId = model.StudentId,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, UserRoles.Student);
                return user;
            }

            _logger.LogError("Failed to create student: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return null;
        }

        public async Task<ApplicationUser?> CreateTeacherAsync(RegisterViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Department = model.Department,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, UserRoles.Teacher);
                return user;
            }

            _logger.LogError("Failed to create teacher: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return null;
        }

        public async Task<List<StudentImportModel>> ImportStudentsFromExcelAsync(IFormFile file)
        {
            // This method is not actively used, return empty list
            // In the future, this could be implemented to parse Excel files
            return new List<StudentImportModel>();
        }

        public async Task<List<ApplicationUser>> BulkCreateStudentsAsync(List<StudentImportModel> students, bool sendWelcomeEmail = true)
        {
            var createdUsers = new List<ApplicationUser>();

            await using var context = await _contextFactory.CreateDbContextAsync();

            foreach (var student in students)
            {
                try
                {
                    // Check if user already exists
                    var existingUser = await _userManager.FindByEmailAsync(student.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("User with email {Email} already exists", student.Email);
                        continue;
                    }

                    // Generate random password
                    var password = GenerateRandomPassword();

                    var registerModel = new RegisterViewModel
                    {
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        Email = student.Email,
                        StudentId = student.StudentId,
                        Password = password,
                        Role = UserRoles.Student
                    };

                    var user = await CreateStudentAsync(registerModel, password);
                    if (user != null)
                    {
                        createdUsers.Add(user);

                        if (sendWelcomeEmail)
                        {
                            await SendWelcomeEmailAsync(user, password);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating student {Email}", student.Email);
                }
            }

            return createdUsers;
        }

        public async Task<bool> SendWelcomeEmailAsync(ApplicationUser user, string password)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var emailSettings = await GetEmailSettingsAsync(context);
                if (emailSettings == null) return false;

                var platformName = await GetSettingValueAsync(context, "PlatformName") ?? "PeP - Programming Examination Platform";

                var subject = $"Welcome to {platformName}";
                var body = $@"
                    <h2>Welcome to {platformName}</h2>
                    <p>Hello {user.FirstName} {user.LastName},</p>
                    <p>Your account has been created successfully.</p>
                    <p><strong>Login Details:</strong></p>
                    <p>Email: {user.Email}</p>
                    <p>Password: {password}</p>
                    <p>Please change your password after your first login.</p>
                    <br>
                    <p>Best regards,<br>{platformName} Team</p>
                ";

                using var smtpClient = new SmtpClient(emailSettings.Server, emailSettings.Port);
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password);

                var message = new MailMessage(emailSettings.Username, user.Email, subject, body)
                {
                    IsBodyHtml = true
                };

                await smtpClient.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = false;
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            return false;
        }

        public async Task<bool> ActivateUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = true;
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            return false;
        }

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            var password = new char[12];

            // Ensure at least one of each type
            password[0] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[random.Next(26)]; // Uppercase
            password[1] = "abcdefghijklmnopqrstuvwxyz"[random.Next(26)]; // Lowercase
            password[2] = "0123456789"[random.Next(10)]; // Digit
            password[3] = "!@#$%"[random.Next(5)]; // Special character

            // Fill the rest randomly
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            // Shuffle the password
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }

        private async Task<EmailSettings?> GetEmailSettingsAsync(ApplicationDbContext context)
        {
            var server = await GetSettingValueAsync(context, "EmailServer");
            var portStr = await GetSettingValueAsync(context, "EmailPort");
            var username = await GetSettingValueAsync(context, "EmailUsername");
            var password = await GetSettingValueAsync(context, "EmailPassword");

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            if (!int.TryParse(portStr, out int port))
                port = 587;

            return new EmailSettings
            {
                Server = server,
                Port = port,
                Username = username,
                Password = password
            };
        }

        private async Task<string?> GetSettingValueAsync(ApplicationDbContext context, string key)
        {
            var setting = await context.PlatformSettings
                .FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        private class EmailSettings
        {
            public string Server { get; set; } = string.Empty;
            public int Port { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }

    // Supporting classes for the import functionality
    public class StudentImportData
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ImportResult
    {
        public StudentImportData StudentData { get; set; } = null!;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}