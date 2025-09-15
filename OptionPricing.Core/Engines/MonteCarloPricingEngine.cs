namespace OptionPricing.Core.Engines;

using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;
using OptionPricing.Core.Strategies.Payoff;

public sealed class MonteCarloPricingEngine : IPricingEngine
{
    public int Steps { get; }
    public int Paths { get; }

    public MonteCarloPricingEngine(int steps = 500, int paths = 10000)
    {
        if (steps <= 0) throw new ArgumentOutOfRangeException(nameof(steps), "Number of steps must be positive.");
        if (paths <= 0) throw new ArgumentOutOfRangeException(nameof(paths), "Number of paths must be positive.");
        Steps = steps;
        Paths = paths;
    }

    public double Price(Option option)
    {
        if (option == null) throw new ArgumentNullException(nameof(option));

        if (option.Exercise is not EuropeanExercise)
        {
            throw new NotImplementedException("Only European style exercise is supported in MonteCarloPricingEngine. Try BinomialTreePricingEngine or LongstaffSchwartzPricingEngine for American style exercise.");
        }
        var generator = new GBMPathGenerator(
            S0: option.S,
            R: option.R,
            Q: option.Q,
            T: option.T,
            Sigma: option.Sigma,
            Steps: Steps,
            rng: MathUtils.BoxMuller
        );
        double payoffSum = 0.0;
        for (int i = 0; i < Paths; i++)
        {
            var path = generator.GeneratePath();
            double finalPrice = path[^1];
            double averagePrice = path.Average();
            var payoffValue = option.Payoff.Value(new PayoffArgs
            {
                CurrentPrice = finalPrice,
                Average = averagePrice,
                Step = Steps,
                Time = option.T
            });
            payoffSum += payoffValue;
        }
        var discountFactor = Math.Exp(-option.R * option.T);
        var optionPrice = payoffSum / Paths * discountFactor;
        return optionPrice;
    }
    
}