using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.XncfBuilder.OHS.Local;
using System;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class AddMigrationTests
    {
        [TestMethod]
        public void GetOutputDirectoryArgument_ShouldUseProjectRelativePath()
        {
            var projectDirectory = CreateTestProject();
            try
            {
                var migrationDirectory = Path.Combine(projectDirectory, "Domain", "Migrations", "Sqlite");

                var outputDirectory = MigrationFileLayoutHelper.GetOutputDirectoryArgument(
                    projectDirectory,
                    migrationDirectory);

                Assert.AreEqual(Path.Combine("Domain", "Migrations", "Sqlite"), outputDirectory);
            }
            finally
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void GetExpectedNamespace_ShouldFollowPromptRangeLayout()
        {
            var projectDirectory = CreateTestProject("Sample.Org.Xncf.SampleModule");
            try
            {
                var expectedNamespace = MigrationFileLayoutHelper.GetExpectedNamespace(projectDirectory, "MySql");

                Assert.AreEqual("Sample.Org.Xncf.SampleModule.Domain.Migrations.MySql", expectedNamespace);
            }
            finally
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void VerifyGeneratedMigrationFiles_ShouldRequireMatchingPairInMigrationDirectory()
        {
            var projectDirectory = CreateTestProject();
            try
            {
                var migrationDirectory = Path.Combine(projectDirectory, "Domain", "Migrations", "SqlServer");
                Directory.CreateDirectory(migrationDirectory);
                var filesBefore = MigrationFileLayoutHelper.CaptureMigrationFiles(migrationDirectory);
                var migrationFile = Path.Combine(migrationDirectory, "20260803120000_Add_Test.cs");
                var designerFile = Path.Combine(migrationDirectory, "20260803120000_Add_Test.Designer.cs");
                File.WriteAllText(migrationFile, string.Empty);
                File.WriteAllText(designerFile, string.Empty);
                File.WriteAllText(
                    Path.Combine(migrationDirectory, "SampleSenparcEntities_SqlServerModelSnapshot.cs"),
                    string.Empty);

                var generatedFiles = MigrationFileLayoutHelper.VerifyGeneratedMigrationFiles(
                    migrationDirectory,
                    filesBefore);

                Assert.AreEqual(migrationFile, generatedFiles.MigrationFile);
                Assert.AreEqual(designerFile, generatedFiles.DesignerFile);
            }
            finally
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void AlignSnapshot_ShouldMoveGeneratedSnapshotAndRemoveStaleDuplicate()
        {
            const string dbContextName = "SampleSenparcEntities_SqlServer";
            const string snapshotClassName = dbContextName + "ModelSnapshot";
            const string expectedNamespace = "Sample.Org.Xncf.SampleModule.Domain.Migrations.SqlServer";
            var projectDirectory = CreateTestProject("Sample.Org.Xncf.SampleModule");
            try
            {
                var migrationDirectory = Path.Combine(projectDirectory, "Domain", "Migrations", "SqlServer");
                Directory.CreateDirectory(migrationDirectory);

                var staleSnapshot = Path.Combine(migrationDirectory, "SampleEntities_SqlServerModelSnapshot.cs");
                File.WriteAllText(staleSnapshot, CreateSnapshotSource(
                    "Sample.Org.Xncf.SampleModule.Migrations.Deomain.Migrations.SqlServer",
                    snapshotClassName,
                    dbContextName,
                    "stale"));

                var generatedDirectory = Path.Combine(
                    projectDirectory,
                    "Migrations",
                    "Deomain",
                    "Migrations",
                    "SqlServer");
                Directory.CreateDirectory(generatedDirectory);
                var generatedSnapshot = Path.Combine(generatedDirectory, snapshotClassName + ".cs");
                File.WriteAllText(generatedSnapshot, CreateSnapshotSource(
                    "Sample.Org.Xncf.SampleModule.Migrations.Deomain.Migrations.SqlServer",
                    snapshotClassName,
                    dbContextName,
                    "generated"));

                var result = MigrationFileLayoutHelper.AlignSnapshot(
                    projectDirectory,
                    migrationDirectory,
                    dbContextName,
                    expectedNamespace);

                var expectedSnapshot = Path.Combine(migrationDirectory, snapshotClassName + ".cs");
                Assert.IsTrue(result.SnapshotFound);
                Assert.IsTrue(result.Moved);
                Assert.IsTrue(result.NamespaceChanged);
                Assert.IsTrue(File.Exists(expectedSnapshot));
                Assert.IsFalse(File.Exists(staleSnapshot));
                Assert.IsFalse(File.Exists(generatedSnapshot));
                Assert.AreEqual(1, Directory.GetFiles(projectDirectory, "*ModelSnapshot.cs", SearchOption.AllDirectories).Length);

                var alignedContent = File.ReadAllText(expectedSnapshot);
                StringAssert.Contains(alignedContent, $"namespace {expectedNamespace}");
                StringAssert.Contains(alignedContent, "// generated");
            }
            finally
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void AlignSnapshot_ShouldKeepCanonicalSnapshotAndRemoveWrongFileName()
        {
            const string dbContextName = "SampleSenparcEntities_PostgreSQL";
            const string snapshotClassName = dbContextName + "ModelSnapshot";
            const string expectedNamespace = "Sample.Org.Xncf.SampleModule.Domain.Migrations.PostgreSQL";
            var projectDirectory = CreateTestProject("Sample.Org.Xncf.SampleModule");
            try
            {
                var migrationDirectory = Path.Combine(projectDirectory, "Domain", "Migrations", "PostgreSQL");
                Directory.CreateDirectory(migrationDirectory);

                var canonicalSnapshot = Path.Combine(migrationDirectory, snapshotClassName + ".cs");
                File.WriteAllText(canonicalSnapshot, CreateSnapshotSource(
                    expectedNamespace,
                    snapshotClassName,
                    dbContextName,
                    "canonical"));
                var duplicateSnapshot = Path.Combine(migrationDirectory, "SampleEntities_PostgreSQLModelSnapshot.cs");
                File.WriteAllText(duplicateSnapshot, CreateSnapshotSource(
                    expectedNamespace,
                    snapshotClassName,
                    dbContextName,
                    "duplicate"));

                var result = MigrationFileLayoutHelper.AlignSnapshot(
                    projectDirectory,
                    migrationDirectory,
                    dbContextName,
                    expectedNamespace);

                Assert.IsTrue(result.SnapshotFound);
                Assert.IsFalse(result.Moved);
                Assert.IsFalse(result.NamespaceChanged);
                Assert.IsTrue(File.Exists(canonicalSnapshot));
                Assert.IsFalse(File.Exists(duplicateSnapshot));
                Assert.AreEqual(1, result.RemovedDuplicateFiles.Count);
                StringAssert.Contains(File.ReadAllText(canonicalSnapshot), "// canonical");
            }
            finally
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }

        private static string CreateTestProject(string rootNamespace = "Sample.Project")
        {
            var projectDirectory = Path.Combine(
                Path.GetTempPath(),
                "NcfXncfBuilderMigrationTests",
                Guid.NewGuid().ToString("N"),
                "Project With Spaces");
            Directory.CreateDirectory(projectDirectory);
            File.WriteAllText(
                Path.Combine(projectDirectory, "Sample.Project.csproj"),
                $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>{rootNamespace}</RootNamespace></PropertyGroup></Project>");
            return projectDirectory;
        }

        private static string CreateSnapshotSource(
            string snapshotNamespace,
            string snapshotClassName,
            string dbContextName,
            string marker)
        {
            return $$"""
                using Microsoft.EntityFrameworkCore;
                using Microsoft.EntityFrameworkCore.Infrastructure;

                namespace {{snapshotNamespace}}
                {
                    [DbContext(typeof({{dbContextName}}))]
                    partial class {{snapshotClassName}} : ModelSnapshot
                    {
                        // {{marker}}
                        protected override void BuildModel(ModelBuilder modelBuilder)
                        {
                        }
                    }
                }
                """;
        }
    }
}
