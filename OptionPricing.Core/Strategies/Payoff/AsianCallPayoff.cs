namespace OptionPricing.Core.Strategies.Payoff;

public sealed class AsianCallPayoff : IPathDependentPayoff
{
    public double K { get; } // Strike price

    public AsianCallPayoff(double K)
    {
        if (K <= 0) throw new ArgumentOutOfRangeException(nameof(K), "Strike price must be positive.");
        this.K = K;
    }

    public double Value(PayoffArgs args)
    {
        if (args.Average is null)
            throw new ArgumentException("AveragePrice must be set in PayoffArgs.", nameof(args));
        double AveragePrice = args.Average.Value;
        if (AveragePrice < 0)
                throw new ArgumentException("AveragePrice cannot be negative.", nameof(args));
        return Math.Max(AveragePrice - K, 0.0);
    }
}