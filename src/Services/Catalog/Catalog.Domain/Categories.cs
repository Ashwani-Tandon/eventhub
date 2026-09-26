// Defines the only categories accepted by Catalog.
// Domain rules and API validation share these constants without duplicating magic strings.
namespace Catalog.Domain;

public static class Categories
{
    public const string Music = "Music";
    public const string Tech = "Tech";
    public const string Sports = "Sports";
    public const string Comedy = "Comedy";
    public const string Workshop = "Workshop";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Music, Tech, Sports, Comedy, Workshop
    };

    public static bool IsKnown(string category) => All.Contains(category);
}
