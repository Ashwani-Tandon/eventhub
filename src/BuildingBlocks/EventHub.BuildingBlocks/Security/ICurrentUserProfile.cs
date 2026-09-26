// Exposes token profile claims without adding HTTP dependencies.
// Queries can read the signed email and name while ICurrentUser remains a narrow id/role port.
namespace EventHub.BuildingBlocks.Security;

public interface ICurrentUserProfile : ICurrentUser
{
    string? Email { get; }
    string? FullName { get; }
}
