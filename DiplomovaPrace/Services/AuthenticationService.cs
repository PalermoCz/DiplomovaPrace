using System.Security.Cryptography;
using DiplomovaPrace.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiplomovaPrace.Services;

/// <summary>
/// Služba pro bezpečné hashování a ověřování hesel.
/// Používá PBKDF2 (Rfc2898DeriveBytes - modernizované API).
/// </summary>
public class AuthenticationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AuthenticationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>Zahešuje heslo pomocí PBKDF2.</summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 20);

        byte[] hashWithSalt = new byte[36];
        Buffer.BlockCopy(salt, 0, hashWithSalt, 0, 16);
        Buffer.BlockCopy(hash, 0, hashWithSalt, 16, 20);

        return Convert.ToBase64String(hashWithSalt);
    }

    /// <summary>Ověří heslo proti hešu.</summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            byte[] hashWithSalt = Convert.FromBase64String(hash);
            byte[] salt = new byte[16];
            Buffer.BlockCopy(hashWithSalt, 0, salt, 0, 16);

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 20);

            for (int i = 0; i < 20; i++)
                if (hashWithSalt[i + 16] != computedHash[i])
                    return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Change password for an authenticated user.
    /// Returns false when currentPassword does not match.
    /// </summary>
    public async Task<ChangePasswordResult> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return ChangePasswordResult.NewPasswordTooShort;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ChangePasswordResult.UserNotFound;

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return ChangePasswordResult.WrongCurrentPassword;

        user.PasswordHash = HashPassword(newPassword);
        user.IsPasswordSet = true;
        await db.SaveChangesAsync(ct);

        return ChangePasswordResult.Success;
    }

    /// <summary>
    /// Set password using a secure invite/reset token.
    /// Clears the token on success.
    /// </summary>
    public async Task<SetPasswordFromTokenResult> SetPasswordFromTokenAsync(
        string token,
        string newPassword,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return SetPasswordFromTokenResult.InvalidToken;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return SetPasswordFromTokenResult.PasswordTooShort;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var user = await db.AppUsers
            .FirstOrDefaultAsync(u => u.InviteToken == token, ct);

        if (user is null)
            return SetPasswordFromTokenResult.InvalidToken;

        if (user.InviteTokenExpiresUtc.HasValue && user.InviteTokenExpiresUtc.Value < DateTime.UtcNow)
            return SetPasswordFromTokenResult.TokenExpired;

        user.PasswordHash = HashPassword(newPassword);
        user.IsPasswordSet = true;
        user.InviteToken = null;
        user.InviteTokenExpiresUtc = null;
        await db.SaveChangesAsync(ct);

        return SetPasswordFromTokenResult.Success;
    }

    /// <summary>
    /// Look up the user associated with a valid, non-expired invite token.
    /// Returns null if the token is invalid or expired.
    /// </summary>
    public async Task<AppUserEntity?> FindUserByValidInviteTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var user = await db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.InviteToken == token, ct);

        if (user is null)
            return null;

        if (user.InviteTokenExpiresUtc.HasValue && user.InviteTokenExpiresUtc.Value < DateTime.UtcNow)
            return null;

        return user;
    }
}

public enum ChangePasswordResult
{
    Success,
    UserNotFound,
    WrongCurrentPassword,
    NewPasswordTooShort
}

public enum SetPasswordFromTokenResult
{
    Success,
    InvalidToken,
    TokenExpired,
    PasswordTooShort
}

