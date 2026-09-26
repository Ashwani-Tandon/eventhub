// Defines the authenticated user's id and role as technical request context.
// Application handlers can consume this interface without depending on HttpContext.
namespace EventHub.BuildingBlocks.Security;

/// <summary>
/// Defines the authenticated user's id and role as technical request context. Application handlers can consume this interface without depending on HttpContext.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Role { get; }
}
