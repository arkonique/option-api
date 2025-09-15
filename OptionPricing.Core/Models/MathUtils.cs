namespace OptionPricing.Core.Models;

public static class MathUtils
{
    private static readonly ThreadLocal<Random> _rng = new(() => new Random());
    public static double BoxMuller()
    {
        // Generate two uniform random numbers in (0,1)
        Random rand = _rng.Value!;
        double u1 = 1.0 - rand.NextDouble(); // Avoid 0
        double u2 = 1.0 - rand.NextDouble(); // Avoid 0

        // Box-Muller transform
        double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return z0; // Standard normal random variable
    }

    public static double[] SolveTridiagonal(double[] sub, double[] diag, double[] sup, double[] rhs)
    {
        int n = diag.Length;
        double[] cPrime = new double[n - 1];
        double[] dPrime = new double[n];

        cPrime[0] = sup[0] / diag[0];
        dPrime[0] = rhs[0] / diag[0];

        for (int i = 1; i < n; i++)
        {
            double denom = diag[i] - sub[i - 1] * cPrime[i - 1];
            if (i < n - 1) cPrime[i] = sup[i] / denom;
            dPrime[i] = (rhs[i] - sub[i - 1] * dPrime[i - 1]) / denom;
        }

        double[] x = new double[n];
        x[n - 1] = dPrime[n - 1];
        for (int i = n - 2; i >= 0; i--) x[i] = dPrime[i] - cPrime[i] * x[i + 1];
        return x;
    }

}