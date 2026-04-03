namespace VibeCoders.Domain;

public static class ExerciseCalorieCalculator
{
    
    public static int Calculate(double met, double weightKg, TimeSpan durationSlice)
    {
        return (int)Math.Round(met * weightKg * durationSlice.TotalHours);
    }
}
