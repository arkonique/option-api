using System.IO.Compression;

namespace OptionPricing.Core.Models;

public sealed class GBMPathGenerator
{
    public double S0 { get; }
    public double R { get; }
    public double Q { get; }
    public double Sigma { get; }
    public double T { get; }
    public int Steps { get; }
    private readonly Func<double> _Rng;
    public double Dt { get; }

    public GBMPathGenerator(double S0, double R, double Q, double Sigma, double T, int Steps, Func<double>? rng = null)
    {
        if (S0 <= 0) throw new ArgumentOutOfRangeException(nameof(S0), "Initial stock price must be positive.");
        if (T <= 0) throw new ArgumentOutOfRangeException(nameof(T), "Time to expiration must be positive.");
        if (Sigma <= 0) throw new ArgumentOutOfRangeException(nameof(Sigma), "Volatility must be positive.");
        if (Steps <= 0) throw new ArgumentOutOfRangeException(nameof(Steps), "Number of steps must be positive.");
        this.S0 = S0;
        this.R = R;
        this.Q = Q;
        this.Sigma = Sigma;
        this.T = T;
        this.Steps = Steps;
        Dt = T / Steps;
        _Rng = rng ?? throw new ArgumentNullException(nameof(rng), "RNG function cannot be null.");
    }

    public double[] GeneratePath()
    {
        double[] path = new double[Steps + 1];
        path[0] = S0;
        double drift = (R - Q - 0.5 * Sigma * Sigma) * Dt;
        double diffusionCoefficient = Sigma * Math.Sqrt(Dt);

        for (int i = 1; i <= Steps; i++)
        {
            var z = _Rng(); // Standard normal random variable
            double diffusion = diffusionCoefficient * z;
            path[i] = path[i - 1] * Math.Exp(drift + diffusion);
        }

        return path;
    }
}