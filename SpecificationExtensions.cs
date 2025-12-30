namespace SpecificationPatternDemo;

public class SpecificationExtensions
{
    public static IQueryable<TEntity> ApplySpecification<TEntity>(
        this ApplicationDbContext dbContext,
        ISpecification<TEntity> specification) where TEntity : class
    {
        var efCoreSpecification = new EfCoreSpecification<TEntity>(specification);

        var query = dbContext.Set<TEntity>().AsNoTracking();
        query = efCoreSpecification.Apply(query);

        return query;
    }
}
