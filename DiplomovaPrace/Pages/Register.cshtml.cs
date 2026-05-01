using DiplomovaPrace.Persistence;
using DiplomovaPrace.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DiplomovaPrace.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class RegisterModel : PageModel
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly DiplomovaPrace.Services.AuthenticationService _authService;

        public string? ErrorMessage { get; set; }

        public RegisterModel(IDbContextFactory<AppDbContext> dbFactory, DiplomovaPrace.Services.AuthenticationService authService)
        {
            _dbFactory = dbFactory;
            _authService = authService;
        }

        public async Task<IActionResult> OnPostAsync(string email, string password, string confirmPassword)
        {
            // Validate input
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ErrorMessage = "All fields are required.";
                return Page();
            }

            if (password != confirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            if (password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters long.";
                return Page();
            }

            // Check for existing user
            await using var db = await _dbFactory.CreateDbContextAsync();
            var existingUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            
            if (existingUser != null)
            {
                ErrorMessage = "An account with this email already exists.";
                return Page();
            }

            try
            {
                // Hash password and create new user
                var passwordHash = _authService.HashPassword(password);
                var newUser = new AppUserEntity
                {
                    Email = email,
                    PasswordHash = passwordHash,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastLoginUtc = DateTime.UtcNow
                };

                db.AppUsers.Add(newUser);
                await db.SaveChangesAsync();

                // Auto sign in
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString()),
                    new Claim(ClaimTypes.Email, newUser.Email)
                };

                var identity = new ClaimsIdentity(claims, "CookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("CookieAuth", principal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                return LocalRedirect("/facility");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                return Page();
            }
        }
    }
}
