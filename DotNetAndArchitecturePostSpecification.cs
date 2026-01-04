using Microsoft.Extensions.Hosting;

namespace SpecificationPatternDemo;

public class DotNetAndArchitecturePostSpecification : Specification<Post>
{
    public DotNetAndArchitecturePostSpecification()
    {
        var dotNetSpec = new PostByCategorySpecification(".NET");
        var architectureSpec = new PostByCategorySpecification("Architecture");

        // Combine 2 specifications with OrSpecification
        var combinedSpec = dotNetSpec.Or(architectureSpec);

        AddFilteringQuery(combinedSpec.FilterQuery!);

        AddOrderByDescendingQuery(post => post.Id);
    }
}

public class Post
{
}