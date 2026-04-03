using VibeCoders.Models;

namespace VibeCoders.Services
{
    public interface ICalendarExportService
    {
        string GenerateCalendar(WorkoutTemplate workoutTemplate, int durationWeeks, int[] selectedDays, DateTime? startDate = null);
    }
}
