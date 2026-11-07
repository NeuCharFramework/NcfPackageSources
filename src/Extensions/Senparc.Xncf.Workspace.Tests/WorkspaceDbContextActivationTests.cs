using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Workspace.Domain.Models.MultipleDatabase;

namespace Senparc.Xncf.Workspace.Tests;

[TestClass]
public sealed class WorkspaceDbContextActivationTests
{
    [TestMethod]
    public void ActivatorUtilities_CreatesSqlServerContext_FromGenericOptions()
    {
        using var context = Create(new DbContextOptionsBuilder<WorkspaceSenparcEntities_SqlServer>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ncf_workspace_activation;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

        Assert.IsInstanceOfType<WorkspaceSenparcEntities_SqlServer>(context);
    }

    [TestMethod]
    public void ActivatorUtilities_CreatesSqlServerContext_FromUntypedOptions()
    {
        var options = new DbContextOptionsBuilder()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ncf_workspace_activation;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        Assert.IsFalse(options is DbContextOptions<WorkspaceSenparcEntities_SqlServer>);

        using var context = Create(options);
        Assert.IsInstanceOfType<WorkspaceSenparcEntities_SqlServer>(context);
        Console.WriteLine($"AdminChat workspace context created from {options.GetType().Name}.");
    }

    private static WorkspaceSenparcEntities_SqlServer Create(DbContextOptions options)
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        return (WorkspaceSenparcEntities_SqlServer)ActivatorUtilities.CreateInstance(
            provider,
            typeof(WorkspaceSenparcEntities_SqlServer),
            options);
    }
}
