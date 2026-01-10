using Microsoft.EntityFrameworkCore;

namespace SpecificationPatternDemo;

public class BaseRepository<TEntity> where TEntity : class
{
    private readonly ApplicationDbContext _dbContext;

    public BaseRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TEntity>> WhereAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var efCoreSpecification = new EfCoreSpecification<TEntity>(specification);

        var query = _dbContext.Set<TEntity>().AsNoTracking();
        query = efCoreSpecification.Apply(query);

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
