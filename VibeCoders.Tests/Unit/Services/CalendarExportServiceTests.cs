using System;
using System.Threading.Tasks;
using FluentAssertions;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.Tests.Mocks.DataFactories;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class CalendarExportServiceTests
    {
        private readonly CalendarExportService calendarExportService;

        public CalendarExportServiceTests()
        {
            this.calendarExportService = new CalendarExportService();
        }

        [Fact]
        public void GenerateCalendar_Should_ThrowArgumentNullException_When_WorkoutTemplateIsNull()
        {
            Action act = () => this.calendarExportService.GenerateCalendar(null!, 4, new[] { 1 });

            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(53)]
        public void GenerateCalendar_Should_ThrowArgumentOutOfRangeException_When_DurationWeeksIsInvalid(int invalidWeeks)
        {
            var workoutTemplate = new WorkoutTemplate { Name = "Test" };

            Action act = () => this.calendarExportService.GenerateCalendar(workoutTemplate, invalidWeeks, new[] { 1 });

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GenerateCalendar_Should_GenerateCorrectNumberOfEvents_ForMultipleWeeks()
        {
            var workoutTemplate = WorkoutTemplateFactory.CreateFullBodyTemplate();
            int weeks = 3;
            int[] days = { 1, 3, 5 };

            var result = this.calendarExportService.GenerateCalendar(workoutTemplate, weeks, days);

            int eventCount = this.CountStringOccurrences(result, "BEGIN:VEVENT");
            eventCount.Should().Be(9);
        }

        [Fact]
        public async Task SaveCalendarToDownloadsAsync_Should_ReturnNull_When_ContentIsEmpty()
        {
            var result = await this.calendarExportService.SaveCalendarToDownloadsAsync(string.Empty, "Workout");

            result.Should().BeNull();
        }

        [Theory]
        [InlineData("Upper Body!", "Upper-Body!")]
        [InlineData("Legs / Glutes", "Legs---Glutes")]
        [InlineData(null, "Workout")]
        public async Task SaveCalendarToDownloadsAsync_Should_HandleInvalidCharactersInFileName(string? inputName, string expectedSubstring)
        {
            var result = await this.calendarExportService.SaveCalendarToDownloadsAsync("test content", inputName);

            result.Should().Contain(expectedSubstring);
        }

        private int CountStringOccurrences(string text, string pattern)
        {
            int count = 0;
            int position = 0;
            while ((position = text.IndexOf(pattern, position)) != -1)
            {
                position += pattern.Length;
                count++;
            }

            return count;
        }

        [Fact]
        public void GenerateCalendar_Should_IncludeFormattedExerciseDescription_InIcsOutput()
        {
            var workoutTemplate = WorkoutTemplateFactory.CreateTemplateWithSpecificExercises();
            int[] selectedDays = { 1 };

            string expectedLine = "Deadlift - 5x5 @ 100kg";

            var result = this.calendarExportService.GenerateCalendar(workoutTemplate, 1, selectedDays);

            result.Should().Contain(expectedLine);
        }

        [Fact]
        public void GenerateCalendar_Should_ShowNoExercisesMessage_When_TemplateIsEmpty()
        {
            var emptyTemplate = new WorkoutTemplate { Name = "Empty" };
            int[] selectedDays = { 1 };

            var result = this.calendarExportService.GenerateCalendar(emptyTemplate, 1, selectedDays);

            result.Should().Contain("No exercises specified.");
        }
    }
}