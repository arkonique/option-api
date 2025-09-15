namespace OptionPricing.Core.Strategies.Exercise;

public sealed class ExerciseArgs
{
    public double? CurrentPrice { get; set; }
    public double? Intrinsic { get; set; }
    public double? Continuation { get; set; }
    public int? Step { get; set; }
    public double? Time { get; set; }
    public double[]? Path { get; set; }
}