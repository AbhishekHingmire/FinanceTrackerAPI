namespace FinanceTrackerAPI.Data;

using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

public static class CategoryDefaults
{
    public static List<Category> GetDefaultCategories(Guid userId)
    {
        return new List<Category>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Food & dining", Kind = EntryKind.Expense, Icon = "🍽️", Color = "rose", MonthlyCap = 1200000, DefaultNeedWant = NeedWant.Need },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Housing & Rent", Kind = EntryKind.Expense, Icon = "🏠", Color = "blue", MonthlyCap = 2500000, DefaultNeedWant = NeedWant.Need },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Transport & Fuel", Kind = EntryKind.Expense, Icon = "🚗", Color = "teal", MonthlyCap = 600000, DefaultNeedWant = NeedWant.Need },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Shopping & Personal", Kind = EntryKind.Expense, Icon = "🛍️", Color = "violet", MonthlyCap = 800000, DefaultNeedWant = NeedWant.Want },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Bills & Utilities", Kind = EntryKind.Expense, Icon = "💡", Color = "amber", MonthlyCap = 700000, DefaultNeedWant = NeedWant.Need },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Healthcare", Kind = EntryKind.Expense, Icon = "🏥", Color = "green", MonthlyCap = 500000, DefaultNeedWant = NeedWant.Need },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Entertainment", Kind = EntryKind.Expense, Icon = "🎬", Color = "pink", MonthlyCap = 400000, DefaultNeedWant = NeedWant.Want },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Salary", Kind = EntryKind.Income, Icon = "💼", Color = "green", MonthlyCap = null },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Freelance & Consulting", Kind = EntryKind.Income, Icon = "💻", Color = "blue", MonthlyCap = null },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Dividends & Returns", Kind = EntryKind.Income, Icon = "📈", Color = "teal", MonthlyCap = null },
            new() { Id = Guid.NewGuid(), UserId = userId, Name = "Loan EMI & Debt", Kind = EntryKind.Expense, Icon = "💳", Color = "rose", MonthlyCap = 4500000, DefaultNeedWant = NeedWant.Need }
        };
    }
}
