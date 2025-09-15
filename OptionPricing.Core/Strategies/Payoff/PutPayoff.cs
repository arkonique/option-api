namespace OptionPricing.Core.Strategies.Payoff;

public sealed class PutPayoff : IVanillaPayoff
{
    public double K { get; } // Strike price

    public PutPayoff(double K)
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
        return Math.Max(K - CurrentPrice, 0.0);
    }

}