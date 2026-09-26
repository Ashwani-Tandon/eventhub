// Owns user identity and enforces valid role transitions.
// Registration creates attendees; administrator commands invoke its role-change rule.
using EventHub.BuildingBlocks.Domain;
using EventHub.BuildingBlocks.Results;
namespace Identity.Domain;
public sealed class User : Entity<Guid>
{
    private User() : base(Guid.Empty) { }
    private User(Guid id, string email, string fullName, string passwordHash, string role, DateTimeOffset createdAt) : base(id)
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
    public static User Create(Guid id, string email, string fullName, string passwordHash, DateTimeOffset createdAt) =>
        new(id, email.Trim().ToLowerInvariant(), fullName.Trim(), passwordHash, Roles.Attendee, createdAt);
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
