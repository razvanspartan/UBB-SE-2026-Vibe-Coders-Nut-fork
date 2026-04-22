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
        private readonly NavigationService _sut;

        public NavigationServiceTests()
        {
            _refreshBus = Substitute.For<IAnalyticsDashboardRefreshBus>();
            _sut = new NavigationService(_refreshBus);
        }

        [Fact]
        public void NavigateToClientDashboard_WithRequestRefresh_RequestsRefresh()
        {
            // Arrange
            bool refreshRequested = false;
            _refreshBus.When(x => x.RequestRefresh()).Do(x => refreshRequested = true);

            // Act
            _sut.NavigateToClientDashboard(true);

            // Assert
            refreshRequested.Should().BeTrue();
        }

        [Fact]
        public void NavigateToClientDashboard_WithoutRequestRefresh_DoesNotRequestRefresh()
        {
            // Arrange
            bool refreshRequested = false;
            _refreshBus.When(x => x.RequestRefresh()).Do(x => refreshRequested = true);

            // Act
            _sut.NavigateToClientDashboard(false);

            // Assert
            refreshRequested.Should().BeFalse();
        }

    }
}