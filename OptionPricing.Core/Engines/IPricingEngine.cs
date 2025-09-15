namespace OptionPricing.Core.Engines;

using OptionPricing.Core.Models;
public interface IPricingEngine
{
    double Price(Option option);
}