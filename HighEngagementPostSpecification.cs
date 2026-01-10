namespace SpecificationPatternDemo;

public class HighEngagementPostSpecification : Specification<Post>
{
    public HighEngagementPostSpecification(int minLikes = 100, int minComments = 30)
    {
        AddFilteringQuery(post => post.Likes.Count >= minLikes && post.Comments.Count >= minComments);
    }
}
