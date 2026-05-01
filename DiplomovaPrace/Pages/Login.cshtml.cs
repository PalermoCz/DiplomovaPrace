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
    public class LoginModel : PageModel
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly DiplomovaPrace.Services.AuthenticationService _authService;

        public string? ErrorMessage { get; set; }

        public LoginModel(IDbContextFactory<AppDbContext> dbFactory, DiplomovaPrace.Services.AuthenticationService authService)
        {
            _dbFactory = dbFactory;
            _authService = authService;
        }

        public async Task<IActionResult> OnPostAsync(string email, string password)
        {
            // Validate input
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Email a heslo jsou vyžadovány.";
                return Page();
            }

            try
            {
                // Find user by email
                await using var db = await _dbFactory.CreateDbContextAsync();
                var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ErrorMessage = "Nesprávné přihlašovací údaje.";
                    return Page();
                }

                // Verify password
                if (!_authService.VerifyPassword(password, user.PasswordHash))
                {
                    ErrorMessage = "Nesprávné přihlašovací údaje.";
                    return Page();
                }

                // Update last login
                user.LastLoginUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();

                // Create authentication cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
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
                ErrorMessage = $"Chyba: {ex.Message}";
                return Page();
            }
        }
    }
}
