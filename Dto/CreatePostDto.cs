namespace SpecificationPatternDemo.Dto;

public record CreatePostDto(
    string Title,
    string Content,
    string Category
);
