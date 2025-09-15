namespace OptionPricing.Core.Engines.Rules;

using OptionPricing.Core.Models;

public interface IEngineRule
{
    // Higher number wins if multiple rules match
    int Priority { get; }

    // Return true if this rule knows how to price the given option
    bool Matches(Option option);

    // Build and return the appropriate engine for this option
    IPricingEngine Build(Option option, EngineFactory.Options knobs);
}
