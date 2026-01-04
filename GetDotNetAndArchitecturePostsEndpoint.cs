namespace SpecificationPatternDemo;

public class GetDotNetAndArchitecturePostsEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/social-media/dotnet-architecture-posts", Handle);
    }

    private static async Task<IResult> Handle(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var specification = new DotNetAndArchitecturePostSpecification();

        var response = await dbContext
            .ApplySpecification(specification)
            .Select(post => post.ToDto())
            .ToListAsync(cancellationToken);

        return Results.Ok(response);
    }
}
