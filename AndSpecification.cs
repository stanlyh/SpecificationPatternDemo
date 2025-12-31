using System.Linq.Expressions;

namespace SpecificationPatternDemo;

public class AndSpecification<TEntity> : Specification<TEntity>
    where TEntity : class
{
    public AndSpecification(Specification<TEntity> left, Specification<TEntity> right)
    {
        RegisterFilteringQuery(left, right);
    }

    private void RegisterFilteringQuery(Specification<TEntity> left, Specification<TEntity> right)
    {
        var leftExpression = left.FilterQuery;
        var rightExpression = right.FilterQuery;

        if (leftExpression is null && rightExpression is null)
        {
            return;
        }

        if (leftExpression is not null && rightExpression is null)
        {
            AddFilteringQuery(leftExpression);
            return;
        }

        if (leftExpression is null && rightExpression is not null)
        {
            AddFilteringQuery(rightExpression);
            return;
        }

        var replaceVisitor = new ReplaceExpressionVisitor(rightExpression!.Parameters.Single(), leftExpression!.Parameters.Single());
        var replacedBody = replaceVisitor.Visit(rightExpression.Body);

        var andExpression = Expression.AndAlso(leftExpression.Body, replacedBody);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(andExpression, leftExpression.Parameters.Single());

        AddFilteringQuery(lambda);
    }
}

