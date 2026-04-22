using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryNutritionTests : IDisposable
{
    private const int DefaultClientId = 1;
    private const int SecondClientId = 2;
    private const int ThirdClientId = 3;
    private const int DefaultTrainerId = 1;
    private const double DefaultWeightInKg = 75.0;
    private const double DefaultHeightInCm = 180.0;
    private const int UserIdOffset = 1000;
    private const int StandardPlanDurationInDays = 30;
    private const int WeekDurationInDays = 7;
    private const int TestYear = 2024;
    private const int FirstMealCount = 2;
    private const int SecondMealCount = 3;
    private const int SingleMealCount = 1;
    private const int ThreeMealCount = 3;
    private const int FourIngredientCount = 4;

    private readonly SqliteConnection connection;
    private readonly string connectionString;
    private readonly RepositoryNutrition repository;

    public RepositoryNutritionTests()
    {
        this.connectionString = "Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared";
        this.connection = new SqliteConnection(this.connectionString);
        this.connection.Open();

        CreateSchema(this.connection);

        this.repository = new RepositoryNutrition(this.connectionString);
    }

    public void Dispose()
    {
        this.connection?.Dispose();
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var command = new SqliteCommand(
            @"
            CREATE TABLE IF NOT EXISTS CLIENT (
                client_id  INTEGER PRIMARY KEY,
                user_id    INTEGER NOT NULL,
                trainer_id INTEGER NOT NULL,
                weight     REAL,
                height     REAL
            );

            CREATE TABLE IF NOT EXISTS NUTRITION_PLAN (
                nutrition_plan_id INTEGER PRIMARY KEY,
                start_date        TEXT NOT NULL,
                end_date          TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS MEAL (
                meal_id           INTEGER PRIMARY KEY,
                nutrition_plan_id INTEGER NOT NULL,
                name              TEXT NOT NULL,
                ingredients       TEXT NOT NULL,
                instructions      TEXT NOT NULL,
                FOREIGN KEY (nutrition_plan_id) REFERENCES NUTRITION_PLAN(nutrition_plan_id)
            );

            CREATE TABLE IF NOT EXISTS CLIENT_NUTRITION_PLAN (
                client_id         INTEGER NOT NULL,
                nutrition_plan_id INTEGER NOT NULL,
                PRIMARY KEY (client_id, nutrition_plan_id),
                FOREIGN KEY (client_id)         REFERENCES CLIENT(client_id),
                FOREIGN KEY (nutrition_plan_id) REFERENCES NUTRITION_PLAN(nutrition_plan_id)
            );", connection);
        command.ExecuteNonQuery();
    }

    [Fact]
    public void InsertNutritionPlan_ShouldReturnValidPlanId()
    {
        var plan = CreateTestNutritionPlan();

        var nutritionPlanId = this.repository.InsertNutritionPlan(plan);

        nutritionPlanId.Should().BeGreaterThan(0);
    }

    [Fact]
    public void InsertNutritionPlan_ShouldInsertPlanIntoDatabase()
    {
        var plan = CreateTestNutritionPlan();

        var nutritionPlanId = this.repository.InsertNutritionPlan(plan);

        var exists = NutritionPlanExists(nutritionPlanId);
        exists.Should().BeTrue();
    }

    [Fact]
    public void InsertNutritionPlan_ShouldSaveDatesCorrectly()
    {
        var plan = new NutritionPlan
        {
            StartDate = new DateTime(TestYear, 1, 15),
            EndDate = new DateTime(TestYear, 2, 15)
        };

        var nutritionPlanId = this.repository.InsertNutritionPlan(plan);

        var saved = GetNutritionPlanFromDatabase(nutritionPlanId);
        saved.Should().NotBeNull();
        saved!.StartDate.Should().Be("2024-01-15");
        saved.EndDate.Should().Be("2024-02-15");
    }

    [Fact]
    public void InsertNutritionPlan_ShouldHandleDateTimeWithTimeComponent()
    {
        var plan = new NutritionPlan
        {
            StartDate = new DateTime(TestYear, 1, 15, 14, 30, 45),
            EndDate = new DateTime(TestYear, 2, 15, 18, 45, 30)
        };

        var nutritionPlanId = this.repository.InsertNutritionPlan(plan);

        var saved = GetNutritionPlanFromDatabase(nutritionPlanId);
        saved.Should().NotBeNull();
        saved!.StartDate.Should().Be("2024-01-15");
        saved.EndDate.Should().Be("2024-02-15");
    }

    [Fact]
    public void InsertMeal_ShouldInsertMealIntoDatabase()
    {
        var nutritionPlanId = InsertTestNutritionPlan();
        var meal = CreateTestMeal("Breakfast");

        this.repository.InsertMeal(meal, nutritionPlanId);

        var count = GetMealCountForPlan(nutritionPlanId);
        count.Should().Be(SingleMealCount);
    }

    [Fact]
    public void InsertMeal_ShouldSaveAllMealProperties()
    {
        var nutritionPlanId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Grilled Chicken Salad",
            Ingredients = new List<string> { "Chicken breast", "Lettuce", "Tomatoes", "Olive oil" },
            Instructions = "Grill chicken, chop vegetables, mix together"
        };

        this.repository.InsertMeal(meal, nutritionPlanId);

        var saved = GetMealFromDatabase(nutritionPlanId);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Grilled Chicken Salad");
        saved.Ingredients.Should().Contain("Chicken breast");
        saved.Ingredients.Should().Contain("Lettuce");
        saved.Ingredients.Should().Contain("Tomatoes");
        saved.Ingredients.Should().Contain("Olive oil");
        saved.Instructions.Should().Be("Grill chicken, chop vegetables, mix together");
    }

    [Fact]
    public void InsertMeal_ShouldSerializeIngredientsAsJson()
    {
        var nutritionPlanId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Test Meal",
            Ingredients = new List<string> { "Ingredient 1", "Ingredient 2", "Ingredient 3" },
            Instructions = "Test instructions"
        };

        this.repository.InsertMeal(meal, nutritionPlanId);

        var rawIngredients = GetRawIngredientsFromDatabase(nutritionPlanId);
        rawIngredients.Should().Contain("Ingredient 1");
        rawIngredients.Should().Contain("Ingredient 2");
        rawIngredients.Should().Contain("[");
        rawIngredients.Should().Contain("]");
    }

    [Fact]
    public void InsertMeal_ShouldHandleEmptyIngredientsList()
    {
        var nutritionPlanId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Test Meal",
            Ingredients = new List<string>(),
            Instructions = "Test instructions"
        };

        this.repository.InsertMeal(meal, nutritionPlanId);

        var saved = GetMealFromDatabase(nutritionPlanId);
        saved.Should().NotBeNull();
        saved!.Ingredients.Should().BeEmpty();
    }

    [Fact]
    public void InsertMeal_ShouldHandleEmptyNameAndInstructions()
    {
        var nutritionPlanId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = string.Empty,
            Ingredients = new List<string> { "Ingredient 1" },
            Instructions = string.Empty
        };

        this.repository.InsertMeal(meal, nutritionPlanId);

        var saved = GetMealFromDatabase(nutritionPlanId);
        saved.Should().NotBeNull();
        saved!.Name.Should().BeEmpty();
        saved.Instructions.Should().BeEmpty();
    }

    [Fact]
    public void AssignNutritionPlanToClient_ShouldCreateAssignment()
    {
        InsertTestClient(DefaultClientId);
        var nutritionPlanId = InsertTestNutritionPlan();

        this.repository.AssignNutritionPlanToClient(DefaultClientId, nutritionPlanId);

        var assigned = IsNutritionPlanAssignedToClient(DefaultClientId, nutritionPlanId);
        assigned.Should().BeTrue();
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
        count.Should().Be(SingleMealCount);
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
        plans.Should().HaveCount(SingleMealCount);
        plans[0].Meals.Should().HaveCount(ThreeMealCount);
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
        plans.Should().HaveCount(SingleMealCount);
        plans[0].Meals.Should().BeEmpty();
    }

    [Fact]
    public void SaveNutritionPlanForClient_ShouldSaveMultiplePlansForClient()
    {
        InsertTestClient(DefaultClientId);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, FirstMealCount);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(WeekDurationInDays), SecondMealCount);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        plans.Should().HaveCount(FirstMealCount);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldReturnEmptyList_WhenNoPlansExist()
    {
        InsertTestClient(DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);

        plans.Should().BeEmpty();
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldReturnAllPlansForClient()
    {
        InsertTestClient(DefaultClientId);
        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, FirstMealCount);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(StandardPlanDurationInDays), SecondMealCount);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);

        plans.Should().HaveCount(FirstMealCount);
        plans[0].Meals.Should().HaveCount(FirstMealCount);
        plans[1].Meals.Should().HaveCount(SecondMealCount);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldReturnPlansOnlyForSpecificClient()
    {
        InsertTestClient(DefaultClientId);
        InsertTestClient(SecondClientId);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, FirstMealCount);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(StandardPlanDurationInDays), SingleMealCount);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, SecondClientId);

        var client1Plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        var client2Plans = this.repository.GetNutritionPlansForClient(SecondClientId);

        client1Plans.Should().HaveCount(SingleMealCount);
        client2Plans.Should().HaveCount(SingleMealCount);
        client1Plans[0].Meals.Should().HaveCount(FirstMealCount);
        client2Plans[0].Meals.Should().HaveCount(SingleMealCount);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldOrderByStartDate()
    {
        InsertTestClient(DefaultClientId);

        var plan1 = CreateCompleteNutritionPlan(new DateTime(TestYear, 3, 1), SingleMealCount);
        var plan2 = CreateCompleteNutritionPlan(new DateTime(TestYear, 1, 1), SingleMealCount);
        var plan3 = CreateCompleteNutritionPlan(new DateTime(TestYear, 2, 1), SingleMealCount);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan3, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);

        plans.Should().HaveCount(ThreeMealCount);
        plans[0].StartDate.Should().Be(new DateTime(TestYear, 1, 1));
        plans[1].StartDate.Should().Be(new DateTime(TestYear, 2, 1));
        plans[2].StartDate.Should().Be(new DateTime(TestYear, 3, 1));
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldMapAllPropertiesCorrectly()
    {
        InsertTestClient(DefaultClientId);
        var startDate = new DateTime(TestYear, 1, 15);
        var endDate = new DateTime(TestYear, 2, 15);

        var plan = new NutritionPlan
        {
            StartDate = startDate,
            EndDate = endDate,
            Meals = new List<Meal>
            {
                new Meal
                {
                    Name = "Test Meal",
                    Ingredients = new List<string> { "Ingredient 1", "Ingredient 2" },
                    Instructions = "Test instructions"
                }
            }
        };

        this.repository.SaveNutritionPlanForClient(plan, DefaultClientId);

        var plans = this.repository.GetNutritionPlansForClient(DefaultClientId);

        plans.Should().HaveCount(SingleMealCount);
        var retrieved = plans[0];
        retrieved.PlanId.Should().BeGreaterThan(0);
        retrieved.StartDate.Date.Should().Be(startDate.Date);
        retrieved.EndDate.Date.Should().Be(endDate.Date);
        retrieved.Meals.Should().HaveCount(SingleMealCount);
        retrieved.Meals[0].Name.Should().Be("Test Meal");
        retrieved.Meals[0].Ingredients.Should().HaveCount(FirstMealCount);
        retrieved.Meals[0].Instructions.Should().Be("Test instructions");
    }

    [Fact]
    public void GetMealsForPlan_ShouldReturnAllMealsForPlan()
    {
        var nutritionPlanId = InsertTestNutritionPlan();

        var meal1 = CreateTestMeal("Breakfast");
        var meal2 = CreateTestMeal("Lunch");
        var meal3 = CreateTestMeal("Dinner");

        this.repository.InsertMeal(meal1, nutritionPlanId);
        this.repository.InsertMeal(meal2, nutritionPlanId);
        this.repository.InsertMeal(meal3, nutritionPlanId);

        var meals = this.repository.GetMealsForPlan(nutritionPlanId);

        meals.Should().HaveCount(ThreeMealCount);
    }

    [Fact]
    public void GetMealsForPlan_ShouldOrderByMealId()
    {
        var nutritionPlanId = InsertTestNutritionPlan();

        this.repository.InsertMeal(CreateTestMeal("First"), nutritionPlanId);
        this.repository.InsertMeal(CreateTestMeal("Second"), nutritionPlanId);
        this.repository.InsertMeal(CreateTestMeal("Third"), nutritionPlanId);

        var meals = this.repository.GetMealsForPlan(nutritionPlanId);

        meals.Should().HaveCount(ThreeMealCount);
        meals[0].Name.Should().Be("First");
        meals[1].Name.Should().Be("Second");
        meals[2].Name.Should().Be("Third");
    }

    [Fact]
    public void GetMealsForPlan_ShouldDeserializeIngredientsCorrectly()
    {
        var nutritionPlanId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Complex Meal",
            Ingredients = new List<string> { "Chicken", "Rice", "Vegetables", "Spices" },
            Instructions = "Cook everything together"
        };

        this.repository.InsertMeal(meal, nutritionPlanId);

        var meals = this.repository.GetMealsForPlan(nutritionPlanId);

        meals.Should().HaveCount(SingleMealCount);
        meals[0].Ingredients.Should().HaveCount(FourIngredientCount);
        meals[0].Ingredients.Should().Contain("Chicken");
        meals[0].Ingredients.Should().Contain("Rice");
        meals[0].Ingredients.Should().Contain("Vegetables");
        meals[0].Ingredients.Should().Contain("Spices");
    }

    [Fact]
    public void GetMealsForPlan_ShouldMapAllPropertiesCorrectly()
    {
        var nutritionPlanId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Test Meal",
            Ingredients = new List<string> { "Ingredient 1", "Ingredient 2" },
            Instructions = "Test instructions"
        };

        this.repository.InsertMeal(meal, nutritionPlanId);

        var meals = this.repository.GetMealsForPlan(nutritionPlanId);

        meals.Should().HaveCount(SingleMealCount);
        var retrieved = meals[0];
        retrieved.MealId.Should().BeGreaterThan(0);
        retrieved.NutritionPlanId.Should().Be(nutritionPlanId);
        retrieved.Name.Should().Be("Test Meal");
        retrieved.Ingredients.Should().BeEquivalentTo(new List<string> { "Ingredient 1", "Ingredient 2" });
        retrieved.Instructions.Should().Be("Test instructions");
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

        retrievedPlans.Should().HaveCount(SingleMealCount);
        var retrievedPlan = retrievedPlans[0];
        retrievedPlan.StartDate.Date.Should().Be(new DateTime(TestYear, 1, 1));
        retrievedPlan.EndDate.Date.Should().Be(new DateTime(TestYear, 1, 31));
        retrievedPlan.Meals.Should().HaveCount(ThreeMealCount);
        retrievedPlan.Meals[0].Name.Should().Be("Protein Breakfast");
        retrievedPlan.Meals[1].Name.Should().Be("Healthy Lunch");
        retrievedPlan.Meals[2].Name.Should().Be("Light Dinner");
    }

    [Fact]
    public void IntegrationTest_MultipleClientsMultiplePlans()
    {
        InsertTestClient(DefaultClientId);
        InsertTestClient(SecondClientId);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, FirstMealCount);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(StandardPlanDurationInDays), SecondMealCount);
        var plan3 = CreateCompleteNutritionPlan(DateTime.Today, SingleMealCount);

        this.repository.SaveNutritionPlanForClient(plan1, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan2, DefaultClientId);
        this.repository.SaveNutritionPlanForClient(plan3, SecondClientId);

        var client1Plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        var client2Plans = this.repository.GetNutritionPlansForClient(SecondClientId);

        client1Plans.Should().HaveCount(FirstMealCount);
        client2Plans.Should().HaveCount(SingleMealCount);
        client1Plans[0].Meals.Should().HaveCount(FirstMealCount);
        client1Plans[1].Meals.Should().HaveCount(SecondMealCount);
        client2Plans[0].Meals.Should().HaveCount(SingleMealCount);
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
            var plan = CreateCompleteNutritionPlan(DateTime.Today.AddDays(planIndex * StandardPlanDurationInDays), FirstMealCount);
            this.repository.SaveNutritionPlanForClient(plan, DefaultClientId);
        }

        for (int planIndex = 0; planIndex < secondClientPlanCount; planIndex++)
        {
            var plan = CreateCompleteNutritionPlan(DateTime.Today.AddDays(planIndex * StandardPlanDurationInDays), SecondMealCount);
            this.repository.SaveNutritionPlanForClient(plan, SecondClientId);
        }

        var client1Plans = this.repository.GetNutritionPlansForClient(DefaultClientId);
        var client2Plans = this.repository.GetNutritionPlansForClient(SecondClientId);
        var totalPlans = GetTotalNutritionPlanCount();

        client1Plans.Should().HaveCount(firstClientPlanCount);
        client2Plans.Should().HaveCount(secondClientPlanCount);
        totalPlans.Should().Be(totalPlanCount);

        client1Plans.Should().AllSatisfy(plan => plan.Meals.Should().HaveCount(FirstMealCount));
        client2Plans.Should().AllSatisfy(plan => plan.Meals.Should().HaveCount(SecondMealCount));
    }

    private void InsertTestClient(int clientId)
    {
        using var command = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@clientId, @userId, @trainerId, @weight, @height)",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@userId", clientId + UserIdOffset);
        command.Parameters.AddWithValue("@trainerId", DefaultTrainerId);
        command.Parameters.AddWithValue("@weight", DefaultWeightInKg);
        command.Parameters.AddWithValue("@height", DefaultHeightInCm);
        command.ExecuteNonQuery();
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

    private bool NutritionPlanExists(int nutritionPlanId)
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM NUTRITION_PLAN WHERE nutrition_plan_id = @nutritionPlanId",
            this.connection);
        command.Parameters.AddWithValue("@nutritionPlanId", nutritionPlanId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private NutritionPlanData? GetNutritionPlanFromDatabase(int nutritionPlanId)
    {
        using var command = new SqliteCommand(
            "SELECT start_date, end_date FROM NUTRITION_PLAN WHERE nutrition_plan_id = @nutritionPlanId",
            this.connection);
        command.Parameters.AddWithValue("@nutritionPlanId", nutritionPlanId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new NutritionPlanData
        {
            StartDate = reader.GetString(0),
            EndDate = reader.GetString(1)
        };
    }

    private int GetMealCountForPlan(int nutritionPlanId)
    {
        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM MEAL WHERE nutrition_plan_id = @nutritionPlanId",
            this.connection);
        command.Parameters.AddWithValue("@nutritionPlanId", nutritionPlanId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private MealData? GetMealFromDatabase(int nutritionPlanId)
    {
        using var command = new SqliteCommand(
            @"SELECT name, ingredients, instructions 
              FROM MEAL 
              WHERE nutrition_plan_id = @nutritionPlanId 
              ORDER BY meal_id DESC 
              LIMIT 1",
            this.connection);
        command.Parameters.AddWithValue("@nutritionPlanId", nutritionPlanId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var ingredientsJson = reader.GetString(1);
        var ingredients = System.Text.Json.JsonSerializer.Deserialize<List<string>>(ingredientsJson) ?? new List<string>();

        return new MealData
        {
            Name = reader.GetString(0),
            Ingredients = ingredients,
            Instructions = reader.GetString(2)
        };
    }

    private string GetRawIngredientsFromDatabase(int nutritionPlanId)
    {
        using var command = new SqliteCommand(
            "SELECT ingredients FROM MEAL WHERE nutrition_plan_id = @nutritionPlanId LIMIT 1",
            this.connection);
        command.Parameters.AddWithValue("@nutritionPlanId", nutritionPlanId);
        return command.ExecuteScalar()?.ToString() ?? string.Empty;
    }

    private bool IsNutritionPlanAssignedToClient(int clientId, int nutritionPlanId)
    {
        using var command = new SqliteCommand(
            @"SELECT COUNT(*) FROM CLIENT_NUTRITION_PLAN 
              WHERE client_id = @clientId AND nutrition_plan_id = @nutritionPlanId",
            this.connection);
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@nutritionPlanId", nutritionPlanId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
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

    private class NutritionPlanData
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }

    private class MealData
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = new List<string>();
        public string Instructions { get; set; } = string.Empty;
    }
}
