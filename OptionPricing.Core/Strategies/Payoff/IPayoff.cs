namespace OptionPricing.Core.Strategies.Payoff;

public interface IPayoff
{
    double Value(PayoffArgs args);
}