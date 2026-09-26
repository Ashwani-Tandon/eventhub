// Defines the three stored roles and their allowed values.
// Domain rules use this vocabulary so registration and role changes store supported roles.
namespace Identity.Domain;

/// <summary>
/// Defines the three stored roles and their allowed values. Domain rules use this vocabulary so registration and role changes store supported roles.
/// </summary>
public static class Roles
{
    public const string Attendee = "Attendee";

    public const string Organizer = "Organizer";

    public const string Admin = "Admin";

    /// <summary>
    /// Checks whether the supplied role belongs to EventHub's allowed role set.
    /// </summary>
    public static bool IsValid(string role)
    {
        return role is Attendee or Organizer or Admin;
    }
}
