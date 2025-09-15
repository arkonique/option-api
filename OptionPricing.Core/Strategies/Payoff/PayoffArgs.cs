namespace OptionPricing.Core.Strategies.Payoff;

public sealed class PayoffArgs
{
    public double? CurrentPrice { get; set; }
    public double[]? Path { get; set; }
    public double? Average { get; set; }
    public int? Step { get; set; }
    public double? Time { get; set; }
}