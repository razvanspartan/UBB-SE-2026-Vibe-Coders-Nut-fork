using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Services;
using VibeCoders.ViewModels;
using Xunit;

namespace VibeCoders.Tests.Unit.ViewModels
{
    public class CalendarIntegrationViewModelTests
    {
        private readonly ICalendarWorkoutCatalogService workoutCatalogService;
        private readonly ICalendarExportService calendarExportService;
        private readonly IUserSession userSession;
        private readonly CalendarIntegrationViewModel sut;

        public CalendarIntegrationViewModelTests()
        {
            this.workoutCatalogService = Substitute.For<ICalendarWorkoutCatalogService>();
            this.calendarExportService = Substitute.For<ICalendarExportService>();
            this.userSession = Substitute.For<IUserSession>();

            this.userSession.CurrentClientId.Returns(1);
            this.workoutCatalogService.GetFallbackWorkouts(1).Returns(new List<WorkoutTemplate>());

            this.sut = new CalendarIntegrationViewModel(this.workoutCatalogService, this.calendarExportService, this.userSession);
        }

        [Fact]
        public void InitializeDaySelection_SetsDefaultDaysCorrectly()
        {
            this.sut.SelectedDays.Should().HaveCount(7);

            this.sut.SelectedDays[0].IsSelected.Should().BeFalse(); 
            this.sut.SelectedDays[1].IsSelected.Should().BeTrue();  
            this.sut.SelectedDays[2].IsSelected.Should().BeTrue();  
            this.sut.SelectedDays[3].IsSelected.Should().BeTrue();  
            this.sut.SelectedDays[4].IsSelected.Should().BeTrue();  
            this.sut.SelectedDays[5].IsSelected.Should().BeTrue();  
            this.sut.SelectedDays[6].IsSelected.Should().BeFalse(); 
        }

        [Fact]
        public async Task LoadAvailableWorkoutsAsync_Success_PopulatesWorkouts()
        {
            var workouts = new List<WorkoutTemplate>
            {
                new WorkoutTemplate { Id = 1, Name = "Workout A" },
                new WorkoutTemplate { Id = 2, Name = "Workout B" }
            };

            this.workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>()).Returns(workouts);

            await this.sut.LoadAvailableWorkoutsAsync();

            this.sut.AvailableWorkouts.Should().HaveCount(2);
            this.sut.SelectedWorkout.Should().Be(workouts[0]);
            this.sut.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_NotLoadingAndEmpty_CallsLoad()
        {
            this.sut.IsLoading = false;
            this.sut.AvailableWorkouts.Clear();
            var workouts = new List<WorkoutTemplate> { new WorkoutTemplate { Id = 1 } };
            this.workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>()).Returns(workouts);

            await this.sut.EnsureWorkoutsLoadedAsync();

            this.sut.AvailableWorkouts.Should().HaveCount(1);
        }

        [Fact]
        public void GetSelectedDaysOfWeek_ReturnsCorrectIndexes()
        {
            this.sut.SelectedDays[0].IsSelected = true;  
            this.sut.SelectedDays[1].IsSelected = false; 
            this.sut.SelectedDays[2].IsSelected = true;  

            var result = this.sut.GetSelectedDaysOfWeek();

            result.Should().Contain(new[] { 0, 2, 3, 4, 5 });
            result.Should().NotContain(1);
        }

