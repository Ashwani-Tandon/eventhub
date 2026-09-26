// Implements tracked writes and untracked DTO reads.
// This adapter connects the application ports to the hosting infrastructure.
using Identity.Application;
using Identity.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

/// <summary>
/// Implements tracked writes and untracked DTO reads. This adapter connects the application ports to the hosting infrastructure.
/// </summary>
public sealed class UserRepository(IdentityDbContext db) : IUserRepository, IUserQueries, IUnitOfWork
{
    /// <summary>
    /// Loads the user for normalized email lookup during login and duplicate checks.
    /// </summary>
    public Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    /// <summary>
    /// Loads the user targeted by an administrator's role-change command.
    /// </summary>
    public Task<User?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Tracks a new aggregate so the unit of work can insert it.
    /// </summary>
    public void Add(User user)
    {
        db.Users.Add(user);
    }

    /// <summary>
    /// Projects users into safe DTOs without returning password hashes or tracking read-only entities.
    /// </summary>
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Users.AsNoTracking().OrderBy(x => x.Email)
                .Select(x => new UserDto(x.Id, x.Email, x.FullName, x.Role)).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Saves tracked changes; Identity's implementation returns false on a duplicate email race.
    /// </summary>
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
