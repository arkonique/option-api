namespace OptionPricing.Core.Engines;

using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;
using OptionPricing.Core.Strategies.Payoff;

public class FiniteDifferencePricingEngine : IPricingEngine
{
    public int TimeSteps { get; }
    public int PriceSteps { get; }

    public FiniteDifferencePricingEngine(int timeSteps = 100, int priceSteps = 100)
    {
        if (timeSteps <= 0) throw new ArgumentOutOfRangeException(nameof(timeSteps), "Time steps must be positive.");
        if (priceSteps <= 0) throw new ArgumentOutOfRangeException(nameof(priceSteps), "Price steps must be positive.");
        TimeSteps = timeSteps;
        PriceSteps = priceSteps;
    }

    public double Price(Option option)
    {
        if (option == null) throw new ArgumentNullException(nameof(option));

        // Reject Asian payoffs (engine is for vanilla Euro/American)
        string payoffName = option.Payoff.GetType().Name.ToLowerInvariant();
        if (payoffName.Contains("asian"))
            throw new NotSupportedException("FiniteDifferencePricingEngine does not support Asian payoffs.");

        bool isEuropean  = option.Exercise is EuropeanExercise
                        || option.Exercise.GetType().Name.Contains("European", StringComparison.OrdinalIgnoreCase);
        bool isAmerican  = option.Exercise.GetType().Name.Contains("American", StringComparison.OrdinalIgnoreCase);

        if (!isEuropean && !isAmerican)
            throw new NotSupportedException($"Unsupported exercise type: {option.Exercise.GetType().Name}");

        // Parameters
        double S0 = option.S, K = option.K, T = option.T, r = option.R, q = option.Q, sigma = option.Sigma;

        // Grid
        int M = PriceSteps;     // spatial nodes (0..M)
        int N = TimeSteps;      // time steps (0..N)
        if (M <= 2) throw new ArgumentOutOfRangeException(nameof(PriceSteps), "PriceSteps must be >= 3");
        if (N <= 0) throw new ArgumentOutOfRangeException(nameof(TimeSteps), "TimeSteps must be >= 1");

        double Smin = 0.0;
        double Smax = 5.0 * Math.Max(S0, K);   // tune if needed
        double dS   = (Smax - Smin) / M;
        double dt   = T / N;

        // Space grid
        double[] S = new double[M + 1];
        for (int i = 0; i <= M; i++) S[i] = Smin + i * dS;

        // Terminal condition: payoff at maturity
        double[] Vn   = new double[M + 1];     // known layer
        double[] Vnp1 = new double[M + 1];     // next layer

        for (int i = 0; i <= M; i++)
            Vn[i] = option.Payoff.Value(new PayoffArgs { CurrentPrice = S[i] });

        bool isCall = payoffName.Contains("call");

        // Coefficient & system arrays
        double[] a = new double[M + 1];
        double[] b = new double[M + 1];
        double[] c = new double[M + 1];

        double[] L = new double[M + 1];
        double[] D = new double[M + 1];
        double[] U = new double[M + 1];
        double[] RHS = new double[M + 1];

        // Backward time-march
        for (int n = N - 1; n >= 0; n--)
        {
            double t   = n * dt;       // time we are stepping to
            double tau = T - t;        // remaining to maturity

            // Dirichlet boundaries
            double V_left, V_right;
            if (isCall)
            {
                V_left  = 0.0;
                V_right = Smax * Math.Exp(-q * tau) - K * Math.Exp(-r * tau);
                if (V_right < 0.0) V_right = 0.0;
            }
            else
            {
                V_left  = K * Math.Exp(-r * tau);
                V_right = 0.0;
            }

            // CN coefficients at interior nodes (i = 1..M-1)
            for (int i = 1; i < M; i++)
            {
                double Si = S[i];
                double Ai = 0.5 * sigma * sigma * Si * Si;
                double Bi = (r - q) * Si;
                double Ci = -r;

                a[i] = 0.5 * dt * (Ai / (dS * dS) - Bi / (2.0 * dS));
                b[i] = -0.5 * dt * (2.0 * Ai / (dS * dS) - Ci);
                c[i] = 0.5 * dt * (Ai / (dS * dS) + Bi / (2.0 * dS));
            }

            // Assemble LHS bands and RHS
            for (int i = 1; i < M; i++)
            {
                L[i]   = -a[i];
                D[i]   = 1.0 - b[i];
                U[i]   = -c[i];
                RHS[i] = a[i] * Vn[i - 1] + (1.0 + b[i]) * Vn[i] + c[i] * Vn[i + 1];
            }

            // Apply boundaries (fixed)
            Vnp1[0] = V_left;
            Vnp1[M] = V_right;

            // Shift boundary contributions into RHS of first/last interior rows
            RHS[1]   -= L[1]   * Vnp1[0];
            RHS[M-1] -= U[M-1] * Vnp1[M];

            if (isEuropean)
            {
                // ---- European: single Thomas solve ----
                int Nsys = M - 1;
                double[] sub = new double[Nsys - 1];
                double[] diag = new double[Nsys];
                double[] sup = new double[Nsys - 1];
                double[] rhs = new double[Nsys];

                for (int k = 0; k < Nsys; k++)
                {
                    int i = k + 1;
                    diag[k] = D[i];
                    rhs[k] = RHS[i];
                    if (i >= 2) sub[k - 1] = L[i];
                    if (i <= M - 2) sup[k] = U[i];
                }

                double[] sol = MathUtils.SolveTridiagonal(sub, diag, sup, rhs);
                for (int k = 0; k < Nsys; k++)
                    Vnp1[k + 1] = sol[k];
            }
            else
            {
                // ---- American: CN + policy iteration ----
                double[] Pay = new double[M + 1];
                for (int i = 0; i <= M; i++)
                    Pay[i] = option.Payoff.Value(new PayoffArgs { CurrentPrice = S[i] });

                // Pack base interior system from current CN assembly
                int Nsys = M - 1;
                double[] baseSub  = new double[Nsys - 1];
                double[] baseDiag = new double[Nsys];
                double[] baseSup  = new double[Nsys - 1];
                double[] baseRhs  = new double[Nsys];

                for (int k = 0; k < Nsys; k++)
                {
                    int i = k + 1;
                    baseDiag[k] = D[i];
                    baseRhs[k]  = RHS[i];
                    if (i >= 2)     baseSub[k - 1] = L[i];
                    if (i <= M - 2) baseSup[k]     = U[i];
                }

                // 1) Get a continuation *seed*: solve the European CN once for this step
                double[] sub0  = (double[])baseSub.Clone();
                double[] diag0 = (double[])baseDiag.Clone();
                double[] sup0  = (double[])baseSup.Clone();
                double[] rhs0  = (double[])baseRhs.Clone();
                double[] cont  = MathUtils.SolveTridiagonal(sub0, diag0, sup0, rhs0);

                // Build Vguess from that continuation (plus fixed BCs)
                double[] Vguess = new double[M + 1];
                Vguess[0] = Vnp1[0];
                for (int k = 0; k < Nsys; k++) Vguess[k + 1] = cont[k];
                Vguess[M] = Vnp1[M];

                // 2) Policy iteration
                const int maxIter = 20;
                const double tol  = 1e-8;
                bool[] activeOld = new bool[M + 1];
                bool[] activeNew = new bool[M + 1];

                for (int iter = 0; iter < maxIter; iter++)
                {
                    // Start from base CN system
                    double[] sub  = (double[])baseSub.Clone();
                    double[] diag = (double[])baseDiag.Clone();
                    double[] sup  = (double[])baseSup.Clone();
                    double[] rhs  = (double[])baseRhs.Clone();

                    // Mark/Pin active nodes (where exercise dominates continuation)
                    for (int i = 1; i < M; i++)
                    {
                        activeNew[i] = Vguess[i] <= Pay[i] + 1e-12;
                        if (activeNew[i])
                        {
                            int k = i - 1;
                            diag[k] = 1.0;
                            rhs[k]  = Pay[i];
                            if (i > 1)     sub[k - 1] = 0.0;
                            if (i < M - 1) sup[k]     = 0.0;
                        }
                    }
                    activeNew[0] = true;  activeNew[M] = true;

                    // Solve
                    double[] sol = MathUtils.SolveTridiagonal(sub, diag, sup, rhs);

                    // Write back interior + keep boundaries
                    Vnp1[0] = Vnp1[0];
                    for (int k = 0; k < Nsys; k++) Vnp1[k + 1] = sol[k];
                    Vnp1[M] = Vnp1[M];

                    // Project: V ≥ Pay
                    for (int i = 1; i < M; i++) if (Vnp1[i] < Pay[i]) Vnp1[i] = Pay[i];

                    // Convergence checks
                    bool same = true;
                    double diff = 0.0;
                    for (int i = 0; i <= M; i++)
                    {
                        if (activeNew[i] != activeOld[i]) same = false;
                        diff = Math.Max(diff, Math.Abs(Vnp1[i] - Vguess[i]));
                        activeOld[i] = activeNew[i];
                        Vguess[i]    = Vnp1[i];
                    }
                    if (same && diff < tol) break;
                }

            }
            // Roll: Vn <- Vnp1
            var tmp = Vn; Vn = Vnp1; Vnp1 = tmp;
        }

        // Interpolate to S0
        if (S0 <= S[0]) return Vn[0];
        if (S0 >= S[M]) return Vn[M];

        int j = (int)Math.Floor((S0 - Smin) / dS);
        if (j < 0) j = 0;
        if (j >= M) j = M - 1;
        double w = (S0 - S[j]) / (S[j + 1] - S[j]);
        return (1.0 - w) * Vn[j] + w * Vn[j + 1];
    }

}