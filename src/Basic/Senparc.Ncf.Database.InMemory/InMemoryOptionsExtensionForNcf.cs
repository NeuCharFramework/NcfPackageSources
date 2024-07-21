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
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public override DbContextOptionsExtensionInfo Info => _info ?? (_info = new ExtensionInfo(this));

        //
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public virtual string StoreName => _storeName;

        //
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public virtual bool IsNullabilityCheckEnabled => _nullabilityCheckEnabled;

        //
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public virtual InMemoryDatabaseRoot? DatabaseRoot => _databaseRoot;

        //
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public InMemoryOptionsExtensionForNcf()
        {
        }

        //
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        protected InMemoryOptionsExtensionForNcf(InMemoryOptionsExtensionForNcf copyFrom)
        {
            _storeName = copyFrom.StoreName;
            _databaseRoot = copyFrom._databaseRoot;
        }

        //
        // 摘要:
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
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
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
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
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
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
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
        // 摘要:
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
        // 摘要:
        //     This is an internal API that supports the Entity Framework Core infrastructure
        //     and not subject to the same compatibility standards as public APIs. It may be
        //     changed or removed without notice in any release. You should only use it directly
        //     in your code with extreme caution and knowing that doing so can result in application
        //     failures when updating to a new Entity Framework Core release.
        public virtual void Validate(IDbContextOptions options)
        {

        }

        protected override RelationalOptionsExtension Clone()
        {
            return new InMemoryOptionsExtensionForNcf(this);
        }
    }
}
