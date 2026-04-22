using FluentAssertions;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Repositories;
using Xunit;

namespace VibeCoders.Tests.Integration.Repositories;

public sealed class RepositoryNutritionTests : IDisposable
{
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
        using var cmd = new SqliteCommand(
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
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void InsertNutritionPlan_ShouldReturnValidPlanId()
    {
        var plan = CreateTestNutritionPlan();

        var planId = this.repository.InsertNutritionPlan(plan);

        planId.Should().BeGreaterThan(0);
    }

    [Fact]
    public void InsertNutritionPlan_ShouldInsertPlanIntoDatabase()
    {
        var plan = CreateTestNutritionPlan();

        var planId = this.repository.InsertNutritionPlan(plan);

        var exists = NutritionPlanExists(planId);
        exists.Should().BeTrue();
    }

    [Fact]
    public void InsertNutritionPlan_ShouldSaveDatesCorrectly()
    {
        var plan = new NutritionPlan
        {
            StartDate = new DateTime(2024, 1, 15),
            EndDate = new DateTime(2024, 2, 15)
        };

        var planId = this.repository.InsertNutritionPlan(plan);

        var saved = GetNutritionPlanFromDatabase(planId);
        saved.Should().NotBeNull();
        saved!.StartDate.Should().Be("2024-01-15");
        saved.EndDate.Should().Be("2024-02-15");
    }

    [Fact]
    public void InsertNutritionPlan_ShouldHandleDateTimeWithTimeComponent()
    {
        var plan = new NutritionPlan
        {
            StartDate = new DateTime(2024, 1, 15, 14, 30, 45),
            EndDate = new DateTime(2024, 2, 15, 18, 45, 30)
        };

        var planId = this.repository.InsertNutritionPlan(plan);

        var saved = GetNutritionPlanFromDatabase(planId);
        saved.Should().NotBeNull();
        saved!.StartDate.Should().Be("2024-01-15");
        saved.EndDate.Should().Be("2024-02-15");
    }

    [Fact]
    public void InsertNutritionPlan_ShouldInsertMultiplePlans()
    {
        var plan1 = CreateTestNutritionPlan();
        var plan2 = CreateTestNutritionPlan();
        var plan3 = CreateTestNutritionPlan();

        var planId1 = this.repository.InsertNutritionPlan(plan1);
        var planId2 = this.repository.InsertNutritionPlan(plan2);
        var planId3 = this.repository.InsertNutritionPlan(plan3);

        planId1.Should().BeGreaterThan(0);
        planId2.Should().BeGreaterThan(planId1);
        planId3.Should().BeGreaterThan(planId2);
    }

    [Fact]
    public void InsertMeal_ShouldInsertMealIntoDatabase()
    {
        var planId = InsertTestNutritionPlan();
        var meal = CreateTestMeal("Breakfast");

        this.repository.InsertMeal(meal, planId);

        var count = GetMealCountForPlan(planId);
        count.Should().Be(1);
    }

    [Fact]
    public void InsertMeal_ShouldSaveAllMealProperties()
    {
        var planId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Grilled Chicken Salad",
            Ingredients = new List<string> { "Chicken breast", "Lettuce", "Tomatoes", "Olive oil" },
            Instructions = "Grill chicken, chop vegetables, mix together"
        };

        this.repository.InsertMeal(meal, planId);

        var saved = GetMealFromDatabase(planId);
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
        var planId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Test Meal",
            Ingredients = new List<string> { "Ingredient 1", "Ingredient 2", "Ingredient 3" },
            Instructions = "Test instructions"
        };

        this.repository.InsertMeal(meal, planId);

