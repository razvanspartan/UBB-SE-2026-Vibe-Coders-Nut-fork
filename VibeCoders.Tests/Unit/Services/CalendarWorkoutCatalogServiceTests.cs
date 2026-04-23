using System.Threading;
using FluentAssertions;
using NSubstitute;
using VibeCoders.Models;
using VibeCoders.Repositories.Interfaces;
using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
	public class CalendarWorkoutCatalogServiceTests
	{
		private const int ClientId = 7;
		private const int FallbackWorkoutCount = 4;
		private const int ExercisesPerFallbackWorkout = 3;
		private static readonly TimeSpan RepositoryResponseTimeout = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(1);

		private readonly IRepositoryWorkoutTemplate workoutTemplateRepositoryMock;
		private readonly CalendarWorkoutCatalogService systemUnderTest;

		public CalendarWorkoutCatalogServiceTests()
		{
			this.workoutTemplateRepositoryMock = Substitute.For<IRepositoryWorkoutTemplate>();
			this.systemUnderTest = new CalendarWorkoutCatalogService(this.workoutTemplateRepositoryMock);
		}

		[Fact]
		public async Task GetAvailableWorkoutsAsync_WhenRepositoryReturnsWorkouts_ReturnsRepositoryWorkouts()
		{
			var expectedWorkouts = new List<WorkoutTemplate>
			{
				new WorkoutTemplate { Id = 1, ClientId = ClientId, Name = "Push Day", Type = WorkoutType.CUSTOM }
			};

			this.workoutTemplateRepositoryMock.GetAvailableWorkouts(ClientId).Returns(expectedWorkouts);

			var availableWorkouts = await this.systemUnderTest.GetAvailableWorkoutsAsync(ClientId, RepositoryResponseTimeout);

			availableWorkouts.Should().BeSameAs(expectedWorkouts);
		}

		[Fact]
		public async Task GetAvailableWorkoutsAsync_WhenRepositoryReturnsEmptyList_ReturnsFallbackWorkouts()
		{
			this.workoutTemplateRepositoryMock.GetAvailableWorkouts(ClientId).Returns(new List<WorkoutTemplate>());

			var availableWorkouts = await this.systemUnderTest.GetAvailableWorkoutsAsync(ClientId, ShortTimeout);

			availableWorkouts.Should().HaveCount(FallbackWorkoutCount);
			availableWorkouts.All(workout => workout.ClientId == ClientId).Should().BeTrue();
		}

		[Fact]
		public async Task GetAvailableWorkoutsAsync_WhenRepositoryThrows_ReturnsFallbackWorkouts()
		{
			this.workoutTemplateRepositoryMock.GetAvailableWorkouts(ClientId).Returns(_ => throw new InvalidOperationException("load failed"));

			var availableWorkouts = await this.systemUnderTest.GetAvailableWorkoutsAsync(ClientId, ShortTimeout);

			availableWorkouts.Should().HaveCount(FallbackWorkoutCount);
		}

		[Fact]
		public void GetFallbackWorkouts_WhenCalled_ReturnsFourPrebuiltWorkoutsWithExercises()
		{
			var fallbackWorkouts = this.systemUnderTest.GetFallbackWorkouts(ClientId);

			fallbackWorkouts.Should().HaveCount(FallbackWorkoutCount);
			fallbackWorkouts.All(workout => workout.ClientId == ClientId).Should().BeTrue();
			fallbackWorkouts.All(workout => workout.Type == WorkoutType.PREBUILT).Should().BeTrue();
			fallbackWorkouts.All(workout => workout.GetExercises().Count == ExercisesPerFallbackWorkout).Should().BeTrue();
		}
	}
}
