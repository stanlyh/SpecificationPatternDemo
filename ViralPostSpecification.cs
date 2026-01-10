namespace SpecificationPatternDemo;

public class ViralPostSpecification : Specification<Post>
{
    public ViralPostSpecification(int minLikes = 150)
    {
        AddFilteringQuery(post => post.Likes.Count >= minLikes);
        AddOrderByDescendingQuery(post => post.Likes.Count);
    }
}
