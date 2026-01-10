using System;

namespace SpecificationPatternDemo;

public class PostByCategorySpecification : Specification<Post>
{
    public PostByCategorySpecification(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("category is required", nameof(category));

        AddFilteringQuery(post => post.Category == category);
    }
}