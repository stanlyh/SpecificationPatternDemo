using System.Linq.Expressions;

namespace SpecificationPatternDemo;

public class Specification<TEntity> : ISpecification<TEntity>
    where TEntity : class
{
    private List<Expression<Func<TEntity, object>>>? _includeQueries;
    private List<Expression<Func<TEntity, object>>>? _orderByQueries;
    private List<Expression<Func<TEntity, object>>>? _orderByDescendingQueries;

    public Expression<Func<TEntity, bool>>? FilterQuery { get; private set; }
    public IReadOnlyCollection<Expression<Func<TEntity, object>>>? IncludeQueries => _includeQueries;
    public IReadOnlyCollection<Expression<Func<TEntity, object>>>? OrderByQueries => _orderByQueries;
    public IReadOnlyCollection<Expression<Func<TEntity, object>>>? OrderByDescendingQueries => _orderByDescendingQueries;

    protected Specification() { }

    protected Specification(Expression<Func<TEntity, bool>> query)
    {
        FilterQuery = query;
    }

    protected Specification(ISpecification<TEntity> specification)
    {
        FilterQuery = specification.FilterQuery;

        _includeQueries = specification.IncludeQueries?.ToList();
        _orderByQueries = specification.OrderByQueries?.ToList();
        _orderByDescendingQueries = specification.OrderByDescendingQueries?.ToList();
    }

    protected void AddFilteringQuery(Expression<Func<TEntity, bool>> query)
    {
        FilterQuery = query;
    }

    protected void AddIncludeQuery(Expression<Func<TEntity, object>> query)
    {
        _includeQueries ??= new();
        _includeQueries.Add(query);
    }

    protected void AddOrderByQuery(Expression<Func<TEntity, object>> query)
    {
        _orderByQueries ??= new();
        _orderByQueries.Add(query);
    }

    protected void AddOrderByDescendingQuery(Expression<Func<TEntity, object>> query)
    {
        _orderByDescendingQueries ??= new();
        _orderByDescendingQueries.Add(query);
    }

    public Specification<TEntity> And(Specification<TEntity> specification)
    => new AndSpecification<TEntity>(this, specification);

    public Specification<TEntity> Or(Specification<TEntity> specification)
        => new OrSpecification<TEntity>(this, specification);
}

