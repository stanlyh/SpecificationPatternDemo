using System.Linq.Expressions;

namespace SpecificationPatternDemo;

public interface ISpecification<TEntity>
    where TEntity : class
{
    Expression<Func<TEntity, bool>>? FilterQuery { get; }
    IReadOnlyCollection<Expression<Func<TEntity, object>>>? IncludeQueries { get; }
    IReadOnlyCollection<Expression<Func<TEntity, object>>>? OrderByQueries { get; }
    IReadOnlyCollection<Expression<Func<TEntity, object>>>? OrderByDescendingQueries { get; }
}
