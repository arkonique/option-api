namespace OptionPricing.Core.Strategies.Payoff;

// Describe what the payoff depends on.
// Compose freely: a payoff could implement multiple markers if needed.
public interface IVanillaPayoff : IPayoff { }       // depends only on S (spot) at this time
public interface IPathDependentPayoff : IPayoff { } // needs the path (e.g., Asian)
public interface IBarrierLikePayoff : IPayoff { }   // (future) knock-in/out, cliquet, etc.
