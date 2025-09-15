namespace OptionPricing.Core.Engines;

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;
using OptionPricing.Core.Strategies.Payoff;

public sealed class LongstaffSchwartzPricingEngine : IPricingEngine
{
    public int Steps { get; }
    public int Paths { get; }
    public int BasisDegree { get; }   // 1 -> [1,S], 2 -> [1,S,S^2], etc.

    public LongstaffSchwartzPricingEngine(int steps = 100, int paths = 10000, int basisDegree = 2)
    {
        if (steps <= 0) throw new ArgumentOutOfRangeException(nameof(steps));
        if (paths <= 0) throw new ArgumentOutOfRangeException(nameof(paths));
        if (basisDegree < 1 || basisDegree > 5) throw new ArgumentOutOfRangeException(nameof(basisDegree));
        Steps = steps;
        Paths = paths;
        BasisDegree = basisDegree;
    }

    public double Price(Option option)
    {
        if (option is null) throw new ArgumentNullException(nameof(option));
        if (option.Exercise is not AmericanExercise)
            throw new ArgumentException("Longstaff–Schwarz engine only supports American options.", nameof(option));

        double dt = option.T / Steps;
        double disc = Math.Exp(-option.R * dt);

        var generator = new GBMPathGenerator(
            S0: option.S, R: option.R, Q: option.Q, T: option.T, Sigma: option.Sigma,
            Steps: Steps, rng: MathUtils.BoxMuller
        );

        // paths[p][m] = S_p(t_m), m = 0..Steps
        var paths = new double[Paths][];
        for (int p = 0; p < Paths; p++) paths[p] = generator.GeneratePath();

        // Terminal cashflows: C_p(t_M) = payoff(S_p(T))
        var cashNext = new double[Paths];
        for (int p = 0; p < Paths; p++)
        {
            double ST = paths[p][Steps];
            cashNext[p] = option.Payoff.Value(new PayoffArgs
            {
                CurrentPrice = ST, Step = Steps, Time = option.T, Path = paths[p]
            });
        }

        // Backward induction: m = Steps-1 .. 0
        for (int m = Steps - 1; m >= 0; m--)
        {
            double tm = m * dt;

            // Targets: Y_i^(m) = disc * C_i(t_{m+1})
            var Y = new double[Paths];
            for (int p = 0; p < Paths; p++) Y[p] = disc * cashNext[p];

            // In-the-money set at t_m (only those might exercise)
            var itmIdx = new List<int>();
            for (int p = 0; p < Paths; p++)
            {
                double S = paths[p][m];
                double intrinsic = option.Payoff.Value(new PayoffArgs
                {
                    CurrentPrice = S, Step = m, Time = tm, Path = paths[p]
                });
                if (intrinsic > 0.0) itmIdx.Add(p);
            }

            // Fit OLS on ITM paths: Y ~ φ(S) via QR (stable)
            Vector<double>? beta = null;
            int K = BasisDegree + 1; // include constant
            if (itmIdx.Count >= K + 1) // overdetermined for stability
            {
                var X = Matrix<double>.Build.Dense(itmIdx.Count, K);
                var y = Vector<double>.Build.Dense(itmIdx.Count);

                for (int row = 0; row < itmIdx.Count; row++)
                {
                    int p = itmIdx[row];
                    double S = paths[p][m];
                    var phi = Basis(S);               // [1, S, S^2, ...]
                    for (int col = 0; col < K; col++) X[row, col] = phi[col];
                    y[row] = Y[p];                    // Y_i^(m)
                }

                beta = X.QR().Solve(y);              // β = argmin ||Xβ - y||₂
            }

            // Decide exercise vs continue for all paths
            var cashNow = new double[Paths];
            for (int p = 0; p < Paths; p++)
            {
                double S = paths[p][m];
                double intrinsic = option.Payoff.Value(new PayoffArgs
                {
                    CurrentPrice = S, Step = m, Time = tm, Path = paths[p]
                });

                bool exerciseNow;
                if (intrinsic <= 0.0)
                {
                    exerciseNow = false; // OTM never exercises
                }
                else if (beta is null)
                {
                    exerciseNow = false; // no stable regression -> continue
                }
                else
                {
                    var phi = Basis(S);
                    double chat = 0.0;
                    for (int k = 0; k < phi.Length; k++) chat += beta[k] * phi[k];
                    exerciseNow = intrinsic >= chat;
                }

                cashNow[p] = exerciseNow ? intrinsic : (disc * cashNext[p]);
            }

            cashNext = cashNow; // shift backward to next iteration
        }

        // cashNext now holds C_p(t_0). Average = price
        return cashNext.Average();
    }

    // Polynomial basis: [1, S, S^2, ..., S^BasisDegree]
    private double[] Basis(double S)
    {
        var phi = new double[BasisDegree + 1];
        phi[0] = 1.0;
        double pow = 1.0;
        for (int d = 1; d <= BasisDegree; d++)
        {
            pow *= S;
            phi[d] = pow;
        }
        return phi;
    }
}
