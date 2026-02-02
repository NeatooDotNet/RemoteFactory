using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-value-object-factory
[Factory]
public partial record SkillMoney
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    [Create]
    public void Create(decimal amount, string currency = "USD")
    {
        Amount = amount;
        Currency = currency;
    }

    public static SkillMoney Zero => new() { Amount = 0, Currency = "USD" };

    public SkillMoney Add(SkillMoney other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new SkillMoney { Amount = Amount + other.Amount, Currency = Currency };
    }

    public static SkillMoney operator +(SkillMoney a, SkillMoney b)
    {
        return a.Add(b);
    }
}
#endregion
