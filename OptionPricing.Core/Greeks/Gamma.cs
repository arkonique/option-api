namespace OptionPricing.Core.Greeks;

using System;
using OptionPricing.Core.Engines;
using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;

public sealed class Gamma : IGreek
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

        double S0 = option.S;
        double h  = Math.Max(Math.Abs(S0) * relativeBump, absoluteFloor);

        var eng0 = factory.Create(option, knobs);
        double V0 = eng0.Price(option);

        if (S0 - h <= 0.0)
        {
            var up1 = new Option(S0 + h,  option.K, option.T, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);
            var up2 = new Option(S0 + 2*h, option.K, option.T, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);

            var engU1 = factory.Create(up1, knobs);
            var engU2 = factory.Create(up2, knobs);

            double Vup1 = engU1.Price(up1);
            double Vup2 = engU2.Price(up2);

            return (Vup2 - 2.0 * Vup1 + V0) / (h * h);
        }
        else
        {
            var up = new Option(S0 + h, option.K, option.T, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);
            var dn = new Option(S0 - h, option.K, option.T, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);

            var engU = factory.Create(up, knobs);
            var engD = factory.Create(dn, knobs);

            double Vup = engU.Price(up);
            double Vdn = engD.Price(dn);

            return (Vup - 2.0 * V0 + Vdn) / (h * h);
        }
    }
}