        [Fact]
        public void ValidateInput_NoWorkoutSelected_ReturnsErrorMessage()
        {
            this.sut.SelectedWorkout = null;

            var result = this.sut.ValidateInput();

            result.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public void ValidateInput_InvalidDuration_ReturnsErrorMessage()
        {
            this.sut.SelectedWorkout = new WorkoutTemplate();
            this.sut.DurationWeeks = 0;

            var result = this.sut.ValidateInput();

            result.Should().Be("Duration must be between 1 and 52 weeks.");
        }

        [Fact]
        public void ValidateInput_NoDaysSelected_ReturnsErrorMessage()
        {
            this.sut.SelectedWorkout = new WorkoutTemplate();
            this.sut.DurationWeeks = 4;
            foreach (var day in this.sut.SelectedDays)
            {
                day.IsSelected = false;
            }

            var result = this.sut.ValidateInput();

            result.Should().Be("Please select at least one training day.");
        }

        [Fact]
        public void ValidateInput_ValidInput_ReturnsNull()
        {
            this.sut.SelectedWorkout = new WorkoutTemplate();
            this.sut.DurationWeeks = 4;
            this.sut.SelectedDays[1].IsSelected = true;

            var result = this.sut.ValidateInput();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GenerateCalendarAsync_ValidInput_SetsGeneratedContentAndReturnsIt()
        {
            this.sut.SelectedWorkout = new WorkoutTemplate { Id = 1 };
            this.sut.DurationWeeks = 4;

            this.calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), 4, Arg.Any<int[]>())
                .Returns("ICS CONTENT");

            var result = await this.sut.GenerateCalendarAsync();

            result.Should().Be("ICS CONTENT");
            this.sut.GeneratedIcsContent.Should().Be("ICS CONTENT");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_ValidInput_ReturnsSuccessResult()
        {
            this.sut.SelectedWorkout = new WorkoutTemplate { Id = 1 };

            this.calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), Arg.Any<int>(), Arg.Any<int[]>())
                .Returns("ICS CONTENT");

            var result = await this.sut.GenerateCalendarForExportAsync();

