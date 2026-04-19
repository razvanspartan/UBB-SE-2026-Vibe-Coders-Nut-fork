namespace VibeCoders.Domain;

public static class BmiCalculator
{
    private const double CentimetersInMeter = 100.0;
    private const int DecimalPlaces = 2;

    public static double CalculateBmi(double weightKg, double heightCm)
    {
        if (weightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Weight must be greater than zero.");
        }

        if (heightCm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heightCm), "Height must be greater than zero.");
        }

        var heightMetres = heightCm / CentimetersInMeter;
        var bmi = weightKg / (heightMetres * heightMetres);
        return Math.Round(bmi, DecimalPlaces);
    }
}
