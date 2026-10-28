namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Categories;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

public class CategoriesController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(FinanceTrackerDbContext dbContext, ILogger<CategoriesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var userId = GetUserId();
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId || c.UserId == null)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var dtos = categories.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<CategoryDto>>.SuccessResult(dtos));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResult("Validation error", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetUserId();
        var kind = dto.Kind switch
        {
            "Income" => EntryKind.Income,
            "Transfer" => EntryKind.Transfer,
            _ => EntryKind.Expense
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Kind = kind,
            Icon = dto.Icon,
            Color = dto.Color,
            MonthlyCap = dto.MonthlyLimit > 0 ? (long)(dto.MonthlyLimit * 100) : null,
            DefaultNeedWant = dto.DefaultNeed == "Want" ? NeedWant.Want : NeedWant.Need,
            IsArchived = false
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<CategoryDto>.SuccessResult(MapToDto(category), "Category created successfully"));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

        if (category == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Category not found"));
        }

        category.Name = dto.Name.Trim();
        category.Kind = dto.Kind == "Income" ? EntryKind.Income : dto.Kind == "Transfer" ? EntryKind.Transfer : EntryKind.Expense;
        category.Icon = dto.Icon;
        category.Color = dto.Color;
        category.MonthlyCap = dto.MonthlyLimit > 0 ? (long)(dto.MonthlyLimit * 100) : null;
        category.DefaultNeedWant = dto.DefaultNeed == "Want" ? NeedWant.Want : NeedWant.Need;
        category.IsArchived = dto.Archived;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<CategoryDto>.SuccessResult(MapToDto(category), "Category updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchiveCategory(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

        if (category == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Category not found"));
        }

        category.IsArchived = true;
        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Category archived"));
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Kind = c.Kind switch
        {
            EntryKind.Income => "Income",
            EntryKind.Transfer => "Transfer",
            _ => "Expense"
        },
        Icon = c.Icon,
        Color = c.Color ?? "blue",
        MonthlyLimit = (c.MonthlyCap ?? 0) / 100m,
        DefaultNeed = c.DefaultNeedWant == NeedWant.Want ? "Want" : "Need",
        Archived = c.IsArchived
    };
}