        var rawIngredients = GetRawIngredientsFromDatabase(planId);
        rawIngredients.Should().Contain("Ingredient 1");
        rawIngredients.Should().Contain("Ingredient 2");
        rawIngredients.Should().Contain("[");
        rawIngredients.Should().Contain("]");
    }

    [Fact]
    public void InsertMeal_ShouldHandleEmptyIngredientsList()
    {
        var planId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Test Meal",
            Ingredients = new List<string>(),
            Instructions = "Test instructions"
        };

        this.repository.InsertMeal(meal, planId);

        var saved = GetMealFromDatabase(planId);
        saved.Should().NotBeNull();
        saved!.Ingredients.Should().BeEmpty();
    }

    [Fact]
    public void InsertMeal_ShouldHandleSpecialCharactersInIngredients()
    {
        var planId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Special Meal",
            Ingredients = new List<string> { "Ingredient with \"quotes\"", "Ingredient's special", "Item & more" },
            Instructions = "Instructions with 'quotes' and special chars"
        };

        this.repository.InsertMeal(meal, planId);

        var saved = GetMealFromDatabase(planId);
        saved.Should().NotBeNull();
        saved!.Ingredients.Should().Contain("Ingredient with \"quotes\"");
        saved.Ingredients.Should().Contain("Ingredient's special");
        saved.Ingredients.Should().Contain("Item & more");
    }

    [Fact]
    public void InsertMeal_ShouldInsertMultipleMealsForSamePlan()
    {
        var planId = InsertTestNutritionPlan();

        var breakfast = CreateTestMeal("Breakfast");
        var lunch = CreateTestMeal("Lunch");
        var dinner = CreateTestMeal("Dinner");

        this.repository.InsertMeal(breakfast, planId);
        this.repository.InsertMeal(lunch, planId);
        this.repository.InsertMeal(dinner, planId);

        var count = GetMealCountForPlan(planId);
        count.Should().Be(3);
    }

    [Fact]
    public void InsertMeal_ShouldHandleEmptyNameAndInstructions()
    {
        var planId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = string.Empty,
            Ingredients = new List<string> { "Ingredient 1" },
            Instructions = string.Empty
        };

        this.repository.InsertMeal(meal, planId);

        var saved = GetMealFromDatabase(planId);
        saved.Should().NotBeNull();
        saved!.Name.Should().BeEmpty();
        saved.Instructions.Should().BeEmpty();
    }

    [Fact]
    public void AssignNutritionPlanToClient_ShouldCreateAssignment()
    {
        InsertTestClient(1);
        var planId = InsertTestNutritionPlan();

        this.repository.AssignNutritionPlanToClient(1, planId);

        var assigned = IsNutritionPlanAssignedToClient(1, planId);
        assigned.Should().BeTrue();
    }

    [Fact]
    public void AssignNutritionPlanToClient_ShouldHandleMultiplePlansForSameClient()
    {
        InsertTestClient(1);
        var planId1 = InsertTestNutritionPlan();
        var planId2 = InsertTestNutritionPlan();
        var planId3 = InsertTestNutritionPlan();

        this.repository.AssignNutritionPlanToClient(1, planId1);
        this.repository.AssignNutritionPlanToClient(1, planId2);
        this.repository.AssignNutritionPlanToClient(1, planId3);

        var count = GetAssignedPlanCountForClient(1);
        count.Should().Be(3);
    }

    [Fact]
    public void AssignNutritionPlanToClient_ShouldHandleSamePlanForMultipleClients()
    {
        InsertTestClient(1);
        InsertTestClient(2);
        var planId = InsertTestNutritionPlan();

        this.repository.AssignNutritionPlanToClient(1, planId);
        this.repository.AssignNutritionPlanToClient(2, planId);

        IsNutritionPlanAssignedToClient(1, planId).Should().BeTrue();
        IsNutritionPlanAssignedToClient(2, planId).Should().BeTrue();
    }

    [Fact]
    public void AssignNutritionPlanToClient_ShouldIgnoreDuplicateAssignments()
    {
        InsertTestClient(1);
        var planId = InsertTestNutritionPlan();

        this.repository.AssignNutritionPlanToClient(1, planId);
        this.repository.AssignNutritionPlanToClient(1, planId);
        this.repository.AssignNutritionPlanToClient(1, planId);

        var count = GetAssignedPlanCountForClient(1);
        count.Should().Be(1);
    }

    [Fact]
    public void SaveNutritionPlanForClient_ShouldSavePlanAndMealsAndAssignment()
    {
        InsertTestClient(1);
        var plan = new NutritionPlan
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(30),
            Meals = new List<Meal>
            {
                CreateTestMeal("Breakfast"),
                CreateTestMeal("Lunch"),
                CreateTestMeal("Dinner")
            }
        };

        this.repository.SaveNutritionPlanForClient(plan, 1);

        var plans = this.repository.GetNutritionPlansForClient(1);
        plans.Should().HaveCount(1);
        plans[0].Meals.Should().HaveCount(3);
    }

    [Fact]
    public void SaveNutritionPlanForClient_ShouldHandleEmptyMealsList()
    {
        InsertTestClient(1);
        var plan = new NutritionPlan
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            Meals = new List<Meal>()
        };

        this.repository.SaveNutritionPlanForClient(plan, 1);

        var plans = this.repository.GetNutritionPlansForClient(1);
        plans.Should().HaveCount(1);
        plans[0].Meals.Should().BeEmpty();
    }

    [Fact]
    public void SaveNutritionPlanForClient_ShouldSaveMultiplePlansForClient()
    {
        InsertTestClient(1);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, 2);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(7), 3);

        this.repository.SaveNutritionPlanForClient(plan1, 1);
        this.repository.SaveNutritionPlanForClient(plan2, 1);

        var plans = this.repository.GetNutritionPlansForClient(1);
        plans.Should().HaveCount(2);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldReturnEmptyList_WhenNoPlansExist()
    {
        InsertTestClient(1);

        var plans = this.repository.GetNutritionPlansForClient(1);

        plans.Should().BeEmpty();
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldReturnAllPlansForClient()
    {
        InsertTestClient(1);
        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, 2);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(30), 3);

        this.repository.SaveNutritionPlanForClient(plan1, 1);
        this.repository.SaveNutritionPlanForClient(plan2, 1);

        var plans = this.repository.GetNutritionPlansForClient(1);

        plans.Should().HaveCount(2);
        plans[0].Meals.Should().HaveCount(2);
        plans[1].Meals.Should().HaveCount(3);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldReturnPlansOnlyForSpecificClient()
    {
        InsertTestClient(1);
        InsertTestClient(2);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, 2);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(30), 1);

        this.repository.SaveNutritionPlanForClient(plan1, 1);
        this.repository.SaveNutritionPlanForClient(plan2, 2);

        var client1Plans = this.repository.GetNutritionPlansForClient(1);
        var client2Plans = this.repository.GetNutritionPlansForClient(2);

        client1Plans.Should().HaveCount(1);
        client2Plans.Should().HaveCount(1);
        client1Plans[0].Meals.Should().HaveCount(2);
        client2Plans[0].Meals.Should().HaveCount(1);
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldOrderByStartDate()
    {
        InsertTestClient(1);

        var plan1 = CreateCompleteNutritionPlan(new DateTime(2024, 3, 1), 1);
        var plan2 = CreateCompleteNutritionPlan(new DateTime(2024, 1, 1), 1);
        var plan3 = CreateCompleteNutritionPlan(new DateTime(2024, 2, 1), 1);

        this.repository.SaveNutritionPlanForClient(plan1, 1);
        this.repository.SaveNutritionPlanForClient(plan2, 1);
        this.repository.SaveNutritionPlanForClient(plan3, 1);

        var plans = this.repository.GetNutritionPlansForClient(1);

        plans.Should().HaveCount(3);
        plans[0].StartDate.Should().Be(new DateTime(2024, 1, 1));
        plans[1].StartDate.Should().Be(new DateTime(2024, 2, 1));
        plans[2].StartDate.Should().Be(new DateTime(2024, 3, 1));
    }

    [Fact]
    public void GetNutritionPlansForClient_ShouldMapAllPropertiesCorrectly()
    {
        InsertTestClient(1);
        var startDate = new DateTime(2024, 1, 15);
        var endDate = new DateTime(2024, 2, 15);

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

        this.repository.SaveNutritionPlanForClient(plan, 1);

        var plans = this.repository.GetNutritionPlansForClient(1);

        plans.Should().HaveCount(1);
        var retrieved = plans[0];
        retrieved.PlanId.Should().BeGreaterThan(0);
        retrieved.StartDate.Date.Should().Be(startDate.Date);
        retrieved.EndDate.Date.Should().Be(endDate.Date);
        retrieved.Meals.Should().HaveCount(1);
        retrieved.Meals[0].Name.Should().Be("Test Meal");
        retrieved.Meals[0].Ingredients.Should().HaveCount(2);
        retrieved.Meals[0].Instructions.Should().Be("Test instructions");
    }

    [Fact]
    public void GetMealsForPlan_ShouldReturnEmptyList_WhenNoMealsExist()
    {
        var planId = InsertTestNutritionPlan();

        var meals = this.repository.GetMealsForPlan(planId);

        meals.Should().BeEmpty();
    }

    [Fact]
    public void GetMealsForPlan_ShouldReturnAllMealsForPlan()
    {
        var planId = InsertTestNutritionPlan();

        var meal1 = CreateTestMeal("Breakfast");
        var meal2 = CreateTestMeal("Lunch");
        var meal3 = CreateTestMeal("Dinner");

        this.repository.InsertMeal(meal1, planId);
        this.repository.InsertMeal(meal2, planId);
        this.repository.InsertMeal(meal3, planId);

        var meals = this.repository.GetMealsForPlan(planId);

        meals.Should().HaveCount(3);
    }

    [Fact]
    public void GetMealsForPlan_ShouldOrderByMealId()
    {
        var planId = InsertTestNutritionPlan();

        this.repository.InsertMeal(CreateTestMeal("First"), planId);
        this.repository.InsertMeal(CreateTestMeal("Second"), planId);
        this.repository.InsertMeal(CreateTestMeal("Third"), planId);

        var meals = this.repository.GetMealsForPlan(planId);

        meals.Should().HaveCount(3);
        meals[0].Name.Should().Be("First");
        meals[1].Name.Should().Be("Second");
        meals[2].Name.Should().Be("Third");
    }

    [Fact]
    public void GetMealsForPlan_ShouldDeserializeIngredientsCorrectly()
    {
        var planId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Complex Meal",
            Ingredients = new List<string> { "Chicken", "Rice", "Vegetables", "Spices" },
            Instructions = "Cook everything together"
        };

        this.repository.InsertMeal(meal, planId);

        var meals = this.repository.GetMealsForPlan(planId);

        meals.Should().HaveCount(1);
        meals[0].Ingredients.Should().HaveCount(4);
        meals[0].Ingredients.Should().Contain("Chicken");
        meals[0].Ingredients.Should().Contain("Rice");
        meals[0].Ingredients.Should().Contain("Vegetables");
        meals[0].Ingredients.Should().Contain("Spices");
    }

    [Fact]
    public void GetMealsForPlan_ShouldMapAllPropertiesCorrectly()
    {
        var planId = InsertTestNutritionPlan();
        var meal = new Meal
        {
            Name = "Test Meal",
            Ingredients = new List<string> { "Ingredient 1", "Ingredient 2" },
            Instructions = "Test instructions"
        };

        this.repository.InsertMeal(meal, planId);

        var meals = this.repository.GetMealsForPlan(planId);

        meals.Should().HaveCount(1);
        var retrieved = meals[0];
        retrieved.MealId.Should().BeGreaterThan(0);
        retrieved.NutritionPlanId.Should().Be(planId);
        retrieved.Name.Should().Be("Test Meal");
        retrieved.Ingredients.Should().BeEquivalentTo(new List<string> { "Ingredient 1", "Ingredient 2" });
        retrieved.Instructions.Should().Be("Test instructions");
    }

    [Fact]
    public void IntegrationTest_CompleteNutritionPlanFlow()
    {
        InsertTestClient(1);

        var plan = new NutritionPlan
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31),
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

        this.repository.SaveNutritionPlanForClient(plan, 1);

        var retrievedPlans = this.repository.GetNutritionPlansForClient(1);

        retrievedPlans.Should().HaveCount(1);
        var retrievedPlan = retrievedPlans[0];
        retrievedPlan.StartDate.Date.Should().Be(new DateTime(2024, 1, 1));
        retrievedPlan.EndDate.Date.Should().Be(new DateTime(2024, 1, 31));
        retrievedPlan.Meals.Should().HaveCount(3);
        retrievedPlan.Meals[0].Name.Should().Be("Protein Breakfast");
        retrievedPlan.Meals[1].Name.Should().Be("Healthy Lunch");
        retrievedPlan.Meals[2].Name.Should().Be("Light Dinner");
    }

    [Fact]
    public void IntegrationTest_MultipleClientsMultiplePlans()
    {
        InsertTestClient(1);
        InsertTestClient(2);

        var plan1 = CreateCompleteNutritionPlan(DateTime.Today, 2);
        var plan2 = CreateCompleteNutritionPlan(DateTime.Today.AddDays(30), 3);
        var plan3 = CreateCompleteNutritionPlan(DateTime.Today, 1);

        this.repository.SaveNutritionPlanForClient(plan1, 1);
        this.repository.SaveNutritionPlanForClient(plan2, 1);
        this.repository.SaveNutritionPlanForClient(plan3, 2);

        var client1Plans = this.repository.GetNutritionPlansForClient(1);
        var client2Plans = this.repository.GetNutritionPlansForClient(2);

        client1Plans.Should().HaveCount(2);
        client2Plans.Should().HaveCount(1);
        client1Plans[0].Meals.Should().HaveCount(2);
        client1Plans[1].Meals.Should().HaveCount(3);
        client2Plans[0].Meals.Should().HaveCount(1);
    }

    [Fact]
    public void MultipleOperations_ShouldMaintainDataIntegrity()
    {
        InsertTestClient(1);
        InsertTestClient(2);

        for (int i = 0; i < 3; i++)
        {
            var plan = CreateCompleteNutritionPlan(DateTime.Today.AddDays(i * 30), 2);
            this.repository.SaveNutritionPlanForClient(plan, 1);
        }

        for (int i = 0; i < 2; i++)
        {
            var plan = CreateCompleteNutritionPlan(DateTime.Today.AddDays(i * 30), 3);
            this.repository.SaveNutritionPlanForClient(plan, 2);
        }

        var client1Plans = this.repository.GetNutritionPlansForClient(1);
        var client2Plans = this.repository.GetNutritionPlansForClient(2);
        var totalPlans = GetTotalNutritionPlanCount();

        client1Plans.Should().HaveCount(3);
        client2Plans.Should().HaveCount(2);
        totalPlans.Should().Be(5);

        client1Plans.Should().AllSatisfy(p => p.Meals.Should().HaveCount(2));
        client2Plans.Should().AllSatisfy(p => p.Meals.Should().HaveCount(3));
    }

    private void InsertTestClient(int clientId)
    {
        using var cmd = new SqliteCommand(
            "INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (@id, @uid, 1, 75.0, 180.0)",
            this.connection);
        cmd.Parameters.AddWithValue("@id", clientId);
        cmd.Parameters.AddWithValue("@uid", clientId + 1000);
        cmd.ExecuteNonQuery();
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
            EndDate = DateTime.Today.AddDays(30)
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
        for (int i = 0; i < mealCount; i++)
        {
            meals.Add(CreateTestMeal($"Meal {i + 1}"));
        }

        return new NutritionPlan
        {
            StartDate = startDate,
            EndDate = startDate.AddDays(30),
            Meals = meals
        };
    }

    private bool NutritionPlanExists(int planId)
    {
        using var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM NUTRITION_PLAN WHERE nutrition_plan_id = @planId",
            this.connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private NutritionPlanData? GetNutritionPlanFromDatabase(int planId)
    {
        using var cmd = new SqliteCommand(
            "SELECT start_date, end_date FROM NUTRITION_PLAN WHERE nutrition_plan_id = @planId",
            this.connection);
        cmd.Parameters.AddWithValue("@planId", planId);

        using var reader = cmd.ExecuteReader();
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

    private int GetMealCountForPlan(int planId)
    {
        using var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM MEAL WHERE nutrition_plan_id = @planId",
            this.connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private MealData? GetMealFromDatabase(int planId)
    {
        using var cmd = new SqliteCommand(
            @"SELECT name, ingredients, instructions 
              FROM MEAL 
              WHERE nutrition_plan_id = @planId 
              ORDER BY meal_id DESC 
              LIMIT 1",
            this.connection);
        cmd.Parameters.AddWithValue("@planId", planId);

        using var reader = cmd.ExecuteReader();
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

    private string GetRawIngredientsFromDatabase(int planId)
    {
        using var cmd = new SqliteCommand(
            "SELECT ingredients FROM MEAL WHERE nutrition_plan_id = @planId LIMIT 1",
            this.connection);
        cmd.Parameters.AddWithValue("@planId", planId);
        return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
    }

    private bool IsNutritionPlanAssignedToClient(int clientId, int planId)
    {
        using var cmd = new SqliteCommand(
            @"SELECT COUNT(*) FROM CLIENT_NUTRITION_PLAN 
              WHERE client_id = @clientId AND nutrition_plan_id = @planId",
            this.connection);
        cmd.Parameters.AddWithValue("@clientId", clientId);
        cmd.Parameters.AddWithValue("@planId", planId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private int GetAssignedPlanCountForClient(int clientId)
    {
        using var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM CLIENT_NUTRITION_PLAN WHERE client_id = @clientId",
            this.connection);
        cmd.Parameters.AddWithValue("@clientId", clientId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int GetTotalNutritionPlanCount()
    {
        using var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM NUTRITION_PLAN",
            this.connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
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
