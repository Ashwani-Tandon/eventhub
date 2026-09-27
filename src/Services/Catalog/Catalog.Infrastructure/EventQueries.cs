// Executes untracked Catalog reads and projects rows directly into API-safe DTOs.
// This keeps query concerns separate from tracked aggregate repositories.
using Catalog.Application;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

/// <summary>
/// Reads event DTOs without tracking entities; counts filters before applying paging and scopes organizer reads by user id.
/// </summary>
public sealed class EventQueries(CatalogDbContext db) : IEventQueries
{
    /// <summary>
    /// Applies filters, counts all matches, and projects the requested page into event DTOs.
    /// </summary>
    public async Task<EventSearchResult> SearchAsync(
        EventSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = db.Events.AsNoTracking().Where(x => x.StartsAt >= (criteria.From ?? criteria.Now));
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            query = query.Where(x => x.Category == criteria.Category);
        }

        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            query = query.Where(x => x.City == criteria.City);
        }

        if (criteria.To is { } to)
        {
            query = query.Where(x => x.StartsAt <= to);
        }

        if (criteria.MaxPrice is { } maxPrice)
        {
            query = query.Where(x => x.Price <= maxPrice);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await Project(query.OrderBy(x => x.StartsAt)
            .Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize))
            .ToListAsync(cancellationToken);
        var items = rows.Select(ToDto).ToList();
        return new(items, total);
    }

    /// <summary>
    /// Projects a single event into a DTO without tracking its entity.
    /// </summary>
    public async Task<EventDto?> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var row = await Project(db.Events.AsNoTracking().Where(x => x.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : ToDto(row);
    }

    /// <summary>
    /// Projects owned events, including past events; administrators can include every owner.
    /// </summary>
    public async Task<IReadOnlyList<EventDto>> GetMineAsync(
        Guid userId,
        bool includeAll,
        CancellationToken cancellationToken)
    {
        var query = db.Events.AsNoTracking();
        if (!includeAll)
        {
            query = query.Where(x => x.OrganizerId == userId);
        }

        var rows = await Project(query.OrderBy(x => x.StartsAt)).ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps database event columns directly into DTO fields, including calculated seats left.
    /// </summary>
    private static IQueryable<EventReadModel> Project(IQueryable<Domain.Event> query)
    {
        return query.Select(x => new EventReadModel(x.Id, x.Title, x.Description, x.Category, x.Venue, x.City,
            x.StartsAt, x.Price, x.Capacity, x.SeatsBooked, x.OrganizerId, x.RowVersion));
    }

    private static EventDto ToDto(EventReadModel row)
    {
        return new EventDto(row.Id, row.Title, row.Description, row.Category, row.Venue, row.City,
            row.StartsAt, row.Price, row.Capacity, row.SeatsBooked, row.Capacity - row.SeatsBooked,
            row.OrganizerId, Convert.ToBase64String(row.RowVersion));
    }

    private sealed record EventReadModel(
        int Id,
        string Title,
        string Description,
        string Category,
        string Venue,
        string City,
        DateTimeOffset StartsAt,
        decimal Price,
        int Capacity,
        int SeatsBooked,
        Guid OrganizerId,
        byte[] RowVersion);
}
