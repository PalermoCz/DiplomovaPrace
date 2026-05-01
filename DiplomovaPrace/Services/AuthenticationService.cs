using System.Security.Cryptography;

namespace DiplomovaPrace.Services;

/// <summary>
/// Služba pro bezpečné hashování a ověřování hesel.
/// Používá PBKDF2 (Rfc2898DeriveBytes - modernizované API).
/// V budoucnosti: lze rozšířit o MFA, password reset, email verification.
/// </summary>
public class AuthenticationService
{
    /// <summary>Zahešuje heslo pomocí PBKDF2.</summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Heslo nemůže být prázdné.", nameof(password));
        }

        // Generuj náhodný salt
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Použij PBKDF2 s SHA256 (10000 iterací = dostatečné pro MVP)
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 20);

        // Kombinuj salt + hash do jednoho stringu
        byte[] hashWithSalt = new byte[36];
        System.Buffer.BlockCopy(salt, 0, hashWithSalt, 0, 16);
        System.Buffer.BlockCopy(hash, 0, hashWithSalt, 16, 20);

        return Convert.ToBase64String(hashWithSalt);
    }

    /// <summary>Ověří heslo proti hešu.</summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            byte[] hashWithSalt = Convert.FromBase64String(hash);
            byte[] salt = new byte[16];
            System.Buffer.BlockCopy(hashWithSalt, 0, salt, 0, 16);

            // Recompute hash
            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 20);

            // Porovnej
            for (int i = 0; i < 20; i++)
            {
                if (hashWithSalt[i + 16] != computedHash[i])
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
