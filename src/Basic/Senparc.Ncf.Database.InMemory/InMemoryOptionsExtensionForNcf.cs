/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InMemoryOptionsExtensionForNcf.cs
    File description: InMemoryOptionsExtensionForNcf related implementation

    Created by: Senparc - 20240721

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal
{
    public class InMemoryOptionsExtensionForNcf : RelationalOptionsExtension
    {
        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private string _logFragment;

            private new InMemoryOptionsExtensionForNcf Extension => (InMemoryOptionsExtensionForNcf)base.Extension;

            public override bool IsDatabaseProvider => true;

            public override string LogFragment
            {
                get
                {
                    if (_logFragment == null)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.Append("StoreName=").Append(Extension.StoreName).Append(' ');
                        if (!Extension.IsNullabilityCheckEnabled)
                        {
                            stringBuilder.Append("NullabilityChecksEnabled ");
                        }

                        _logFragment = stringBuilder.ToString();
                    }

                    return _logFragment;
                }
            }

            /// <summary>
            /// Initializes diagnostic information for the NCF in-memory options extension.
            /// </summary>
            /// <param name="extension">The options extension represented by this diagnostic object.</param>
            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            public override int GetServiceProviderHashCode()
            {
                HashCode hashCode = default(HashCode);
                hashCode.Add(Extension.DatabaseRoot);
                hashCode.Add(Extension.IsNullabilityCheckEnabled);
                return hashCode.ToHashCode();
            }

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            {
                if (other is ExtensionInfo extensionInfo && Extension.DatabaseRoot == extensionInfo.Extension.DatabaseRoot)
                {
                    return Extension.IsNullabilityCheckEnabled == extensionInfo.Extension.IsNullabilityCheckEnabled;
                }

                return false;
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["InMemoryDatabase:DatabaseRoot"] = (Extension.DatabaseRoot?.GetHashCode() ?? 0).ToString(CultureInfo.InvariantCulture);
                debugInfo["InMemoryDatabase:NullabilityChecksEnabled"] = (!Extension.IsNullabilityCheckEnabled).GetHashCode().ToString(CultureInfo.InvariantCulture);
            }
        }

        private string _storeName;

        private bool _nullabilityCheckEnabled;

        private InMemoryDatabaseRoot _databaseRoot;

        private DbContextOptionsExtensionInfo _info;

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public override DbContextOptionsExtensionInfo Info => _info ?? (_info = new ExtensionInfo(this));

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public virtual string StoreName => _storeName;

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public virtual bool IsNullabilityCheckEnabled => _nullabilityCheckEnabled;

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public virtual InMemoryDatabaseRoot? DatabaseRoot => _databaseRoot;

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        /// <summary>
        /// Initializes empty NCF in-memory database options.
        /// </summary>
        public InMemoryOptionsExtensionForNcf()
        {
        }

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        /// <summary>
        /// Copies the mutable store and database-root settings from an existing extension.
        /// </summary>
        /// <param name="copyFrom">The extension whose settings should be copied.</param>
        protected InMemoryOptionsExtensionForNcf(InMemoryOptionsExtensionForNcf copyFrom)
        {
            _storeName = copyFrom.StoreName;
            _databaseRoot = copyFrom._databaseRoot;
        }

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        //protected virtual InMemoryOptionsExtensionForNcf Clone()
        //{
        //    return new InMemoryOptionsExtensionForNcf(this);
        //}

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        /// <summary>
        /// Creates a cloned options extension with the specified in-memory store name.
        /// </summary>
        /// <param name="storeName">The logical store name used by the in-memory provider.</param>
        /// <returns>A cloned extension containing the requested store name.</returns>
        public virtual InMemoryOptionsExtensionForNcf WithStoreName(string storeName)
        {
            InMemoryOptionsExtensionForNcf inMemoryOptionsExtension = Clone() as InMemoryOptionsExtensionForNcf;

            var fieldInfo = typeof(InMemoryOptionsExtensionForNcf).GetField("_storeName", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(inMemoryOptionsExtension, storeName);
            }

            return inMemoryOptionsExtension;
        }

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        /// <summary>
        /// Creates a cloned options extension with the requested nullability-check setting.
        /// </summary>
        /// <param name="nullabilityCheckEnabled">Whether entity nullability checks should be enabled.</param>
        /// <returns>A cloned extension containing the requested setting.</returns>
        public virtual InMemoryOptionsExtensionForNcf WithNullabilityCheckEnabled(bool nullabilityCheckEnabled)
        {
            InMemoryOptionsExtensionForNcf inMemoryOptionsExtension = Clone() as InMemoryOptionsExtensionForNcf;

            var fieldInfo = typeof(InMemoryOptionsExtensionForNcf).GetField("_isNullabilityCheckEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(inMemoryOptionsExtension, nullabilityCheckEnabled);
            }

            return inMemoryOptionsExtension;
        }

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        /// <summary>
        /// Creates a cloned options extension using the supplied shared database root.
        /// </summary>
        /// <param name="databaseRoot">The root used to share in-memory database state.</param>
        /// <returns>A cloned extension containing the requested root.</returns>
        public virtual InMemoryOptionsExtensionForNcf WithDatabaseRoot(InMemoryDatabaseRoot databaseRoot)
        {
            InMemoryOptionsExtensionForNcf inMemoryOptionsExtension = Clone() as InMemoryOptionsExtensionForNcf;

            var fieldInfo = typeof(InMemoryOptionsExtensionForNcf).GetField("_databaseRoot", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(inMemoryOptionsExtension, databaseRoot);
            }

            return inMemoryOptionsExtension;
        }

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public override void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkInMemoryDatabase();
        }

        //
        // Summary:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        /// <summary>
        /// Validates provider options required by the NCF in-memory integration.
        /// </summary>
        /// <param name="options">The complete EF Core options collection.</param>
        public virtual void Validate(IDbContextOptions options)
        {

        }

        protected override RelationalOptionsExtension Clone()
        {
            return new InMemoryOptionsExtensionForNcf(this);
        }
    }
}
