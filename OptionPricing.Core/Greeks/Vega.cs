namespace OptionPricing.Core.Greeks;

using System;
using OptionPricing.Core.Engines;
using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;

public sealed class Vega : IGreek
{
    public double Compute(
        Option option,
        EngineFactory? factory = null,
        double relativeBump = 1e-4,
        double absoluteFloor = 1e-4
    )
    {
        if (option is null) throw new ArgumentNullException(nameof(option));
        factory ??= EngineFactory.Default;

        var accuracy = option.Exercise is IAllowsEarlyExercise
            ? EngineAccuracy.Balanced
            : EngineAccuracy.Balanced;

        var knobs = new EngineFactory.Options(accuracy);

        double s0 = option.Sigma;
        double h  = Math.Max(Math.Abs(s0) * relativeBump, absoluteFloor);

        if (s0 - h <= 0.0)
        {
            var up   = new Option(option.S, option.K, option.T, option.R, s0 + h, option.Q, option.Exercise, option.Payoff);

            var engU = factory.Create(up, knobs);
            double Vup = engU.Price(up);

            var eng0 = factory.Create(option, knobs);
            double V0  = eng0.Price(option);

            return (Vup - V0) / h;
        }
        else
        {
            var up = new Option(option.S, option.K, option.T, option.R, s0 + h, option.Q, option.Exercise, option.Payoff);
            var dn = new Option(option.S, option.K, option.T, option.R, s0 - h, option.Q, option.Exercise, option.Payoff);

            var engU = factory.Create(up, knobs);
            var engD = factory.Create(dn, knobs);

            double Vup = engU.Price(up);
            double Vdn = engD.Price(dn);

            return (Vup - Vdn) / (2.0 * h);
        }
    }
}
