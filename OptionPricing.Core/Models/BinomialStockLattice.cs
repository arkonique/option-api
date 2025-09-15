namespace OptionPricing.Core.Models;

public sealed class BinomialStockLattice
{
    private readonly BinomialTree _tree;
    private readonly double[][] _lattice; // 2D array to hold stock prices

    public BinomialStockLattice(BinomialTree tree)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        _lattice = BuildLattice();
    }

    private double[][] BuildLattice()
    {
        double s = _tree.S;
        double u = _tree.U;
        double d = _tree.D;
        int steps = _tree.Steps;
        double[][] lattice = new double[steps + 1][];
        for (int i = 0; i <= steps; i++)
        {
            lattice[i] = new double[i + 1];
            for (int j = 0; j <= i; j++)
            {
                lattice[i][j] = s * Math.Pow(u, j) * Math.Pow(d, i - j);
            }
        }
        return lattice;
    }

    public double? Get(int i, int j) // Get stock price at node (i, j), i: time step, j: number of up moves
    {
        if (i < 0 || i > _tree.Steps)
            return null;
        if (j < 0 || j > i)
            return null;
        return _lattice[i][j];
    }

    public override string ToString()
    {
        return string.Join("\n",
            _lattice.Select(
                row => string.Join(", ", row.Select(p => p.ToString("F2")))
            )
        );
    }
}