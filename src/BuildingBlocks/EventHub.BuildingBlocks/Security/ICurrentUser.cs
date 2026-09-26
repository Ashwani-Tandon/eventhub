namespace EventHub.BuildingBlocks.Security;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Role { get; }
}
// Defines the authenticated user's id and role as technical request context.
// Application handlers can consume this interface without depending on HttpContext.
