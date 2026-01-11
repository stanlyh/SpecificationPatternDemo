using SpecificationPatternDemo.Dto;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SpecificationPatternDemo;

public class Post
{
    public int Id { get; set; }
    public string? AuthorId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Like> Likes { get; } = new();
    public List<Comment> Comments { get; } = new();

    public static readonly Expression<Func<Post, PostDto>> ToDtoExpression =
        post => new PostDto(
            post.Id,
            post.AuthorId ?? string.Empty,
            post.Title ?? string.Empty,
            post.Content ?? string.Empty,
            post.Category ?? string.Empty,
            post.CreatedAt,
            post.Likes.Count,
            post.Comments.Count
        );

    public PostDto ToDto() => ToDtoExpression.Compile().Invoke(this);
}
