namespace VibeCoders.Models;

public class NutritionPlan
{
    public int PlanId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

    public List<Meal> Meals { get; set; } = new ();
}
