using System;
using System.Linq.Expressions;

namespace SpecificationPatternDemo.Dto;

public record PostDto(
    int Id,
    string AuthorId,
    string Title,
    string Content,
    string Category,
    DateTime CreatedAt,
    int LikesCount,
    int CommentsCount
);
