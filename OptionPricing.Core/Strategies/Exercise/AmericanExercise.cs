namespace OptionPricing.Core.Strategies.Exercise;

public sealed class AmericanExercise : IExercise, IAllowsEarlyExercise
{
    public double ValueAtNode(ExerciseArgs args)
    {
        if (args.Intrinsic is null || args.Continuation is null)
                throw new ArgumentException("AmericanExercise requires both Intrinsic and Continuation.", nameof(args));
        return Math.Max(args.Intrinsic.Value, args.Continuation.Value);
    }
}