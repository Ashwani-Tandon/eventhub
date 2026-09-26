// Implements tracked writes and untracked DTO reads.
// This adapter connects the application ports to the hosting infrastructure.
using Identity.Application;
using Identity.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace Identity.Infrastructure;
public sealed class UserRepository(IdentityDbContext db) : IUserRepository, IUserQueries, IUnitOfWork
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public void Add(User user) => db.Users.Add(user);
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().OrderBy(x => x.Email)
            .Select(x => new UserDto(x.Id, x.Email, x.FullName, x.Role)).ToListAsync(cancellationToken);
    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Registration races are expected conflicts; discard the losing tracked insert.
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
