using System.Collections.Generic;
using VibeCoders.Models;

namespace VibeCoders.Tests.Mocks.DataFactories;

public static class WorkoutTemplateFactory
{
    public static WorkoutTemplate CreateFullBodyTemplate()
    {
        var template = new WorkoutTemplate
        {
            Id = 1,
            Name = "Full Body"
        };

        template.AddExercise(new TemplateExercise
        {
            Name = "Squat",
            TargetSets = 3,
            TargetReps = 10,
            MuscleGroup = MuscleGroup.LEGS 
        });

        template.AddExercise(new TemplateExercise
        {
            Name = "Bench Press",
            TargetSets = 3,
            TargetReps = 10,
            MuscleGroup = MuscleGroup.CHEST
        });

        return template;
    }

    public static WorkoutTemplate CreateTemplateWithSpecificExercises()
    {
        var template = new WorkoutTemplate
        {
            Id = 1,
            Name = "Strength Day"
        };

        template.AddExercise(new TemplateExercise
        {
            Name = "Deadlift",
            TargetSets = 5,
            TargetReps = 5,
            TargetWeight = 100
        });

        return template;
    }
}