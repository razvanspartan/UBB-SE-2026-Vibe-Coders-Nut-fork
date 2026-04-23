namespace VibeCoders.ViewModels;

/// <summary>
/// Carries one-shot UI messages after a workout (e.g. progression notes) until the dashboard shows them.
/// </summary>
public sealed class WorkoutUiState
{
    public string? ProgressionHeadsUp { get; set; }
}