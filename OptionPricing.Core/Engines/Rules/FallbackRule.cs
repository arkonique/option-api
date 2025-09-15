namespace OptionPricing.Core.Engines.Rules;

using OptionPricing.Core.Models;

public sealed class FallbackRule : IEngineRule
{
    public int Priority => 0;                 // always last
    public bool Matches(Option o) => true;    // catch-all

    public IPricingEngine Build(Option o, OptionPricing.Core.Engines.EngineFactory.Options k)
        => new BinomialTreePricingEngine(k.Steps ?? 400);
}
