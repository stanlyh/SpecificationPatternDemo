namespace SpecificationPatternDemo;

public class HighEngagementRecentPostSpecification : Specification<Post>
{
    public HighEngagementRecentPostSpecification(int daysBack = 7,
        int minLikes = 100, int minComments = 30)
    {
        var recentSpec = new RecentPostSpecification(daysBack);
        var highEngagementSpec = new HighEngagementPostSpecification(minLikes, minComments);

        // Combine 2 specifications with AndSpecification
        var combinedSpec = recentSpec.And(highEngagementSpec);

        AddFilteringQuery(combinedSpec.FilterQuery!);

        AddOrderByDescendingQuery(post => post.Likes.Count + post.Comments.Count);
    }
}
