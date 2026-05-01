using DiplomovaPrace.Persistence;
using DiplomovaPrace.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DiplomovaPrace.Pages;

[AllowAnonymous]
public class SetPasswordModel : PageModel
{
    private readonly DiplomovaPrace.Services.AuthenticationService _authService;

    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public bool TokenInvalid { get; set; }

    public SetPasswordModel(DiplomovaPrace.Services.AuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        Token = token;

        if (string.IsNullOrWhiteSpace(token))
        {
            TokenInvalid = true;
            return Page();
        }

        var user = await _authService.FindUserByValidInviteTokenAsync(token);
        if (user is null)
        {
            TokenInvalid = true;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string token, string newPassword, string confirmPassword)
    {
        Token = token;

        if (string.IsNullOrWhiteSpace(token))
        {
            TokenInvalid = true;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters.";
            return Page();
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        var result = await _authService.SetPasswordFromTokenAsync(token, newPassword);

        switch (result)
        {
            case SetPasswordFromTokenResult.Success:
                // Find the user to sign them in
                // Token is now cleared, so we look them up by other means via a fresh GET
                // Sign-in happens after a redirect to login with a success hint
                return LocalRedirect("/login?invited=1");

            case SetPasswordFromTokenResult.TokenExpired:
                TokenInvalid = true;
                return Page();

            case SetPasswordFromTokenResult.InvalidToken:
                TokenInvalid = true;
                return Page();

            case SetPasswordFromTokenResult.PasswordTooShort:
                ErrorMessage = "Password must be at least 6 characters.";
                return Page();

            default:
                ErrorMessage = "An unexpected error occurred. Please try again.";
                return Page();
        }
    }
}
