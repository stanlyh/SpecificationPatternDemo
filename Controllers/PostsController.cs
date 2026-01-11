using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpecificationPatternDemo.Dto;

namespace SpecificationPatternDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public PostsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET api/posts
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? authorId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? minLikes = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) return BadRequest("pageNumber must be >= 1");
        if (pageSize < 1 || pageSize > 100) return BadRequest("pageSize must be between 1 and 100");

        var query = _dbContext.Posts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(authorId))
            query = query.Where(p => p.AuthorId == authorId);

        if (from.HasValue)
            query = query.Where(p => p.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.CreatedAt <= to.Value);

        if (minLikes.HasValue)
            query = query.Where(p => p.Likes.Count >= minLikes.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(Post.ToDtoExpression)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var metadata = BuildPaginationMetadata(
            actionName: nameof(GetAll),
            pageNumber: pageNumber,
            pageSize: pageSize,
            totalCount: total,
            additionalRouteValues: new { category, authorId, from, to, minLikes }
        );

        return Ok(new { Items = items, Metadata = metadata });
    }

    // GET api/posts/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .Where(p => p.Id == id)
            .Select(Post.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (post is null) return NotFound();
        return Ok(post);
    }

    // POST api/posts
    [HttpPost]
    [Authorize(Policy = "CanModifyPosts")]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto, CancellationToken cancellationToken)
    {
        var userId = User.Identity?.Name ?? "";

        var post = new Post
        {
            Title = dto.Title,
            Content = dto.Content,
            Category = dto.Category,
            CreatedAt = DateTime.UtcNow,
            AuthorId = userId
        };

        _dbContext.Posts.Add(post);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, Post.ToDtoExpression.Compile().Invoke(post));
    }

    // PUT api/posts/{id}
    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanModifyPosts")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDto dto, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (post is null) return NotFound();

        // Only author or admin can update — basic check using claim 'sub' or Name
        var userId = User.Identity?.Name ?? string.Empty;
        var isAuthor = !string.IsNullOrEmpty(post.AuthorId) && post.AuthorId == userId;

        if (!isAuthor)
        {
            // For demo, require admin role to modify others
            if (!User.IsInRole("Admin")) return Forbid();
        }

        post.Title = dto.Title ?? post.Title;
        post.Content = dto.Content ?? post.Content;
        post.Category = dto.Category ?? post.Category;

        _dbContext.Posts.Update(post);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    // DELETE api/posts/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanModifyPosts")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (post is null) return NotFound();

        var userId = User.Identity?.Name ?? string.Empty;
        var isAuthor = !string.IsNullOrEmpty(post.AuthorId) && post.AuthorId == userId;
        if (!isAuthor && !User.IsInRole("Admin")) return Forbid();

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    // Likes endpoints
    // POST api/posts/{id}/likes
    [HttpPost("{id:int}/likes")]
    [Authorize]
    public async Task<IActionResult> AddLike(int id, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (post is null) return NotFound();

        var userId = User.Identity?.Name ?? string.Empty;

        var like = new Like { PostId = id, UserId = userId };
        _dbContext.Likes.Add(like);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, new { LikeId = like.Id });
    }

    // DELETE api/posts/{id}/likes/{likeId}
    [HttpDelete("{id:int}/likes/{likeId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteLike(int id, int likeId, CancellationToken cancellationToken)
    {
        var like = await _dbContext.Likes.FindAsync(new object[] { likeId }, cancellationToken).ConfigureAwait(false);
        if (like is null || like.PostId != id) return NotFound();

        var userId = User.Identity?.Name ?? string.Empty;
        if (like.UserId != userId && !User.IsInRole("Admin")) return Forbid();

        _dbContext.Likes.Remove(like);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    // Comments endpoints
    // POST api/posts/{id}/comments
    [HttpPost("{id:int}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentDto dto, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (post is null) return NotFound();

        var userId = User.Identity?.Name ?? string.Empty;

        var comment = new Comment { PostId = id, Text = dto.Text, UserId = userId, CreatedAt = DateTime.UtcNow };
        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, new { CommentId = comment.Id });
    }

    // DELETE api/posts/{id}/comments/{commentId}
    [HttpDelete("{id:int}/comments/{commentId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id, int commentId, CancellationToken cancellationToken)
    {
        var comment = await _dbContext.Comments.FindAsync(new object[] { commentId }, cancellationToken).ConfigureAwait(false);
        if (comment is null || comment.PostId != id) return NotFound();

        var userId = User.Identity?.Name ?? string.Empty;
        if (comment.UserId != userId && !User.IsInRole("Admin")) return Forbid();

        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return NoContent();
    }

    // GET api/posts/{id}/comments
    [HttpGet("{id:int}/comments")]
    public async Task<IActionResult> GetComments(int id, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Posts.AnyAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
        if (!exists) return NotFound();

        var comments = await _dbContext.Comments
            .Where(c => c.PostId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Text, c.UserId, c.CreatedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(comments);
    }

    // GET api/posts/dotnet-architecture (supports pagination)
    [HttpGet("dotnet-architecture")]
    public async Task<IActionResult> GetDotNetArchitecture(
        [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) return BadRequest("pageNumber must be >= 1");
        if (pageSize < 1 || pageSize > 100) return BadRequest("pageSize must be between 1 and 100");

        var specification = new DotNetAndArchitecturePostSpecification();
        var query = _dbContext.ApplySpecification(specification);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(Post.ToDtoExpression)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var metadata = BuildPaginationMetadata(
            actionName: nameof(GetDotNetArchitecture),
            pageNumber: pageNumber,
            pageSize: pageSize,
            totalCount: total,
            additionalRouteValues: null
        );

        return Ok(new { Items = items, Metadata = metadata });
    }

    // GET api/posts/viral?minLikes=100 (supports pagination)
    [HttpGet("viral")]
    public async Task<IActionResult> GetViral(
        [FromQuery] int? minLikes,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) return BadRequest("pageNumber must be >= 1");
        if (pageSize < 1 || pageSize > 100) return BadRequest("pageSize must be between 1 and 100");

        var specification = new ViralPostSpecification(minLikes ?? 150);
        var query = _dbContext.ApplySpecification(specification);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(Post.ToDtoExpression)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var metadata = BuildPaginationMetadata(
            actionName: nameof(GetViral),
            pageNumber: pageNumber,
            pageSize: pageSize,
            totalCount: total,
            additionalRouteValues: new { minLikes }
        );

        return Ok(new { Items = items, Metadata = metadata });
    }

    private object BuildPaginationMetadata(string actionName, int pageNumber, int pageSize, int totalCount, object? additionalRouteValues)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var hasPrevious = pageNumber > 1;
        var hasNext = pageNumber < totalPages;

        object? prevLink = null;
        object? nextLink = null;
        string? self = Url.Action(actionName, "Posts", MergeRouteValues(additionalRouteValues, new { pageNumber, pageSize }));

        if (hasPrevious)
            prevLink = Url.Action(actionName, "Posts", MergeRouteValues(additionalRouteValues, new { pageNumber = pageNumber - 1, pageSize }));
        if (hasNext)
            nextLink = Url.Action(actionName, "Posts", MergeRouteValues(additionalRouteValues, new { pageNumber = pageNumber + 1, pageSize }));

        return new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasPrevious = hasPrevious,
            HasNext = hasNext,
            Links = new { Self = self, Prev = prevLink, Next = nextLink }
        };
    }

    private static object MergeRouteValues(object? baseValues, object? overrides)
    {
        var dict = new Dictionary<string, object?>();
        if (baseValues != null)
        {
            foreach (var prop in baseValues.GetType().GetProperties())
            {
                dict[prop.Name] = prop.GetValue(baseValues);
            }
        }

        if (overrides != null)
        {
            foreach (var prop in overrides.GetType().GetProperties())
            {
                dict[prop.Name] = prop.GetValue(overrides);
            }
        }

        return dict;
    }
}
