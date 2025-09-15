namespace OptionPricing.Core.Engines;

using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Payoff;
using OptionPricing.Core.Strategies.Exercise;
public sealed class BinomialTreePricingEngine : IPricingEngine
{

    public int Steps { get; }

    public BinomialTreePricingEngine(int steps = 500)
    {
        if (steps <= 0) throw new ArgumentOutOfRangeException(nameof(steps), "Number of steps must be positive.");
        Steps = steps;
    }
    public double Price(Option option)
    {
        if (option == null) throw new ArgumentNullException(nameof(option));

        if (option.Payoff is AsianCallPayoff || option.Payoff is AsianPutPayoff)
        {
            throw new NotImplementedException("Asian option pricing not implemented in BinomialTreePricingEngine.");
        }
        var tree = new BinomialTree(
            S: option.S,
            K: option.K,
            R: option.R,
            Q: option.Q,
            T: option.T,
            Sigma: option.Sigma,
            Steps: Steps
        );

        var stockLattice = new BinomialStockLattice(tree);

        var payoff = option.Payoff;
        var exercise = option.Exercise;
        double dt = tree.Dt;
        double r = option.R;
        double p = tree.P;
        double discount = Math.Exp(-r * dt);
        var memo = new double[Steps + 1][];
        for (int i = 0; i <= Steps; i++)
            memo[i] = new double[i + 1];

        // Terminal values
        for (int j = 0; j <= Steps; j++)
        {
            double? Sj = stockLattice.Get(Steps, j);
            if (Sj == null) throw new InvalidOperationException($"Stock price at node ({Steps},{j}) is out of bounds.");
            memo[Steps][j] = payoff.Value(new PayoffArgs { CurrentPrice = Sj.Value, Step = Steps, Time = option.T });
        }

        // Backward induction
        for (int i = Steps - 1; i >= 0; i--)
        {
            double time = i * dt;
            for (int j = 0; j <= i; j++)
            {
                double? Sj = stockLattice.Get(i, j);
                if (Sj == null) throw new InvalidOperationException($"Stock price at node ({i},{j}) is out of bounds.");
                double continuationValue = discount * (p * memo[i + 1][j + 1] + (1 - p) * memo[i + 1][j]);
                double intrinsicValue = payoff.Value(new PayoffArgs
                {
                    CurrentPrice = Sj.Value,
                    Step = i,
                    Time = time
                });
                double nodeValue = exercise.ValueAtNode(new ExerciseArgs
                {
                    CurrentPrice = Sj.Value,
                    Step = i,
                    Time = time,
                    Intrinsic = intrinsicValue,
                    Continuation = continuationValue
                });
                memo[i][j] = nodeValue;
            }

        }
        return memo[0][0];
    }
}