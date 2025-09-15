namespace OptionPricing.Core.Strategies.Exercise;

public sealed class EuropeanExercise : IExercise, INoEarlyExercise
{
    public double ValueAtNode(ExerciseArgs args)
    {
        if (args.Continuation is null)
            throw new ArgumentException("EuropeanExercise requires Continuation.", nameof(args));
        return args.Continuation.Value;
    }


}