namespace FinanceTrackerAPI.DTOs.Categories;

using System.ComponentModel.DataAnnotations;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Expense";
    public string Icon { get; set; } = "🏷️";
    public string Color { get; set; } = "blue";
    public decimal MonthlyLimit { get; set; }
    public string? DefaultNeed { get; set; } = "Need";
    public bool Archived { get; set; }
}

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Category name is required")]
    [MaxLength(120, ErrorMessage = "Category name cannot exceed 120 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kind is required")]
    public string Kind { get; set; } = "Expense";

    [MaxLength(30)]
    public string Icon { get; set; } = "🏷️";

    [MaxLength(30)]
    public string Color { get; set; } = "blue";

    public decimal MonthlyLimit { get; set; }

    public string? DefaultNeed { get; set; }
}

public class UpdateCategoryDto
{
    [Required(ErrorMessage = "Category name is required")]
    [MaxLength(120, ErrorMessage = "Category name cannot exceed 120 characters")]
    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = "Expense";

    [MaxLength(30)]
    public string Icon { get; set; } = "🏷️";

    [MaxLength(30)]
    public string Color { get; set; } = "blue";

    public decimal MonthlyLimit { get; set; }

    public string? DefaultNeed { get; set; }

    public bool Archived { get; set; }
}
