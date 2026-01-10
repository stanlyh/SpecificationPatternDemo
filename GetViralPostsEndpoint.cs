using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SpecificationPatternDemo;

public class GetViralPostsEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/social-media/viral-posts", Handle);
    }

    private static async Task<IResult> Handle(
        [FromQuery] int? minLikesCount,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] ILogger<GetViralPostsEndpoint> logger,
        CancellationToken cancellationToken)
    {
        var specification = new ViralPostSpecification(minLikesCount ?? 150);

        var response = await dbContext
            .ApplySpecification(specification)
            .Select(Post.ToDtoExpression)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Retrieved {Count} viral posts with minimum {MinLikes} likes",
            response.Count, minLikesCount ?? 150);

        return Results.Ok(response);
    }
}
