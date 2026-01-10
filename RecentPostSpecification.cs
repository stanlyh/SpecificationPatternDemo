using System;

namespace SpecificationPatternDemo;

public class RecentPostSpecification : Specification<Post>
{
    public RecentPostSpecification(int daysBack = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysBack);
        AddFilteringQuery(post => post.CreatedAt >= cutoff);
        AddOrderByDescendingQuery(post => post.CreatedAt);
    }
}
