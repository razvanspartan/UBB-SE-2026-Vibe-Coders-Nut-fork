namespace VibeCoders.Domain;

public static class ExerciseCalorieCalculator
{
    private const double DefaultMetabolicEquivalent = 5.0;

    private static readonly Dictionary<string, double> MetabolicEquivalentByExercise = new (StringComparer.OrdinalIgnoreCase)
    {
        { "Bench Press",            5.0 },
        { "Incline Dumbbell Press", 5.0 },
        { "Barbell Squat",          6.0 },
        { "Leg Press",              5.0 },
        { "Deadlift",               6.0 },
        { "Pull-Ups",               8.0 },
        { "Overhead Press",         5.0 },
        { "Side Laterals",          2.5 },
        { "Bicep Curls",            2.5 },
        { "Tricep Pushdowns",       2.5 },
    };

    public static double GetMetabolicEquivalent(string exerciseName) =>
        MetabolicEquivalentByExercise.TryGetValue(exerciseName, out var met) ? met : DefaultMetabolicEquivalent;

    public static int CalculateCalories(double metabolicEquivalent, double weightKg, TimeSpan durationSlice)
    {
        return (int)Math.Round(metabolicEquivalent * weightKg * durationSlice.TotalHours);
    }
}
