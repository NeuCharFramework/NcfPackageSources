/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseInstallStateTests.cs
    File description: Test for classification of exceptions in the initial installation of the database and schema upgrade

    Created by: Senparc - 20260803

----------------------------------------------------------------*/

using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Ncf.Core.Tests.Utility
{
    [TestClass]
    public class DatabaseInstallStateTests
    {
        [DataTestMethod]
        [DataRow("Invalid column name 'FooterContent'.")]
        [DataRow("Unknown column 'FooterContent' in 'field list'")]
        [DataRow("no such column: FooterContent")]
        [DataRow("column \"FooterContent\" does not exist")]
        [DataRow("ORA-00904: \"FooterContent\": invalid identifier")]
        public void MissingColumn_ShouldRequireUpgradeWithoutOpeningInstaller(string providerMessage)
        {
            var exception = new InvalidOperationException(
                "EF Core query failed.",
                new TestDbException(providerMessage));

            Assert.IsTrue(DatabaseInstallState.IsSchemaUpgradeRequired(exception));
            Assert.IsFalse(DatabaseInstallState.IsDatabaseUnavailableForInstallation(exception));
        }

        [DataTestMethod]
        [DataRow("no such table: SystemConfigs")]
        [DataRow("Invalid object name 'SystemConfigs'.")]
        [DataRow("relation \"SystemConfigs\" does not exist")]
        [DataRow("Table 'ncf.SystemConfigs' doesn't exist")]
        public void MissingTable_ShouldRemainAFirstInstallationState(string providerMessage)
        {
            var exception = new TestDbException(providerMessage);

            Assert.IsFalse(DatabaseInstallState.IsSchemaUpgradeRequired(exception));
            Assert.IsTrue(DatabaseInstallState.IsDatabaseUnavailableForInstallation(exception));
        }

        [TestMethod]
        public void NonProviderException_ShouldNotBeClassifiedByMessageAlone()
        {
            var exception = new InvalidOperationException("Invalid column name 'FooterContent'.");

            Assert.IsFalse(DatabaseInstallState.IsSchemaUpgradeRequired(exception));
            Assert.IsFalse(DatabaseInstallState.IsDatabaseUnavailableForInstallation(exception));
        }

        private sealed class TestDbException : DbException
        {
            public TestDbException(string message) : base(message)
            {
            }
        }
    }
}
