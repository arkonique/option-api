namespace OptionPricing.Core.Strategies.Exercise;

public interface IExercise
{
    double ValueAtNode(ExerciseArgs args);
}