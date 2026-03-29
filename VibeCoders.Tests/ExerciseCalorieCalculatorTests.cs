using VibeCoders.Domain;

namespace VibeCoders.Tests;


public sealed class ExerciseCalorieCalculatorTests
{
    [Fact]
    public void Typical_values_return_correct_rounded_integer()
    {
        // MET=8, 70kg, 30 minutes → 8 * 70 * 0.5 = 280
        var result = ExerciseCalorieCalculator.Calculate(8.0, 70.0, TimeSpan.FromMinutes(30));
        Assert.Equal(280, result);
    }

    [Fact]
    public void Result_is_rounded_to_nearest_integer()
    {
        // MET=5, 70kg, 10 minutes → 5 * 70 * (10/60) = 58.333... → rounds to 58
        var result = ExerciseCalorieCalculator.Calculate(5.0, 70.0, TimeSpan.FromMinutes(10));
        Assert.Equal(58, result);
    }

    [Fact]
    public void Zero_duration_returns_zero()
    {
        var result = ExerciseCalorieCalculator.Calculate(8.0, 70.0, TimeSpan.Zero);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Zero_met_returns_zero()
    {
        var result = ExerciseCalorieCalculator.Calculate(0.0, 70.0, TimeSpan.FromMinutes(30));
        Assert.Equal(0, result);
    }

    [Fact]
    public void Zero_weight_returns_zero()
    {
        var result = ExerciseCalorieCalculator.Calculate(8.0, 0.0, TimeSpan.FromMinutes(30));
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(3.0, 60.0, 20, 60)]   // 3 * 60 * (20/60) = 60
    [InlineData(10.0, 80.0, 60, 800)]  // 10 * 80 * 1.0 = 800
    [InlineData(6.0, 75.0, 45, 338)]   // 6 * 75 * 0.75 = 337.5 → 338
    public void Various_inputs_produce_expected_calories(
        double met, double weightKg, int durationMinutes, int expected)
    {
        var result = ExerciseCalorieCalculator.Calculate(met, weightKg, TimeSpan.FromMinutes(durationMinutes));
        Assert.Equal(expected, result);
    }
}
