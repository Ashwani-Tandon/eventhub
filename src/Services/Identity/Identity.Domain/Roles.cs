// Defines the three stored roles and their allowed values.
// Domain rules use this vocabulary so registration and role changes store supported roles.
namespace Identity.Domain;
public static class Roles
{
    public const string Attendee = "Attendee";
    public const string Organizer = "Organizer";
    public const string Admin = "Admin";
    public static bool IsValid(string role) => role is Attendee or Organizer or Admin;
}
