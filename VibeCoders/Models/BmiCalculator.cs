namespace VibeCoders.Domain;

public static class BmiCalculator
{
    
    
    public static double Calculate(double weightKg, double heightCm)
    {
        if (weightKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Weight must be greater than zero.");

        if (heightCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightCm), "Height must be greater than zero.");

        var heightMetres = heightCm / 100.0;
        var bmi = weightKg / (heightMetres * heightMetres);
        return Math.Round(bmi, 2);
    }
}
