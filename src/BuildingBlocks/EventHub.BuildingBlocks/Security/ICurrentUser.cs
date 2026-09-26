namespace EventHub.BuildingBlocks.Security;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Role { get; }
}
