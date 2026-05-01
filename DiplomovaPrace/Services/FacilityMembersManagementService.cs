using DiplomovaPrace.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Security.Cryptography;

namespace DiplomovaPrace.Services;

public sealed class FacilityMembersManagementService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationService _authService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<FacilityMembersManagementService> _logger;
    private readonly IWebHostEnvironment _environment;

    public FacilityMembersManagementService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationService authService,
        IEmailSender emailSender,
        ILogger<FacilityMembersManagementService> logger,
        IWebHostEnvironment environment)
    {
        _dbFactory = dbFactory;
        _authService = authService;
        _emailSender = emailSender;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Get all members of a facility with their user details.
    /// </summary>
    public async Task<List<FacilityMemberDto>> GetMembersAsync(int facilityId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var members = await db.FacilityMemberships
            .Where(m => m.FacilityId == facilityId)
            .Include(m => m.AppUser)
            .OrderByDescending(m => m.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return members.Select(m => new FacilityMemberDto(
            m.Id,
            m.AppUserId,
            m.AppUser?.Email ?? "unknown",
            m.Role,
            m.CreatedAtUtc,
            m.AppUser?.IsPasswordSet != false
        )).ToList();
    }

    /// <summary>
    /// Invite or attach a user by email with role assignment.
    /// - If the account exists: attach membership (existing-user flow).
    /// - If the account does not exist: create account, store invite token, send set-password email.
    ///
    /// Role policy: callerRole Admin may not assign Owner.
    /// </summary>
    public async Task<AddMemberByEmailResult> AddMemberByEmailAsync(
        int facilityId,
        string email,
        string role = "Viewer",
        FacilityMembershipRole callerRole = FacilityMembershipRole.Owner,
        string? baseUrl = null,
        CancellationToken ct = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail))
            return new AddMemberByEmailResult(AddMemberByEmailStatus.InvalidEmailFormat, null);

        var parsedRole = FacilityMembershipRoleExtensions.ParseOrViewer(role);

        // Role policy: Admin cannot assign Owner
        if (callerRole == FacilityMembershipRole.Admin && parsedRole == FacilityMembershipRole.Owner)
            return new AddMemberByEmailResult(AddMemberByEmailStatus.InsufficientPermission, null);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var user = await db.AppUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        bool isNewUser = user is null;

        if (isNewUser)
        {
            // Create a pending account with a secure invite token
            var token = GenerateSecureToken();
            user = new AppUserEntity
            {
                Email = normalizedEmail,
                PasswordHash = _authService.HashPassword(Guid.NewGuid().ToString()), // unusable placeholder
                IsPasswordSet = false,
                InviteToken = token,
                InviteTokenExpiresUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow
            };
            db.AppUsers.Add(user);
            await db.SaveChangesAsync(ct); // Persist to get Id
        }
        else
        {
            // Check if already a member
            var existingMembership = await db.FacilityMemberships
                .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.AppUserId == user!.Id, ct);

            if (existingMembership is not null)
                return new AddMemberByEmailResult(AddMemberByEmailStatus.AlreadyMember, null);
        }

        var membership = new FacilityMembershipEntity
        {
            FacilityId = facilityId,
            AppUserId = user!.Id,
            Role = parsedRole.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.FacilityMemberships.Add(membership);
        await db.SaveChangesAsync(ct);

        var dto = new FacilityMemberDto(
            membership.Id,
            membership.AppUserId,
            user.Email,
            membership.Role,
            membership.CreatedAtUtc,
            user.IsPasswordSet
        );

        if (isNewUser && user.InviteToken is not null)
        {
            var emailSent = true;

            // Send invite email (non-blocking on failure — user is already created)
            try
            {
                var setPasswordUrl = string.IsNullOrWhiteSpace(baseUrl)
                    ? $"/set-password?token={Uri.EscapeDataString(user.InviteToken)}"
                    : $"{baseUrl.TrimEnd('/')}/set-password?token={Uri.EscapeDataString(user.InviteToken)}";

                var body = BuildInviteEmailHtml(normalizedEmail, parsedRole.ToString(), setPasswordUrl);
                await _emailSender.SendAsync(normalizedEmail, "You have been invited to Facility Workbench", body, ct);
                _logger.LogInformation("Invite email dispatched to new user {Email}", normalizedEmail);
            }
            catch (Exception ex)
            {
                emailSent = false;

                if (_environment.IsDevelopment())
                {
                    _logger.LogWarning(ex, "Invite email delivery failed in Development for {Email}. Invite token for manual testing: {Token}", normalizedEmail, user.InviteToken);
                }
                else
                {
                    _logger.LogError(ex, "Invite email delivery failed for {Email}. Token is intentionally not logged outside Development.", normalizedEmail);
                }
            }

            if (!emailSent && !_environment.IsDevelopment())
                return new AddMemberByEmailResult(AddMemberByEmailStatus.EmailDeliveryFailed, dto);

            return new AddMemberByEmailResult(AddMemberByEmailStatus.InviteSent, dto);
        }

        return new AddMemberByEmailResult(AddMemberByEmailStatus.Success, dto);
    }

    private static string BuildInviteEmailHtml(string email, string role, string setPasswordUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:system-ui,sans-serif;background:#0f172a;color:#f1f5f9;margin:0;padding:2rem;">
              <div style="max-width:480px;margin:0 auto;background:#1e293b;border-radius:12px;padding:2rem;">
                <div style="text-align:center;margin-bottom:1.5rem;">
                  <span style="font-size:1.8rem;font-weight:800;background:linear-gradient(135deg,#22d3ee,#3b82f6);-webkit-background-clip:text;-webkit-text-fill-color:transparent;">FW</span>
                </div>
                <h1 style="font-size:1.25rem;font-weight:700;margin-bottom:0.5rem;">You have been invited</h1>
                <p style="color:#94a3b8;margin-bottom:1.5rem;">
                  You have been added to <strong>Facility Workbench</strong> as <strong>{role}</strong>.
                </p>
                <p style="color:#cbd5e1;margin-bottom:1.5rem;">
                  Your account has been created for <strong>{email}</strong>.
                  Click the button below to set your password and get started.
                </p>
                <div style="text-align:center;margin-bottom:1.5rem;">
                  <a href="{setPasswordUrl}" style="display:inline-block;padding:0.75rem 2rem;background:linear-gradient(135deg,#22d3ee,#3b82f6);color:#fff;font-weight:700;border-radius:8px;text-decoration:none;">
                    Set my password
                  </a>
                </div>
                <p style="color:#64748b;font-size:0.8rem;text-align:center;">
                  This invite link expires in 7 days. If you did not expect this email, you can ignore it.
                </p>
              </div>
            </body>
            </html>
            """;
    }

    private static bool TryNormalizeEmail(string? email, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;

        if (string.IsNullOrWhiteSpace(email))
            return false;

        var trimmed = email.Trim();

        try
        {
            var parsed = new MailAddress(trimmed);
            if (!string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase))
                return false;

            normalizedEmail = parsed.Address.ToLowerInvariant();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    /// <summary>
    /// Update a member's role with caller-role policy enforcement.
    /// Admin cannot promote anyone to Owner.
    /// </summary>
    public async Task<UpdateRoleResult> UpdateMemberRoleAsync(
        int membershipId,
        string newRole,
        FacilityMembershipRole callerRole = FacilityMembershipRole.Owner,
        int? currentUserId = null,
        CancellationToken ct = default)
    {
        var parsedRole = FacilityMembershipRoleExtensions.ParseOrViewer(newRole);

        // Role policy: Admin cannot set Owner
        if (callerRole == FacilityMembershipRole.Admin && parsedRole == FacilityMembershipRole.Owner)
            return UpdateRoleResult.InsufficientPermission;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var membership = await db.FacilityMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct);

        if (membership is null)
            return UpdateRoleResult.NotFound;

        if (currentUserId.HasValue && membership.AppUserId == currentUserId.Value)
            return UpdateRoleResult.SelfLockoutBlocked;

        var currentRole = FacilityMembershipRoleExtensions.ParseOrViewer(membership.Role);
        if (currentRole == FacilityMembershipRole.Owner && parsedRole != FacilityMembershipRole.Owner)
        {
            var ownerCount = await db.FacilityMemberships
                .CountAsync(m => m.FacilityId == membership.FacilityId && m.Role == FacilityMembershipRole.Owner.ToString(), ct);

            if (ownerCount <= 1)
                return UpdateRoleResult.CannotDemoteLastOwner;
        }

        membership.Role = parsedRole.ToString();
        await db.SaveChangesAsync(ct);
        return UpdateRoleResult.Success;
    }

    /// <summary>
    /// Remove a member from a facility with safeguards.
    /// </summary>
    public async Task<FacilityMemberRemovalResult> RemoveMemberAsync(
        int membershipId,
        int currentUserId,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var membership = await db.FacilityMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct);

        if (membership is null)
            return FacilityMemberRemovalResult.MemberNotFound;

        // Cannot remove yourself
        if (membership.AppUserId == currentUserId)
            return FacilityMemberRemovalResult.CannotRemoveSelf;

        // Check if this is the last Owner
        if (membership.Role == FacilityMembershipRole.Owner.ToString())
        {
            var ownerCount = await db.FacilityMemberships
                .CountAsync(m =>
                    m.FacilityId == membership.FacilityId &&
                    m.Role == FacilityMembershipRole.Owner.ToString(),
                ct);

            if (ownerCount == 1)
                return FacilityMemberRemovalResult.CannotRemoveLastOwner;
        }

        db.FacilityMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);

        return FacilityMemberRemovalResult.Success;
    }

    /// <summary>
    /// Count owners in a facility.
    /// </summary>
    public async Task<int> CountOwnersAsync(int facilityId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.FacilityMemberships
            .CountAsync(m =>
                m.FacilityId == facilityId &&
                m.Role == FacilityMembershipRole.Owner.ToString(),
                ct);
    }

    /// <summary>
    /// Check if user is Owner or Admin of a facility.
    /// </summary>
    public async Task<bool> IsUserOwnerOrAdminAsync(int facilityId, int appUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var membership = await db.FacilityMemberships
            .FirstOrDefaultAsync(m =>
                m.FacilityId == facilityId && m.AppUserId == appUserId,
                ct);

        if (membership is null)
            return false;

        var role = FacilityMembershipRoleExtensions.ParseOrViewer(membership.Role);
        return role.CanUseEditor();
    }

    /// <summary>
    /// Get the role of a specific user in a facility, or null if not a member.
    /// </summary>
    public async Task<FacilityMembershipRole?> GetUserRoleAsync(int facilityId, int appUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var membership = await db.FacilityMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.AppUserId == appUserId, ct);

        if (membership is null)
            return null;

        return FacilityMembershipRoleExtensions.ParseOrViewer(membership.Role);
    }
}

public sealed record FacilityMemberDto(
    int MembershipId,
    int AppUserId,
    string Email,
    string Role,
    DateTime CreatedAtUtc,
    bool IsPasswordSet = true
);

public sealed record AddMemberByEmailResult(
    AddMemberByEmailStatus Status,
    FacilityMemberDto? Member
);

public enum AddMemberByEmailStatus
{
    Success,
    InviteSent,
    EmailDeliveryFailed,
    InvalidEmailFormat,
    UserNotFound,
    AlreadyMember,
    InsufficientPermission
}

public enum UpdateRoleResult
{
    Success,
    NotFound,
    InsufficientPermission,
    CannotDemoteLastOwner,
    SelfLockoutBlocked
}

public enum FacilityMemberRemovalResult
{
    Success,
    MemberNotFound,
    CannotRemoveSelf,
    CannotRemoveLastOwner
}

