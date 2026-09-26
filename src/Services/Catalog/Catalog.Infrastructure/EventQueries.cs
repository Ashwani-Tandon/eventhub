// Executes untracked Catalog reads and projects rows directly into API-safe DTOs.
// This keeps query concerns separate from tracked aggregate repositories.
using Catalog.Application;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class EventQueries(CatalogDbContext db) : IEventQueries
{
    public async Task<EventSearchResult> SearchAsync(EventSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = db.Events.AsNoTracking().Where(x => x.StartsAt >= (criteria.From ?? criteria.Now));
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(criteria.Category))
            query = query.Where(x => x.Category == criteria.Category);
        if (!string.IsNullOrWhiteSpace(criteria.City))
            query = query.Where(x => x.City == criteria.City);
        if (criteria.To is { } to) query = query.Where(x => x.StartsAt <= to);
        if (criteria.MaxPrice is { } maxPrice) query = query.Where(x => x.Price <= maxPrice);

        var total = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderBy(x => x.StartsAt)
            .Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize))
            .ToListAsync(cancellationToken);
        return new(items, total);
    }

    public Task<EventDto?> GetAsync(int id, CancellationToken cancellationToken) =>
        Project(db.Events.AsNoTracking().Where(x => x.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<EventDto>> GetMineAsync(Guid userId, bool includeAll,
        CancellationToken cancellationToken)
    {
        var query = db.Events.AsNoTracking();
        if (!includeAll) query = query.Where(x => x.OrganizerId == userId);
        return await Project(query.OrderBy(x => x.StartsAt)).ToListAsync(cancellationToken);
    }

    private static IQueryable<EventDto> Project(IQueryable<Domain.Event> query) =>
        query.Select(x => new EventDto(x.Id, x.Title, x.Description, x.Category, x.Venue, x.City,
            x.StartsAt, x.Price, x.Capacity, x.SeatsBooked, x.Capacity - x.SeatsBooked, x.OrganizerId));
}
