namespace OptionPricing.Core.Greeks;

using System;
using OptionPricing.Core.Engines;
using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;

public sealed class Rho : IGreek
{
    public double Compute(
        Option option,
        EngineFactory? factory = null,
        double relativeBump = 1e-4,
        double absoluteFloor = 1e-6
    )
    {
        if (option is null) throw new ArgumentNullException(nameof(option));
        factory ??= EngineFactory.Default;

        var accuracy = option.Exercise is IAllowsEarlyExercise
            ? EngineAccuracy.Balanced
            : EngineAccuracy.Balanced;

        var knobs = new EngineFactory.Options(accuracy);

        double r0 = option.R;
        double h  = Math.Max(Math.Abs(r0) * relativeBump, absoluteFloor);

        // Central difference on interest rate
        var up = new Option(option.S, option.K, option.T, r0 + h, option.Sigma, option.Q, option.Exercise, option.Payoff);
        var dn = new Option(option.S, option.K, option.T, r0 - h, option.Sigma, option.Q, option.Exercise, option.Payoff);

        var engU = factory.Create(up, knobs);
        var engD = factory.Create(dn, knobs);

        double Vup = engU.Price(up);
        double Vdn = engD.Price(dn);

        return (Vup - Vdn) / (2.0 * h);
    }
}
