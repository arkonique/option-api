namespace OptionPricing.Core.Greeks;

using System;
using OptionPricing.Core.Engines;
using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;

public sealed class Delta : IGreek
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

        // Use Balanced by default; never use Fast for early-exercise (avoids MC).
        var accuracy = option.Exercise is IAllowsEarlyExercise
            ? EngineAccuracy.Balanced
            : EngineAccuracy.Balanced;

        var knobs = new EngineFactory.Options(accuracy);

        double S0 = option.S;
        double h  = Math.Max(Math.Abs(S0) * relativeBump, absoluteFloor);

        if (S0 - h <= 0.0)
        {
            var up   = new Option(S0 + h, option.K, option.T, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);
            var engU = factory.Create(up, knobs);
            double Vup = engU.Price(up);

            var eng0 = factory.Create(option, knobs);
            double V0  = eng0.Price(option);

            return (Vup - V0) / h;
        }
        else
        {
            var up   = new Option(S0 + h, option.K, option.T, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);
            var dn   = new Option(S0 - h, option.K, option.T, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);

            var engU = factory.Create(up, knobs);
            var engD = factory.Create(dn, knobs);

            double Vup = engU.Price(up);
            double Vdn = engD.Price(dn);

            return (Vup - Vdn) / (2.0 * h);
        }
    }
}
