namespace OptionPricing.Core.Engines.Rules;

using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;
using OptionPricing.Core.Strategies.Payoff;

public sealed class AmericanVanillaRule : IEngineRule
{
    public int Priority => 90; // PathDependent=100 > American=90 > European=80

    public bool Matches(Option o)
        => o.Exercise is IAllowsEarlyExercise    // American/Bermudan
        && o.Payoff   is IVanillaPayoff;         // vanilla (S-only) payoff

    public IPricingEngine Build(Option o, EngineFactory.Options k)
    {
        // Heuristic: all three (Tree, FD, LSMC) are valid for American vanilla.
        // - Fast: MC for speed (rough but acceptable).
        // - Balanced: Binomial Tree (handles dividends naturally, robust).
        // - Accurate: Finite Difference (Crank–Nicolson + LCP/PSOR/Penalty) for smoothness/Greeks.

        return k.Accuracy switch
        {
            EngineAccuracy.Fast
                => new MonteCarloPricingEngine(
                       steps: k.Steps ?? 96,
                       paths: k.Paths ?? 20_000),

            EngineAccuracy.Balanced
                => new BinomialTreePricingEngine(
                       steps: k.Steps ?? 600),

            EngineAccuracy.Accurate
                => new FiniteDifferencePricingEngine(
                       timeSteps:  k.Steps      ?? 1000,
                       priceSteps: k.PriceSteps ?? 1000),

            _ => new BinomialTreePricingEngine(steps: k.Steps ?? 600)
        };
    }
}
