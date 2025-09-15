namespace OptionPricing.Core.Models;

using OptionPricing.Core.Strategies.Exercise;
using OptionPricing.Core.Strategies.Payoff;
public sealed class Option
{
    public double S { get; } // Current stock price
    public double K { get; } // Strike price
    public double T { get; } // Time to expiration in years
    public double R { get; } // Risk-free interest rate
    public double Sigma { get; } // Volatility of the underlying stock
    public double Q { get; } // Dividend yield

    public IExercise Exercise { get; }
    public IPayoff Payoff { get; }

    public Option(
        double S, double K, double T, double R, double Sigma, double Q,
        IExercise exercise, IPayoff payoff)
    {
        if (S <= 0) throw new ArgumentOutOfRangeException(nameof(S), "Stock price must be positive.");
        if (K <= 0) throw new ArgumentOutOfRangeException(nameof(K), "Strike price must be positive.");
        if (T <= 0) throw new ArgumentOutOfRangeException(nameof(T), "Time to expiration must be positive.");
        if (Sigma <= 0) throw new ArgumentOutOfRangeException(nameof(Sigma), "Volatility must be positive.");
        if (Q < 0) throw new ArgumentOutOfRangeException(nameof(Q), "Dividend yield cannot be negative.");
        this.S = S;
        this.K = K;
        this.T = T;
        this.R = R;
        this.Sigma = Sigma;
        this.Q = Q;
        Exercise = exercise ?? throw new ArgumentNullException(nameof(exercise));
        Payoff = payoff ?? throw new ArgumentNullException(nameof(payoff));
    }

    public override string ToString()
    {
        return $"Option(S={S}, K={K}, T={T}, R={R}, Sigma={Sigma}, Q={Q}, Exercise={Exercise.GetType().Name}, Payoff={Payoff.GetType().Name})";
    }
}
