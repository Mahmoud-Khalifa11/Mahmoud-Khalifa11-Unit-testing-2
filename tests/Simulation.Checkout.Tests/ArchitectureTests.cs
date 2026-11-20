using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Infrastructure;
using Xunit;

namespace Simulation.Checkout.Tests;

public class ArchitectureTests
{
    private const string DomainNamespace = "Simulation.Checkout.Domain";
    private const string ApplicationNamespace = "Simulation.Checkout.Application";
    private const string InfrastructureNamespace = "Simulation.Checkout.Infrastructure";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(CheckoutService).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(InMemoryOrderRepository).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        var assemblies = new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly };

        var result = Types.InAssemblies(assemblies)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void OrderRepositoryImplementations_ShouldEndWithRepository()
    {
        var assemblies = new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly };

        var result = Types.InAssemblies(assemblies)
            .That()
            .ImplementInterface(typeof(IOrderRepository))
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}