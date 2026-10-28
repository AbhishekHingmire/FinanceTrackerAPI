namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Accounts;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Transactions;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

public class AccountsController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(FinanceTrackerDbContext dbContext, ILogger<AccountsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AccountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var userId = GetUserId();
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var entries = await _dbContext.Entries
            .AsNoTracking()
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .ToListAsync(ct);

        var dtos = accounts.Select(a => MapToDto(a, entries)).ToList();
        return Ok(ApiResponse<List<AccountDto>>.SuccessResult(dtos));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

        if (account == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Account not found"));
        }

        var entries = await _dbContext.Entries
            .AsNoTracking()
            .Where(e => e.UserId == userId && !e.IsDeleted && (e.AccountId == id || e.ToRefId == id))
            .ToListAsync(ct);

        return Ok(ApiResponse<AccountDto>.SuccessResult(MapToDto(account, entries)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResult("Validation failed", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetUserId();
        var accountType = dto.Kind switch
        {
            "Cash" => AccountType.Cash,
            "Credit card" => AccountType.CreditCard,
            "Digital wallet" => AccountType.Wallet,
            _ => AccountType.Bank
        };

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = accountType,
            Institution = dto.Institution?.Trim(),
            Identifier = dto.Identifier?.Trim(),
            OpeningBalance = (long)(dto.OpeningBalance * 100),
            Notes = dto.Notes?.Trim(),
            IsArchived = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, ApiResponse<AccountDto>.SuccessResult(MapToDto(account, new List<Entry>())));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

        if (account == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Account not found"));
        }

        account.Name = dto.Name.Trim();
        account.Institution = dto.Institution?.Trim();
        account.Identifier = dto.Identifier?.Trim();
        account.OpeningBalance = (long)(dto.OpeningBalance * 100);
        account.Notes = dto.Notes?.Trim();
        account.IsArchived = dto.Archived;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        var entries = await _dbContext.Entries
            .AsNoTracking()
            .Where(e => e.UserId == userId && !e.IsDeleted && (e.AccountId == id || e.ToRefId == id))
            .ToListAsync(ct);

        return Ok(ApiResponse<AccountDto>.SuccessResult(MapToDto(account, entries)));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveAccount(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

        if (account == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Account not found"));
        }

        account.IsArchived = true;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<bool>.SuccessResult(true, "Account archived successfully"));
    }

    [HttpPost("{id:guid}/reconcile")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reconcile(Guid id, [FromBody] ReconcileAccountDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

        if (account == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Account not found"));
        }

        var entries = await _dbContext.Entries
            .Where(e => e.UserId == userId && !e.IsDeleted && (e.AccountId == id || e.ToRefId == id))
            .ToListAsync(ct);

        var currentPaise = account.OpeningBalance +
            entries.Where(e => (e.AccountId == id && e.Kind == EntryKind.Income) || (e.ToRefId == id && e.Kind == EntryKind.Transfer)).Sum(e => e.Amount) -
            entries.Where(e => e.AccountId == id && (e.Kind == EntryKind.Expense || e.Kind == EntryKind.Transfer)).Sum(e => e.Amount);

        var targetPaise = (long)(dto.NewBalance * 100);
        var diffPaise = targetPaise - currentPaise;

        if (diffPaise == 0)
        {
            return Ok(ApiResponse<TransactionDto?>.SuccessResult(null, "Account balance is already balanced"));
        }

        // Direct update of account opening balance as requested
        // Opening balance is updated so that (OpeningBalance + existing entries net) equals targetPaise
        var netEntriesPaise = currentPaise - account.OpeningBalance;
        account.OpeningBalance = targetPaise - netEntriesPaise;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<TransactionDto>.SuccessResult(new TransactionDto
        {
            Id = Guid.NewGuid(),
            Date = DateTimeOffset.UtcNow.ToString("o"),
            Description = string.IsNullOrWhiteSpace(dto.Notes) ? $"Reconciliation for {account.Name}" : dto.Notes.Trim(),
            Type = "Adjustment",
            Amount = Math.Abs(diffPaise) / 100m,
            AccountId = id,
            PaymentMethod = "Adjustment"
        }, "Account balance reconciled successfully"));
    }

    private static AccountDto MapToDto(Account a, List<Entry> entries)
    {
        var inflows = entries
            .Where(e => (e.AccountId == a.Id && e.Kind == EntryKind.Income) || (e.ToRefId == a.Id && e.Kind == EntryKind.Transfer))
            .Sum(e => e.Amount);

        var outflows = entries
            .Where(e => e.AccountId == a.Id && (e.Kind == EntryKind.Expense || e.Kind == EntryKind.Transfer))
            .Sum(e => e.Amount);

        var currentPaise = a.OpeningBalance + inflows - outflows;

        return new AccountDto
        {
            Id = a.Id,
            Name = a.Name,
            Kind = a.Type switch
            {
                AccountType.Cash => "Cash",
                AccountType.CreditCard => "Credit card",
                AccountType.Wallet => "Digital wallet",
                _ => "Bank account"
            },
            Institution = a.Institution,
            Identifier = a.Identifier,
            OpeningBalance = a.OpeningBalance / 100m,
            CurrentBalance = currentPaise / 100m,
            Notes = a.Notes,
            Archived = a.IsArchived
        };
    }
}
