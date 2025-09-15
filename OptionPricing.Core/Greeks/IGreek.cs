namespace OptionPricing.Core.Greeks;

using OptionPricing.Core.Models;
using OptionPricing.Core.Engines;

public interface IGreek
{
    double Compute(
        Option option,
        EngineFactory? factory = null,
        double relativeBump = 1e-4,
        double absoluteFloor = 1e-4
    );
}
