// Names expected Identity failures without depending on HTTP.
// Handlers return these failures as Results; ServiceDefaults maps them to HTTP status codes.
using EventHub.BuildingBlocks.Results;
namespace Identity.Domain;
public static class UserErrors
{
    public static readonly Error DuplicateEmail = Error.Conflict("User.DuplicateEmail", "This email is already registered.");
    public static readonly Error InvalidCredentials = Error.Unauthorized("User.InvalidCredentials", "Invalid email or password.");
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "User not found.");
    public static readonly Error Unauthenticated = Error.Unauthorized("User.Unauthenticated", "Authentication is required.");
    public static readonly Error InvalidRole = Error.Validation("User.InvalidRole", "Choose a valid role.");
}
