namespace BE_AI_Tourism.Application.DTOs.Category;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
