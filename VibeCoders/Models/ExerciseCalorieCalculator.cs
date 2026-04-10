namespace VibeCoders.Domain;

public static class ExerciseCalorieCalculator
{
    private const double DefaultMet = 5.0;

    private static readonly Dictionary<string, double> MetByExercise = new (StringComparer.OrdinalIgnoreCase)
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

    public static double GetMet(string exerciseName) =>
        MetByExercise.TryGetValue(exerciseName, out var met) ? met : DefaultMet;

    public static int Calculate(double met, double weightKg, TimeSpan durationSlice)
    {
        return (int)Math.Round(met * weightKg * durationSlice.TotalHours);
    }
}
