using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class FineTuningWorkerPersistenceTests
{
    [TestMethod]
    public async Task WorkerProfile_RoundTripsThroughSqlite_WithoutStoringAuthenticationSecrets()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AIKernelSenparcEntities_Sqlite>().UseSqlite(connection).Options;
        await using (var context = new AIKernelSenparcEntities_Sqlite(options) { _enableMultiTenant = false })
        {
            await context.Database.EnsureCreatedAsync();
            context.AiFineTuningWorkers.Add(new AIFineTuningWorker(
                "cpu_lab", "CPU lab", "http://127.0.0.1:8091", 180, true, "Offline training only"));
            await context.SaveChangesAsync();
        }
        await using (var context = new AIKernelSenparcEntities_Sqlite(options) { _enableMultiTenant = false })
        {
            var profile = await context.AiFineTuningWorkers.IgnoreQueryFilters().SingleAsync();
            Assert.AreEqual("cpu_lab", profile.Alias);
            Assert.AreEqual("http://127.0.0.1:8091", profile.Endpoint);
            Assert.AreEqual(180, profile.RequestTimeoutSeconds);
            Assert.IsTrue(profile.Enabled);
            Assert.AreEqual("Offline training only", profile.Note);
            Assert.IsNull(typeof(AIFineTuningWorker).GetProperty("ApiKey"));
            profile.Update(profile.Alias, profile.Name, profile.Endpoint, 60, false, "Maintenance");
            await context.SaveChangesAsync();
        }
        await using (var context = new AIKernelSenparcEntities_Sqlite(options) { _enableMultiTenant = false })
        {
            var profile = await context.AiFineTuningWorkers.IgnoreQueryFilters().SingleAsync();
            Assert.AreEqual(60, profile.RequestTimeoutSeconds);
            Assert.IsFalse(profile.Enabled);
            Assert.AreEqual("Maintenance", profile.Note);
        }
    }
}
