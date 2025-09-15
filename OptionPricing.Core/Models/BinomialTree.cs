namespace OptionPricing.Core.Models;

public sealed class BinomialTree
{
    // Given parameters
    public double S { get; } // Current stock price
    public double K { get; } // Strike price
    public double R { get; } // Risk-free interest rate
    public double Q { get; } // Dividend yield
    public double T { get; } // Time to expiration in years
    public double Sigma { get; } // Volatility of the underlying stock
    public int Steps { get; } // Number of time steps

    // Derived parameters
    public double Dt { get; } // Time step
    public double U { get; } // Up factor
    public double D { get; } // Down factor
    public double P { get; } // Risk-neutral probability of up move

    public BinomialTree(double S, double K, double T, double R, double Sigma, double Q, int Steps)
    {
        if (Steps <= 0) throw new ArgumentOutOfRangeException(nameof(Steps), "Number of steps must be positive.");
        if (T <= 0) throw new ArgumentOutOfRangeException(nameof(T), "Time to expiration must be positive.");
        if (Sigma <= 0) throw new ArgumentOutOfRangeException(nameof(Sigma), "Volatility must be positive.");
        if (S <= 0) throw new ArgumentOutOfRangeException(nameof(S), "Stock price must be positive.");
        if (K <= 0) throw new ArgumentOutOfRangeException(nameof(K), "Strike price must be positive.");
        if (Q < 0) throw new ArgumentOutOfRangeException(nameof(Q), "Dividend yield cannot be negative.");

        this.S = S;
        this.K = K;
        this.T = T;
        this.R = R;
        this.Sigma = Sigma;
        this.Q = Q;
        this.Steps = Steps;

        Dt = T / Steps;
        U = Math.Exp(Sigma * Math.Sqrt(Dt));
        D = 1 / U;
        P = (Math.Exp((R - Q) * Dt) - D) / (U - D);

        if (P < 0 || P > 1)
            throw new ArgumentException("Invalid parameters leading to risk-neutral probability out of bounds. Try adjusting the number of steps, volatility, or interest rate.");
    }

    public override string ToString()
    {
        return $"BinomialTree(S={S}, K={K}, T={T}, r={R}, sigma={Sigma}, q={Q}, steps={Steps}, dt={Dt}, u={U}, d={D}, p={P})";
    }
}