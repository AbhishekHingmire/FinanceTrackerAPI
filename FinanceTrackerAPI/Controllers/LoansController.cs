namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Loans;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

public class LoansController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<LoansController> _logger;

    public LoansController(FinanceTrackerDbContext dbContext, ILogger<LoansController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LoanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoans(CancellationToken ct)
    {
        var userId = GetUserId();
        var loans = await _dbContext.Loans
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.StartDate)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<LoanDto>>.SuccessResult(loans.Select(MapToDto).ToList()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoan(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var loan = await _dbContext.Loans
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

        if (loan == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Loan not found"));
        }

        return Ok(ApiResponse<LoanDto>.SuccessResult(MapToDto(loan)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LoanDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResult("Validation error", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetUserId();
        var type = dto.LoanType switch
        {
            "Home Loan" => LoanType.HomeLoan,
            "Car / Auto Loan" => LoanType.CarAutoLoan,
            "Education Loan" => LoanType.EducationLoan,
            "Personal Loan" => LoanType.PersonalLoan,
            _ => LoanType.PersonalLoan
        };

        var startDate = DateOnly.TryParse(dto.StartDate.Split('T')[0], out var s) ? s : DateOnly.FromDateTime(DateTime.UtcNow);

        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Lender = dto.LenderInstitution.Trim(),
            Type = type,
            SanctionedAmount = (long)(dto.PrincipalAmount * 100),
            OpeningOutstanding = dto.CurrentOutstanding.HasValue ? (long)(dto.CurrentOutstanding.Value * 100) : (long)(dto.PrincipalAmount * 100),
            Rate = dto.InterestRate,
            Tenure = dto.TenureMonths,
            Emi = (long)(dto.EmiAmount * 100),
            EmiDay = startDate.Day,
            StartDate = startDate,
            PayFromAccountId = dto.RepaymentAccountId,
            AutoPost = dto.AutoRecurringEmi,
            OpeningInstallmentsPaid = dto.PaidEmisCount,
            Notes = dto.Notes
        };

        _dbContext.Loans.Add(loan);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, ApiResponse<LoanDto>.SuccessResult(MapToDto(loan), "Loan liability created"));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLoan(Guid id, [FromBody] UpdateLoanDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var loan = await _dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

        if (loan == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Loan not found"));
        }

        loan.Name = dto.Name.Trim();
        loan.Lender = dto.LenderInstitution.Trim();
        loan.SanctionedAmount = (long)(dto.PrincipalAmount * 100);
        if (dto.CurrentOutstanding.HasValue)
        {
            loan.OpeningOutstanding = (long)(dto.CurrentOutstanding.Value * 100);
        }
        loan.Rate = dto.InterestRate;
        loan.Tenure = dto.TenureMonths;
        loan.Emi = (long)(dto.EmiAmount * 100);
        loan.PayFromAccountId = dto.RepaymentAccountId;
        loan.AutoPost = dto.AutoRecurringEmi;
        loan.OpeningInstallmentsPaid = dto.PaidEmisCount;
        loan.Notes = dto.Notes;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<LoanDto>.SuccessResult(MapToDto(loan), "Loan updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteLoan(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var loan = await _dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

        if (loan == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Loan not found"));
        }

        _dbContext.Loans.Remove(loan);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Loan deleted"));
    }

    [HttpPost("{id:guid}/payment")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordLoanPaymentDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var loan = await _dbContext.Loans
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

        if (loan == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Loan not found"));
        }

        var paise = (long)(dto.Amount * 100);
        var currentOutstanding = loan.OpeningOutstanding ?? loan.SanctionedAmount;
        loan.OpeningOutstanding = Math.Max(0, currentOutstanding - paise);
        if (dto.Type == "EMI")
        {
            loan.OpeningInstallmentsPaid = (loan.OpeningInstallmentsPaid ?? 0) + 1;
        }

        // Automatically record an Entry for this loan EMI
        var dateOnly = DateOnly.TryParse(dto.Date.Split('T')[0], out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = dateOnly,
            Kind = EntryKind.Expense,
            Subtype = EntrySubtype.Emi,
            Amount = paise,
            AccountId = dto.AccountId,
            Source = EntrySource.Loan,
            SourceId = loan.Id,
            Note = string.IsNullOrWhiteSpace(dto.Notes) ? $"{loan.Name} EMI Payment" : dto.Notes.Trim(),
            Merchant = loan.Lender,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<bool>.SuccessResult(true, "Loan payment recorded successfully"));
    }

    private static LoanDto MapToDto(Loan l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        LoanType = l.Type switch
        {
            LoanType.HomeLoan => "Home Loan",
            LoanType.CarAutoLoan => "Car / Auto Loan",
            LoanType.EducationLoan => "Education Loan",
            LoanType.PersonalLoan => "Personal Loan",
            _ => "Other"
        },
        LenderInstitution = l.Lender,
        PrincipalAmount = l.SanctionedAmount / 100m,
        CurrentOutstanding = (l.OpeningOutstanding ?? l.SanctionedAmount) / 100m,
        InterestRate = l.Rate,
        EmiAmount = l.Emi / 100m,
        RepaymentAccountId = l.PayFromAccountId,
        StartDate = l.StartDate.ToString("yyyy-MM-dd"),
        TenureMonths = l.Tenure,
        PaidEmisCount = l.OpeningInstallmentsPaid ?? 0,
        AutoRecurringEmi = l.AutoPost,
        Notes = l.Notes,
        UpdatedAt = DateTimeOffset.UtcNow.ToString("o")
    };
}
