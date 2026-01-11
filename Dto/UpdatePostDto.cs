namespace SpecificationPatternDemo.Dto;

public record UpdatePostDto(
    string? Title,
    string? Content,
    string? Category
);