            result.IsSuccessful.Should().BeTrue();
            result.GeneratedCalendarContent.Should().Be("ICS CONTENT");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_ValidationFails_ReturnsFailureResult()
        {
            this.sut.SelectedWorkout = null;

            var result = await this.sut.GenerateCalendarForExportAsync();

            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public async Task SaveGeneratedCalendarToDownloadsFallbackAsync_CallsService()
        {
            this.sut.GeneratedIcsContent = "ICS CONTENT";
            this.sut.SelectedWorkout = new WorkoutTemplate { Name = "My Workout" };

            this.calendarExportService.SaveCalendarToDownloadsAsync("ICS CONTENT", "My Workout")
                .Returns(Task.FromResult((string?)"C:\\Downloads\\My_Workout.ics"));

            var result = await this.sut.SaveGeneratedCalendarToDownloadsFallbackAsync();

            result.Should().Be("C:\\Downloads\\My_Workout.ics");
        }

        [Fact]
        public void SetErrorStatus_UpdatesStateCorrectly()
        {
            this.sut.SetErrorStatus("An error occurred");

            this.sut.StatusSeverity.Should().Be(InfoBarSeverity.Error);
            this.sut.StatusTitle.Should().Be("Error");
            this.sut.StatusMessage.Should().Be("An error occurred");
            this.sut.IsStatusOpen.Should().BeTrue();
        }

        [Fact]
        public void SetSuccessStatus_UpdatesStateCorrectly()
        {
            this.sut.SetSuccessStatus("Success message");

            this.sut.StatusSeverity.Should().Be(InfoBarSeverity.Success);
            this.sut.StatusTitle.Should().Be("Success");
            this.sut.StatusMessage.Should().Be("Success message");
            this.sut.IsStatusOpen.Should().BeTrue();
        }

        [Fact]
        public void ClearStatus_ResetsStateCorrectly()
        {
            this.sut.SetErrorStatus("An error occurred");

            this.sut.ClearStatus();

            this.sut.StatusTitle.Should().BeEmpty();
            this.sut.StatusMessage.Should().BeEmpty();
            this.sut.IsStatusOpen.Should().BeFalse();
        }

        [Fact]
        public void ToggleDaySelection_TogglesSelectionCorrectly()
        {
            bool initialState = this.sut.SelectedDays[1].IsSelected; 

            this.sut.ToggleDaySelection(1);

            this.sut.SelectedDays[1].IsSelected.Should().Be(!initialState);
        }

        [Fact]
        public void ToggleDaySelection_UnknownDay_DoesNothing()
        {
            var exception = Record.Exception(() => this.sut.ToggleDaySelection(99));

            exception.Should().BeNull();
        }

        [Fact]
        public async Task LoadAvailableWorkoutsAsync_ExceptionThrown_SetsFallbackWorkouts()
        {
            this.workoutCatalogService.GetAvailableWorkoutsAsync(1, Arg.Any<TimeSpan>())
                .Returns(Task.FromException<IReadOnlyList<WorkoutTemplate>>(new Exception("Network failure")));

            var fallbackWorkouts = new List<WorkoutTemplate>
            {
                new WorkoutTemplate { Id = 99, Name = "Fallback" }
            };

            this.workoutCatalogService.GetFallbackWorkouts(1).Returns(fallbackWorkouts);

            await this.sut.LoadAvailableWorkoutsAsync();

            this.sut.AvailableWorkouts.Should().HaveCount(1);
            this.sut.SelectedWorkout.Should().Be(fallbackWorkouts[0]);
            this.sut.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_AlreadyLoading_DoesNotCallLoad()
        {
            this.workoutCatalogService.ClearReceivedCalls();
            this.sut.IsLoading = true;
            this.sut.AvailableWorkouts.Clear();

            await this.sut.EnsureWorkoutsLoadedAsync();

            await this.workoutCatalogService.DidNotReceiveWithAnyArgs().GetAvailableWorkoutsAsync(default, default);
            this.sut.AvailableWorkouts.Should().BeEmpty();
        }

        [Fact]
        public async Task EnsureWorkoutsLoadedAsync_AlreadyHasWorkouts_DoesNotCallLoad()
        {
            this.workoutCatalogService.ClearReceivedCalls();
            this.sut.IsLoading = false;
            this.sut.AvailableWorkouts.Add(new WorkoutTemplate { Id = 1 });

            await this.sut.EnsureWorkoutsLoadedAsync();

            await this.workoutCatalogService.DidNotReceiveWithAnyArgs().GetAvailableWorkoutsAsync(default, default);
            this.sut.AvailableWorkouts.Should().HaveCount(1);
        }

        [Fact]
        public async Task GenerateCalendarAsync_SelectedWorkoutNull_ThrowsException()
        {
            this.sut.SelectedWorkout = null;

            var exception = await Record.ExceptionAsync(async () => await this.sut.GenerateCalendarAsync());

            exception.Should().NotBeNull();
            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Be("Please select a workout from the dropdown.");
        }

        [Fact]
        public async Task GenerateCalendarForExportAsync_EmptyContent_ReturnsFailureResult()
        {
            this.sut.SelectedWorkout = new WorkoutTemplate { Id = 1 };
            this.sut.DurationWeeks = 4;

            this.calendarExportService.GenerateCalendar(Arg.Any<WorkoutTemplate>(), Arg.Any<int>(), Arg.Any<int[]>())
                .Returns(string.Empty);

            var result = await this.sut.GenerateCalendarForExportAsync();

            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Be("Failed to generate calendar file. Please try again.");
        }

        [Fact]
        public async Task SaveGeneratedCalendarToDownloadsFallbackAsync_NullWorkout_UsesFallbackName()
        {
            this.sut.GeneratedIcsContent = "ICS CONTENT";
            this.sut.SelectedWorkout = null;

            this.calendarExportService.SaveCalendarToDownloadsAsync("ICS CONTENT", "Workout")
                .Returns(Task.FromResult((string?)"C:\\Downloads\\Workout.ics"));

            var result = await this.sut.SaveGeneratedCalendarToDownloadsFallbackAsync();

            result.Should().Be("C:\\Downloads\\Workout.ics");
        }
    }
}