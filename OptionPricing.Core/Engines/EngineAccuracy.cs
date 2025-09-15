namespace OptionPricing.Core.Engines;

public enum EngineAccuracy
{
    Fast,       // fewer steps/paths (quick)
    Balanced,   // sensible default
    Accurate    // more steps/paths/grids (slower)
}