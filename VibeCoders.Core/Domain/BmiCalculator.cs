namespace VibeCoders.Domain;

/// Calculates Body Mass Index (BMI) from a client's stored profile values.

public static class BmiCalculator
{
    
    /// Returns the BMI value rounded to two decimal places.
    
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
