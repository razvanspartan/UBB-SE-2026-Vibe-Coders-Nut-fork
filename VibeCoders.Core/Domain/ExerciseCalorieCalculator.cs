namespace VibeCoders.Domain;


public static class ExerciseCalorieCalculator
{
    /// Calculates calories burned for one exercise.
    
    public static int Calculate(double met, double weightKg, TimeSpan durationSlice)
    {
        return (int)Math.Round(met * weightKg * durationSlice.TotalHours);
    }
}
