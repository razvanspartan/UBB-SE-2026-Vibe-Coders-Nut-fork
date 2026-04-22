using System;
using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NSubstitute;
using VibeCoders.Services;
using VibeCoders.Views;
using Xunit;

namespace VibeCoders.Tests.Unit.Services
{
    public class NavigationServiceTests
    {
        private readonly IAnalyticsDashboardRefreshBus refreshBus;
        private readonly NavigationService systemUnderTest;

        public NavigationServiceTests()
        {
            refreshBus = Substitute.For<IAnalyticsDashboardRefreshBus>();
            systemUnderTest = new NavigationService(refreshBus);
        }

        [Fact]
        public void NavigateToClientDashboard_WithRequestRefresh_RequestsRefresh()
        {
            bool wasRefreshRequested = false;
            refreshBus.When(bus => bus.RequestRefresh()).Do(call => wasRefreshRequested = true);

            systemUnderTest.NavigateToClientDashboard(true);

            wasRefreshRequested.Should().BeTrue();
        }

        [Fact]
        public void NavigateToClientDashboard_WithoutRequestRefresh_DoesNotRequestRefresh()
        {
            bool wasRefreshRequested = false;
            refreshBus.When(bus => bus.RequestRefresh()).Do(call => wasRefreshRequested = true);

            systemUnderTest.NavigateToClientDashboard(false);

            wasRefreshRequested.Should().BeFalse();
        }
    }
}