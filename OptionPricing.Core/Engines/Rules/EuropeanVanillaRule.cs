namespace OptionPricing.Core.Engines.Rules;

using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;
using OptionPricing.Core.Strategies.Payoff;

public sealed class EuropeanVanillaRule : IEngineRule
{
    public int Priority => 80;

    public bool Matches(Option o)
        => o.Exercise is INoEarlyExercise   // European style
        && o.Payoff   is IVanillaPayoff;    // plain vanilla payoff

    public IPricingEngine Build(Option o, EngineFactory.Options k)
    {
        return k.Accuracy switch
        {
            // Quick & dirty → Monte Carlo, fewer paths
            EngineAccuracy.Fast     
                => new MonteCarloPricingEngine(steps: k.Steps ?? 64,
                                               paths: k.Paths ?? 10_000),

            // Balanced default → Binomial tree
            EngineAccuracy.Balanced 
                => new BinomialTreePricingEngine(steps: k.Steps ?? 600),

            // More accurate & Greeks-friendly → Finite Difference
            EngineAccuracy.Accurate 
                => new FiniteDifferencePricingEngine(
                       timeSteps:  k.Steps      ?? 1000,
                       priceSteps: k.PriceSteps ?? 1000),

            _ => new BinomialTreePricingEngine(steps: k.Steps ?? 600)
        };
    }
}
