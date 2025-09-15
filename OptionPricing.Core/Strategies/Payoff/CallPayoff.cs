namespace OptionPricing.Core.Strategies.Payoff;

public sealed class CallPayoff : IVanillaPayoff
{
    public double K { get; } // Strike price

    public CallPayoff(double K)
    {
        if (K <= 0) throw new ArgumentOutOfRangeException(nameof(K), "Strike price must be positive.");
        this.K = K;
    }

    public double Value(PayoffArgs args)
    {
        if (args.CurrentPrice is null)
            throw new ArgumentException("CurrentPrice must be set in PayoffArgs.", nameof(args));
        double CurrentPrice = args.CurrentPrice.Value;
        if (CurrentPrice < 0)
                throw new ArgumentException("CurrentPrice cannot be negative.", nameof(args));
        return Math.Max(CurrentPrice - K, 0.0);
    }
}