// Owns user identity and enforces valid role transitions.
// Registration creates attendees; administrator commands invoke its role-change rule.
using EventHub.BuildingBlocks.Domain;
using EventHub.BuildingBlocks.Results;

namespace Identity.Domain;

/// <summary>
/// Owns user identity and enforces valid role transitions. Registration creates attendees; administrator commands invoke its role-change rule.
/// </summary>
public sealed class User : Entity<Guid>
{
    /// <summary>
    /// Reconstructs the stored user fields; EF Core uses the empty constructor while registration uses the creation factory.
    /// </summary>
    private User() : base(Guid.Empty)
    {
    }

    /// <summary>
    /// Reconstructs the stored user fields; EF Core uses the empty constructor while registration uses the creation factory.
    /// </summary>
    private User(
        Guid id,
        string email,
        string fullName,
        string passwordHash,
        string role,
        DateTimeOffset createdAt) : base(id)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = createdAt;
    }

    public string Email { get; private set; } = "";

    public string FullName { get; private set; } = "";

    public string PasswordHash { get; private set; } = "";

    public string Role { get; private set; } = Roles.Attendee;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a user with a normalized email, a stored password hash, and the default Attendee role.
    /// </summary>
    public static User Create(
        Guid id,
        string email,
        string fullName,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        return new(id, email.Trim().ToLowerInvariant(), fullName.Trim(), passwordHash, Roles.Attendee, createdAt);
    }

    /// <summary>
    /// Accepts only known roles; existing tokens retain their previous role until another login issues fresh claims.
    /// </summary>
    public Result ChangeRole(string role)
    {
        if (!Roles.IsValid(role))
        {
            return Result.Failure(UserErrors.InvalidRole);
        }

        Role = role;
        return Result.Success();
    }
}
