using DiplomovaPrace.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DiplomovaPrace.Pages;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly AuthenticationService _authService;

    public string? ErrorMessage { get; set; }
    public bool Success { get; set; }

    public ChangePasswordModel(AuthenticationService authService)
    {
        _authService = authService;
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            ErrorMessage = "Current password is required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ErrorMessage = "New password must be at least 6 characters.";
            return Page();
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "New passwords do not match.";
            return Page();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            ErrorMessage = "Unable to identify current user. Please sign in again.";
            return Page();
        }

        var result = await _authService.ChangePasswordAsync(userId, currentPassword, newPassword);

        switch (result)
        {
            case ChangePasswordResult.Success:
                Success = true;
                return Page();

            case ChangePasswordResult.WrongCurrentPassword:
                ErrorMessage = "Current password is incorrect.";
                return Page();

            case ChangePasswordResult.NewPasswordTooShort:
                ErrorMessage = "New password must be at least 6 characters.";
                return Page();

            case ChangePasswordResult.UserNotFound:
                ErrorMessage = "User not found. Please sign in again.";
                return Page();

            default:
                ErrorMessage = "An unexpected error occurred. Please try again.";
                return Page();
        }
    }
}
