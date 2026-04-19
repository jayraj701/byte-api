namespace Byte.Domain.Configuration;

public class PayrollRulesOptions
{
    public decimal DisputeThreshold { get; set; }
    public Dictionary<string, decimal> SiteAllowances { get; set; } = new();

    public decimal GetSiteAllowance(string site)
    {
        if (SiteAllowances.TryGetValue(site, out var rate))
            return rate;

        if (SiteAllowances.TryGetValue("Default", out var defaultRate))
            return defaultRate;

        return 0m;
    }
}
