namespace OptionPricing.Core.Greeks;

using System;
using OptionPricing.Core.Engines;
using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;

public sealed class Theta : IGreek
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

        double T0 = option.T;
        double h  = Math.Max(Math.Abs(T0) * relativeBump, absoluteFloor);

        // theta ≈ dV/dt = - dV/dT
        if (T0 - h <= 0.0)
        {
            var upT  = new Option(option.S, option.K, T0 + h, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);

            var engUp = factory.Create(upT, knobs);
            double Vup = engUp.Price(upT);

            var eng0 = factory.Create(option, knobs);
            double V0  = eng0.Price(option);

            return (V0 - Vup) / h;
        }
        else
        {
            var dnT = new Option(option.S, option.K, T0 - h, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);
            var upT = new Option(option.S, option.K, T0 + h, option.R, option.Sigma, option.Q, option.Exercise, option.Payoff);

            var engDn = factory.Create(dnT, knobs);
            var engUp = factory.Create(upT, knobs);

            double Vdn = engDn.Price(dnT);
            double Vup = engUp.Price(upT);

            return (Vdn - Vup) / (2.0 * h);
        }
    }
}
