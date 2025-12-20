using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PeP.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeP.Models;
using PeP.Services;
using Radzen;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Use DbContextFactory to create short-lived DbContext instances and avoid concurrency issues in Blazor Server
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Expose a scoped ApplicationDbContext for services that require it (e.g., Identity EF stores)
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Identity cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
});

// Add Razor Pages (needed for _Host.cshtml and Error.cshtml)
builder.Services.AddRazorPages();

// Add controllers for authentication endpoints
builder.Services.AddControllers();

builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

// Radzen services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Application services
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IExamAppService, ExamAppService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages (for _Host and Error pages)
app.MapRazorPages();

// Map controllers for authentication
app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        await SeedDataAsync(context, userManager, roleManager, app.Environment.IsDevelopment());
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

static async Task SeedDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, bool isDevelopment)
{
    try
    {
        // Apply pending migrations without deleting data.
        // If the database schema exists but migrations history is missing (common in localdb/dev resets),
        // baseline the initial migration so future migrations (like the Exam App tables) can apply.
        await EnsureMigrationsBaselineAsync(context);
        await context.Database.MigrateAsync();

        // Seed roles
        var roles = new[] { UserRoles.Admin, UserRoles.Teacher, UserRoles.Student };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed admin user
        const string adminEmail = "admin@pep.com";
        const string adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }
        }

        // Seed sample courses only if none exist
        if (!context.Courses.Any())
        {
            var courses = new[]
            {
                new Course { Name = "Introduction to Programming", Code = "CS101", Description = "Basic programming concepts and C# fundamentals" },
                new Course { Name = "Data Structures and Algorithms", Code = "CS201", Description = "Advanced data structures and algorithm design" },
                new Course { Name = "Object-Oriented Programming", Code = "CS202", Description = "OOP principles and design patterns" },
                new Course { Name = "Database Systems", Code = "CS301", Description = "Database design and SQL programming" },
                new Course { Name = "Web Development", Code = "CS401", Description = "Modern web development with ASP.NET Core" }
            };

            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        // Log the exception but don't crash the application
        Console.WriteLine($"Error seeding database: {ex.Message}");
    }
}

static async Task EnsureMigrationsBaselineAsync(ApplicationDbContext context)
{
    const string initialMigrationId = "20251130100008_InitialCreate";
    const string productVersion = "6.0.36";

    var connection = context.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;

    try
    {
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        async Task<bool> TableExistsAsync(string tableName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = @name";

            var param = cmd.CreateParameter();
            param.ParameterName = "@name";
            param.Value = tableName;
            cmd.Parameters.Add(param);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        var historyExists = await TableExistsAsync("__EFMigrationsHistory");
        if (!historyExists)
        {
            // If there are already Identity tables, the schema exists and we should baseline.
            var hasIdentityTables = await TableExistsAsync("AspNetRoles");
            if (!hasIdentityTables)
            {
                return;
            }

            using (var createCmd = connection.CreateCommand())
            {
                createCmd.CommandText = @"
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);";
                await createCmd.ExecuteNonQueryAsync();
            }
        }

        using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = @migrationId)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (@migrationId, @productVersion);
END;";

            var p1 = insertCmd.CreateParameter();
            p1.ParameterName = "@migrationId";
            p1.Value = initialMigrationId;
            insertCmd.Parameters.Add(p1);

            var p2 = insertCmd.CreateParameter();
            p2.ParameterName = "@productVersion";
            p2.Value = productVersion;
            insertCmd.Parameters.Add(p2);

            await insertCmd.ExecuteNonQueryAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration baseline check failed: {ex.Message}");
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}
