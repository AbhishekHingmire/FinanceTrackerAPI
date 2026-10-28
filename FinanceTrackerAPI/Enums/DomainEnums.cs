namespace FinanceTrackerAPI.Enums;

public enum OtpPurpose
{
    Signup = 1,
    Login = 2
}

public enum AccountType
{
    Bank = 1,
    Cash = 2,
    Wallet = 3,
    CreditCard = 4
}

public enum EntryKind
{
    Income = 1,
    Expense = 2,
    Transfer = 3
}

public enum EntryStatus
{
    Posted = 1,
    Scheduled = 2
}

public enum NeedWant
{
    Need = 1,
    Want = 2
}

public enum EntrySubtype
{
    None = 0,
    Emi = 1,
    Sip = 2,
    LumpSum = 3,
    Prepayment = 4,
    Lent = 5,
    Recovered = 6,
    Borrowed = 7,
    GoalAllocation = 8,
    Redemption = 9,
    CardPayment = 10
}

public enum EntrySource
{
    Manual = 1,
    Schedule = 2,
    Loan = 3,
    Sip = 4,
    Import = 5
}

public enum ToRefKind
{
    Account = 1,
    Holding = 2,
    Loan = 3,
    Person = 4
}

public enum LoanType
{
    HomeLoan = 1,
    PersonalLoan = 2,
    CarAutoLoan = 3,
    EducationLoan = 4,
    CreditLine = 5,
    Other = 6
}

public enum LoanMode
{
    New = 1,
    AlreadyRunning = 2
}

public enum AssetType
{
    MutualFund = 1,
    StocksEquity = 2,
    Etf = 3,
    GoldCommodities = 4,
    FixedDeposit = 5,
    Crypto = 6,
    Other = 7
}

public enum GoalKind
{
    Emergency = 1,
    Goal = 2,
    Sinking = 3
}

public enum ScheduleFrequency
{
    Weekly = 1,
    Monthly = 2,
    Yearly = 3
}
