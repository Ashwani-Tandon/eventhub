// Shares deterministic demo identifiers for later service seeds.
// Development seeds use these ids so service-owned records can refer to the same demo users.
namespace EventHub.SeedData;

public static class DemoUsers
{
    public static readonly Guid Admin = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid Organizer = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid Organizer2 = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid Attendee = Guid.Parse("00000000-0000-0000-0000-000000000004");
    public static readonly Guid Attendee2 = Guid.Parse("00000000-0000-0000-0000-000000000005");
    public const string Password = "Demo@123";
}
