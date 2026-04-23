using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using VibeCoders.Tests.Mocks.DataFactories;
using VibeCoders.Tests.Mocks.DataFactories.dbSchema;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryNutritionTests : IDisposable
{
    private const int DefaultClientId = 1;
    private const int SecondClientId = 2;
    private const int StandardPlanDurationInDays = 30;
    private const int WeekDurationInDays = 7;
    private const int TestYear = 2024;
    private const int TwoMealsPerDay = 2;
    private const int ThreeMealsPerDay = 3;
    private const int OnePlan = 1;
    private const int TwoPlans = 2;
    private const int ThreePlans = 3;

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryNutrition repository;
    private readonly TestDataHelper testDataHelper;

    public RepositoryNutritionTests()
    {
        this.connectionString = TestDatabaseSchema.CreateSharedInMemoryConnectionString();
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        TestDatabaseSchema.CreateSchema(this.connection);
        this.testDataHelper = new TestDataHelper(this.connection);
        this.testDataHelper.SetupTrainer();

        this.repository = new RepositoryNutrition(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    [Fact]
    public void AssignNutritionPlanToClient_ShouldIgnoreDuplicateAssignments()
    {
        InsertTestClient(DefaultClientId);
        var nutritionPlanId = InsertTestNutritionPlan();

        this.repository.AssignNutritionPlanToClient(DefaultClientId, nutritionPlanId);
        this.repository.AssignNutritionPlanToClient(DefaultClientId, nutritionPlanId);
        this.repository.AssignNutritionPlanToClient(DefaultClientId, nutritionPlanId);

        var count = GetAssignedPlanCountForClient(DefaultClientId);
        count.Should().Be(OnePlan);
    }

    [Fact]
    public void SaveNutritionPlanForClient_ShouldSavePlanAndMealsAndAssignment()
    {
        InsertTestClient(DefaultClientId);
        var plan = new NutritionPlan
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(StandardPlanDurationInDays),
            Meals = new List<Meal>
            {
                CreateTestMeal("Breakfast"),
                CreateTestMeal("Lunch"),
                CreateTestMeal("Dinner")
            }
        };

        this.repository.SaveNutritionPlanForClient(plan, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        plans.Should().HaveCount(OnePlan);
        plans[0].Meals.Should().HaveCount(ThreeMealsPerDay);
    }

    [Fact]
    public void SaveNutritionPlanForClient_ShouldHandleEmptyMealsList()
    {
        InsertTestClient(DefaultClientId);
        var plan = new NutritionPlan
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(WeekDurationInDays),
            Meals = new List<Meal>()
        };

        this.repository.SaveNutritionPlanForClient(plan, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        plans.Should().HaveCount(OnePlan);
        plans[0].Meals.Should().BeEmpty();
    }

    [Fact]
    public void SaveNutritionPlanForClient_ShouldSaveMultiplePlansForClient()
    {
        InsertTestClient(DefaultClientId);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, TwoMealsPerDay);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(WeekDurationInDays), ThreeMealsPerDay);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        plans.Should().HaveCount(TwoPlans);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldReturnPlansOnlyForSpecificClient()
    {
        InsertTestClient(DefaultClientId);
        InsertTestClient(SecondClientId);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, TwoMealsPerDay);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(StandardPlanDurationInDays), TwoMealsPerDay);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, SecondClientId);

        var client1Plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        var client2Plans = this.repository.GetNutritionPlansForClient(SecondClientId);

        client1Plans.Should().HaveCount(OnePlan);
        client2Plans.Should().HaveCount(OnePlan);
        client1Plans[0].Meals.Should().HaveCount(TwoMealsPerDay);
        client2Plans[0].Meals.Should().HaveCount(TwoMealsPerDay);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldOrderByStartDate()
    {
        InsertTestClient(DefaultClientId);

        var plan1 = CreateCompleteNutritionPlan(new DateTime(TestYear, 3, 1), TwoMealsPerDay);
        var plan2 = CreateCompleteNutritionPlan(new DateTime(TestYear, 1, 1), TwoMealsPerDay);
        var plan3 = CreateCompleteNutritionPlan(new DateTime(TestYear, 2, 1), TwoMealsPerDay);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan3, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);

        plans.Should().HaveCount(ThreePlans);
        plans[0].StartDate.Should().Be(new DateTime(TestYear, 1, 1));
        plans[1].StartDate.Should().Be(new DateTime(TestYear, 2, 1));
        plans[2].StartDate.Should().Be(new DateTime(TestYear, 3, 1));
    }

    [Fact]
    public void IntegrationTest_CompleteNutritionPlanFlow()
    {
        InsertTestClient(DefaultClientId);

        var plan = new NutritionPlan
        {
            StartDate = new DateTime(TestYear, 1, 1),
            EndDate = new DateTime(TestYear, 1, 31),
            Meals = new List<Meal>
            {
                new Meal
                {
                    Name = "Protein Breakfast",
                    Ingredients = new List<string> { "Eggs", "Bacon", "Toast" },
                    Instructions = "Cook eggs and bacon, serve with toast"
                },
                new Meal
                {
                    Name = "Healthy Lunch",
                    Ingredients = new List<string> { "Grilled chicken", "Salad", "Olive oil" },
                    Instructions = "Grill chicken and serve with fresh salad"
                },
                new Meal
                {
                    Name = "Light Dinner",
                    Ingredients = new List<string> { "Fish", "Vegetables", "Lemon" },
                    Instructions = "Bake fish with vegetables and lemon"
                }
            }
        };

        this.repository.SaveNutritionPlanForClient(plan, DefaultClientId);

        var retrievedPlans = this.repository.GetNutritionPlansForClient(DefaultClientId);

        retrievedPlans.Should().HaveCount(OnePlan);
        var retrievedPlan = retrievedPlans[0];
        retrievedPlan.StartDate.Date.Should().Be(new DateTime(TestYear, 1, 1));
        retrievedPlan.EndDate.Date.Should().Be(new DateTime(TestYear, 1, 31));
        retrievedPlan.Meals.Should().HaveCount(ThreeMealsPerDay);
        retrievedPlan.Meals[0].Name.Should().Be("Protein Breakfast");
        retrievedPlan.Meals[1].Name.Should().Be("Healthy Lunch");
        retrievedPlan.Meals[2].Name.Should().Be("Light Dinner");
    }

    [Fact]
    public void IntegrationTest_MultipleClientsMultiplePlans()
    {
        InsertTestClient(DefaultClientId);
        InsertTestClient(SecondClientId);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, TwoMealsPerDay);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(StandardPlanDurationInDays), ThreeMealsPerDay);
        var plan3 = CreateCompleteNutritionPlan(DateTime.Today, TwoMealsPerDay);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan3, SecondClientId);

        var client1Plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        var client2Plans = this.repository.GetNutritionPlansForClient(SecondClientId);

        client1Plans.Should().HaveCount(TwoPlans);
        client2Plans.Should().HaveCount(OnePlan);
        client1Plans[0].Meals.Should().HaveCount(TwoMealsPerDay);
        client1Plans[1].Meals.Should().HaveCount(ThreeMealsPerDay);
        client2Plans[0].Meals.Should().HaveCount(TwoMealsPerDay);
    }

    [Fact]
    public void MultipleOperations_ShouldMaintainDataIntegrity()
    {
        const int firstClientPlanCount = 3;
        const int secondClientPlanCount = 2;
        const int totalPlanCount = 5;

        InsertTestClient(DefaultClientId);
        InsertTestClient(SecondClientId);

        for (int planIndex = 0; planIndex < firstClientPlanCount; planIndex++)
        {
            var plan = CreateCompleteNutritionPlan(DateTime.Today.AddDays(planIndex * StandardPlanDurationInDays), TwoMealsPerDay);
            this.repository.SaveNutritionPlanForClient(plan, DefaultClientId);
        }

        for (int planIndex = 0; planIndex < secondClientPlanCount; planIndex++)
        {
            var plan = CreateCompleteNutritionPlan(DateTime.Today.AddDays(planIndex * StandardPlanDurationInDays), ThreeMealsPerDay);
            this.repository.SaveNutritionPlanForClient(plan, SecondClientId);
        }

        var client1Plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        var client2Plans = this.repository.GetNutritionPlansForClient(SecondClientId);
        var totalPlans = GetTotalNutritionPlanCount();

        client1Plans.Should().HaveCount(firstClientPlanCount);
        client2Plans.Should().HaveCount(secondClientPlanCount);
        totalPlans.Should().Be(totalPlanCount);

        client1Plans.Should().AllSatisfy(plan => plan.Meals.Should().HaveCount(TwoMealsPerDay));
        client2Plans.Should().AllSatisfy(plan => plan.Meals.Should().HaveCount(ThreeMealsPerDay));
    }

    private void InsertTestClient(int clientId)
    {
        this.testDataHelper.InsertClient(clientId);
    }

    private int InsertTestNutritionPlan()
    {
        var plan = CreateTestNutritionPlan();
        return this.repository.InsertNutritionPlan(plan);
    }

    private NutritionPlan CreateTestNutritionPlan()
    {
        return new NutritionPlan
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(StandardPlanDurationInDays)
        };
    }

    private Meal CreateTestMeal(string name)
    {
        return new Meal
        {
            Name = name,
            Ingredients = new List<string> { "Ingredient 1", "Ingredient 2", "Ingredient 3" },
            Instructions = $"Instructions for {name}"
        };
    }

    private NutritionPlan CreateCompleteNutritionPlan(DateTime startDate, int mealCount)
    {
        var meals = new List<Meal>();
        for (int mealIndex = 0; mealIndex < mealCount; mealIndex++)
        {
            meals.Add(CreateTestMeal($"Meal {mealIndex + 1}"));
        }

        return new NutritionPlan
        {
            StartDate = startDate,
            EndDate = startDate.AddDays(StandardPlanDurationInDays),
            Meals = meals
        };
    }

    private int GetAssignedPlanCountForClient(int clientId)
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM CLIENT_NUTRITION_PLAN WHERE client_id = @clientId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private int GetTotalNutritionPlanCount()
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM NUTRITION_PLAN",
            this.connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
