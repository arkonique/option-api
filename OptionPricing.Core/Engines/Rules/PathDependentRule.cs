namespace OptionPricing.Core.Engines.Rules;

using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Payoff;

public sealed class PathDependentRule : IEngineRule
{
    public int Priority => 100; // high priority; very specific

    public bool Matches(Option o) => o.Payoff is IPathDependentPayoff;

    public IPricingEngine Build(Option o, EngineFactory.Options k)
    {
        // Choose sensible defaults based on accuracy, allow overrides via k
        (int steps, int paths) = k.Accuracy switch
        {
            EngineAccuracy.Fast     => (k.Steps ?? 64,   k.Paths ?? 20_000),
            EngineAccuracy.Balanced => (k.Steps ?? 128,  k.Paths ?? 50_000),
            EngineAccuracy.Accurate => (k.Steps ?? 256,  k.Paths ?? 100_000),
            _ => (k.Steps ?? 128, k.Paths ?? 50_000)
        };

        return new MonteCarloPricingEngine(steps: steps, paths: paths);
    }
}
