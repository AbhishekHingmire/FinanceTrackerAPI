namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Transactions;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

public class TransactionsController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(FinanceTrackerDbContext dbContext, ILogger<TransactionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? type,
        [FromQuery] string? search,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        var query = _dbContext.Entries
            .AsNoTracking()
            .Where(e => e.UserId == userId && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(startDate) && DateOnly.TryParse(startDate.Split('T')[0], out var sDate))
        {
            query = query.Where(e => e.Date >= sDate);
        }

        if (!string.IsNullOrWhiteSpace(endDate) && DateOnly.TryParse(endDate.Split('T')[0], out var eDate))
        {
            query = query.Where(e => e.Date <= eDate);
        }

        if (accountId.HasValue && accountId.Value != Guid.Empty)
        {
            query = query.Where(e => e.AccountId == accountId.Value || e.ToRefId == accountId.Value);
        }

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Note.Contains(term) || (e.Merchant != null && e.Merchant.Contains(term)));
        }

        var entries = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        var dtos = entries.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var entry = await _dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, ct);

        if (entry == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Transaction not found"));
        }

        return Ok(ApiResponse<TransactionDto>.SuccessResult(MapToDto(entry)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResult("Validation error", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetUserId();
        var dateOnly = DateOnly.TryParse(dto.Date.Split('T')[0], out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);

        var (kind, subtype) = MapTypeToKindAndSubtype(dto.Type);
        var paise = (long)(dto.Amount * 100);

        Guid? loanIdGuid = null;
        if (!string.IsNullOrWhiteSpace(dto.LoanId) && Guid.TryParse(dto.LoanId, out var parsedLoanId))
        {
            loanIdGuid = parsedLoanId;
        }

        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = dateOnly,
            Kind = kind,
            Subtype = subtype,
            Amount = paise,
            AccountId = dto.AccountId,
            ToRefId = dto.DestinationAccountId,
            CategoryId = dto.CategoryId,
            NeedWant = dto.Need == "Want" ? NeedWant.Want : NeedWant.Need,
            Note = dto.Description.Trim(),
            Merchant = dto.Merchant?.Trim() ?? dto.Description.Trim(),
            TagsCsv = dto.Tags != null && dto.Tags.Any() ? string.Join(',', dto.Tags) : null,
            Source = dto.Recurring ? EntrySource.Schedule : EntrySource.Manual,
            SourceId = loanIdGuid,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetTransaction), new { id = entry.Id }, ApiResponse<TransactionDto>.SuccessResult(MapToDto(entry), "Transaction recorded"));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var entry = await _dbContext.Entries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, ct);

        if (entry == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Transaction not found"));
        }

        var (kind, subtype) = MapTypeToKindAndSubtype(dto.Type);
        var dateOnly = DateOnly.TryParse(dto.Date.Split('T')[0], out var d) ? d : entry.Date;

        entry.Date = dateOnly;
        entry.Kind = kind;
        entry.Subtype = subtype;
        entry.Amount = (long)(dto.Amount * 100);
        entry.AccountId = dto.AccountId;
        entry.ToRefId = dto.DestinationAccountId;
        entry.CategoryId = dto.CategoryId;
        entry.NeedWant = dto.Need == "Want" ? NeedWant.Want : NeedWant.Need;
        entry.Note = dto.Description.Trim();
        entry.Merchant = dto.Merchant?.Trim() ?? dto.Description.Trim();
        entry.TagsCsv = dto.Tags != null && dto.Tags.Any() ? string.Join(',', dto.Tags) : null;
        entry.Source = dto.Recurring ? EntrySource.Schedule : EntrySource.Manual;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<TransactionDto>.SuccessResult(MapToDto(entry), "Transaction updated"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTransaction(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var entry = await _dbContext.Entries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, ct);

        if (entry == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Transaction not found"));
        }

        entry.IsDeleted = true;
        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Transaction deleted"));
    }

    [HttpPost("bulk-delete")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteTransactionsDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var entries = await _dbContext.Entries
            .Where(e => dto.Ids.Contains(e.Id) && e.UserId == userId && !e.IsDeleted)
            .ToListAsync(ct);

        foreach (var entry in entries)
        {
            entry.IsDeleted = true;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, $"{entries.Count} transactions deleted"));
    }

    [HttpPost("bulk-reassign-category")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkReassignCategory([FromBody] BulkReassignCategoryDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var entries = await _dbContext.Entries
            .Where(e => dto.Ids.Contains(e.Id) && e.UserId == userId && !e.IsDeleted)
            .ToListAsync(ct);

        foreach (var entry in entries)
        {
            entry.CategoryId = dto.CategoryId;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, $"{entries.Count} transactions updated"));
    }

    [HttpPost("bulk-reassign-priority")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkReassignPriority([FromBody] BulkReassignPriorityDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var entries = await _dbContext.Entries
            .Where(e => dto.Ids.Contains(e.Id) && e.UserId == userId && !e.IsDeleted)
            .ToListAsync(ct);

        var needWant = dto.Priority == "Want" ? NeedWant.Want : NeedWant.Need;
        foreach (var entry in entries)
        {
            entry.NeedWant = needWant;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, $"{entries.Count} transactions updated"));
    }

    private static (EntryKind, EntrySubtype) MapTypeToKindAndSubtype(string type) => type switch
    {
        "Income" => (EntryKind.Income, EntrySubtype.None),
        "Transfer" => (EntryKind.Transfer, EntrySubtype.None),
        "EMI / Loan Payment" => (EntryKind.Expense, EntrySubtype.Emi),
        "Borrow / Loan Taken" => (EntryKind.Income, EntrySubtype.Borrowed),
        "Lend / Loan Given" => (EntryKind.Expense, EntrySubtype.Lent),
        "Debt Recovery" => (EntryKind.Income, EntrySubtype.Recovered),
        "Refund" => (EntryKind.Income, EntrySubtype.None),
        _ => (EntryKind.Expense, EntrySubtype.None)
    };

    private static TransactionDto MapToDto(Entry e)
    {
        var tags = string.IsNullOrWhiteSpace(e.TagsCsv)
            ? new List<string>()
            : e.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var type = e.Subtype switch
        {
            EntrySubtype.Emi => "EMI / Loan Payment",
            EntrySubtype.Borrowed => "Borrow / Loan Taken",
            EntrySubtype.Recovered => "Debt Recovery",
            EntrySubtype.Lent => "Lend / Loan Given",
            _ => e.Kind switch
            {
                EntryKind.Income => "Income",
                EntryKind.Transfer => "Transfer",
                _ => "Expense"
            }
        };

        return new TransactionDto
        {
            Id = e.Id,
            Date = e.Date.ToString("yyyy-MM-dd") + "T" + e.CreatedAt.ToString("HH:mm"),
            Description = e.Note,
            Type = type,
            Amount = e.Amount / 100m,
            CategoryId = e.CategoryId ?? Guid.Empty,
            AccountId = e.AccountId,
            DestinationAccountId = e.ToRefId,
            PaymentMethod = "UPI",
            Need = e.NeedWant == NeedWant.Want ? "Want" : "Need",
            Merchant = e.Merchant ?? e.Note,
            Tags = tags,
            Notes = e.Note,
            Recurring = e.Source == EntrySource.Schedule,
            LoanId = e.SourceId?.ToString()
        };
    }
}
