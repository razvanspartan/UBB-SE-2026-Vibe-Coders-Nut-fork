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
        private readonly IAnalyticsDashboardRefreshBus _refreshBus;
        private readonly NavigationService _systemUnderTest;

        public NavigationServiceTests()
        {
            _refreshBus = Substitute.For<IAnalyticsDashboardRefreshBus>();
            _systemUnderTest = new NavigationService(_refreshBus);
        }

        [Fact]
        public void NavigateToClientDashboard_WithRequestRefresh_RequestsRefresh()
        {
            bool wasRefreshRequested = false;
            _refreshBus.When(refreshBus => refreshBus.RequestRefresh()).Do(call => wasRefreshRequested = true);

            _systemUnderTest.NavigateToClientDashboard(true);

            wasRefreshRequested.Should().BeTrue();
        }

        [Fact]
        public void NavigateToClientDashboard_WithoutRequestRefresh_DoesNotRequestRefresh()
        {
            bool wasRefreshRequested = false;
            _refreshBus.When(refreshBus => refreshBus.RequestRefresh()).Do(call => wasRefreshRequested = true);

            _systemUnderTest.NavigateToClientDashboard(false);

            wasRefreshRequested.Should().BeFalse();
        }
    }
}