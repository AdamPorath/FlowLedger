using System.Reflection;
using NetArchTest.Rules;

namespace FlowLedger.ArchitectureTests;

public class LayeringRuleTests
{
    private static readonly string[] ServiceAssemblyNames =
    [
        "FlowLedger.Transactions.Api",
        "FlowLedger.Consolidation.Api",
        "FlowLedger.Consolidation.Worker",
        "FlowLedger.Identity.Api",
        "FlowLedger.Gateway",
    ];

    [Theory]
    [InlineData("FlowLedger.Transactions.Api", "FlowLedger.Transactions.Api.Domain")]
    [InlineData("FlowLedger.Consolidation.Worker", "FlowLedger.Consolidation.Worker.Domain")]
    public void Domain_Should_Not_DependOn_InfrastructureConcerns(string assemblyName, string domainNamespace)
    {
        var result = Types.InAssembly(Assembly.Load(assemblyName))
            .That()
            .ResideInNamespace(domainNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "MassTransit",
                "Npgsql")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailures(result, $"{domainNamespace} must not depend on infrastructure/ORM/messaging concerns"));
    }

    [Fact]
    public void Domain_Should_Not_DependOn_Endpoints()
    {
        var result = Types.InAssembly(Assembly.Load("FlowLedger.Transactions.Api"))
            .That()
            .ResideInNamespace("FlowLedger.Transactions.Api.Domain")
            .ShouldNot()
            .HaveDependencyOn("FlowLedger.Transactions.Api.Endpoints")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailures(result, "Domain must not depend on the HTTP endpoints layer"));
    }

    [Fact]
    public void Application_Should_Not_DependOn_Endpoints()
    {
        var result = Types.InAssembly(Assembly.Load("FlowLedger.Transactions.Api"))
            .That()
            .ResideInNamespace("FlowLedger.Transactions.Api.Application")
            .ShouldNot()
            .HaveDependencyOn("FlowLedger.Transactions.Api.Endpoints")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailures(result, "Application handlers must not depend on the HTTP endpoints layer"));
    }

    private static readonly Dictionary<string, string[]> BoundedContexts = new()
    {
        ["FlowLedger.Transactions.Api"] = ["FlowLedger.Transactions.Api"],
        ["FlowLedger.Consolidation.Api"] = ["FlowLedger.Consolidation.Api", "FlowLedger.Consolidation.Worker"],
        ["FlowLedger.Consolidation.Worker"] = ["FlowLedger.Consolidation.Api", "FlowLedger.Consolidation.Worker"],
        ["FlowLedger.Identity.Api"] = ["FlowLedger.Identity.Api"],
        ["FlowLedger.Gateway"] = ["FlowLedger.Gateway"],
    };

    [Theory]
    [MemberData(nameof(ServiceAssemblyCases))]
    public void Services_Should_Not_DependOnEachOthersInternals(string serviceAssemblyName)
    {
        var otherServiceNames = ServiceAssemblyNames
            .Where(name => !BoundedContexts[serviceAssemblyName].Contains(name))
            .ToArray();

        var result = Types.InAssembly(Assembly.Load(serviceAssemblyName))
            .That()
            .ResideInNamespaceStartingWith(serviceAssemblyName)
            .ShouldNot()
            .HaveDependencyOnAny(otherServiceNames)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailures(result, $"{serviceAssemblyName} must not depend on the internals of the other services"));
    }

    public static TheoryData<string> ServiceAssemblyCases()
    {
        var data = new TheoryData<string>();

        foreach (var name in ServiceAssemblyNames)
        {
            data.Add(name);
        }

        return data;
    }

    private static string FormatFailures(TestResult result, string message)
    {
        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);

        return $"{message}. Offending types: {offenders}";
    }
}
