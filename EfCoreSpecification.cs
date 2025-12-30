using Microsoft.EntityFrameworkCore;

namespace SpecificationPatternDemo;

public class EfCoreSpecification<TEntity> : Specification<TEntity>
    where TEntity : class
{
    public EfCoreSpecification(ISpecification<TEntity> specification)
        : base(specification) { }

    public virtual IQueryable<TEntity> Apply(IQueryable<TEntity> queryable)
    {
        if (FilterQuery is not null)
        {
            queryable = queryable.Where(FilterQuery);
        }

        if (IncludeQueries?.Count > 0)
        {
            queryable = IncludeQueries.Aggregate(queryable,
                (current, includeQuery) => current.Include(includeQuery));
        }

        if (OrderByQueries?.Count > 0)
        {
            var orderedQueryable = queryable.OrderBy(OrderByQueries.First());

            orderedQueryable = OrderByQueries.Skip(1)
                .Aggregate(orderedQueryable, (current, orderQuery) => current.ThenBy(orderQuery));

            queryable = orderedQueryable;
        }

        if (OrderByDescendingQueries?.Count > 0)
        {
            var orderedQueryable = queryable.OrderByDescending(OrderByDescendingQueries.First());

            orderedQueryable = OrderByDescendingQueries.Skip(1)
                .Aggregate(orderedQueryable, (current, orderQuery) => current.ThenByDescending(orderQuery));

            queryable = orderedQueryable;
        }

        return queryable;
    }
}
