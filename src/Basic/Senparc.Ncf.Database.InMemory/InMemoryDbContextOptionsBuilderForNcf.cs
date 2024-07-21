using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    public class InMemoryDbContextOptionsBuilderForNcf : RelationalDbContextOptionsBuilder<InMemoryDbContextOptionsBuilderForNcf, InMemoryOptionsExtensionForNcf>
    {
        //
        // 摘要:
        //     Clones the configuration in this builder.
        //
        // 返回结果:
        //     The cloned configuration.
        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

        //DbContextOptionsBuilder IInMemoryDbContextOptionsBuilderInfrastructure.OptionsBuilder => OptionsBuilder;

        //
        // 摘要:
        //     Initializes a new instance of the Microsoft.EntityFrameworkCore.Infrastructure.InMemoryDbContextOptionsBuilder
        //     class.
        //
        // 参数:
        //   optionsBuilder:
        //     The options builder.
        public InMemoryDbContextOptionsBuilderForNcf(DbContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
            OptionsBuilder = optionsBuilder;
        }

        //
        // 摘要:
        //     Enables nullability check for all properties across all entities within the in-memory
        //     database.
        //
        // 参数:
        //   nullChecksEnabled:
        //     If true, then nullability check is enforced.
        //
        // 返回结果:
        //     The same builder instance so that multiple calls can be chained.
        //
        // 言论：
        //     See Using DbContextOptions, and The EF Core in-memory database provider for more
        //     information and examples.
        public virtual InMemoryDbContextOptionsBuilderForNcf EnableNullChecks(bool nullChecksEnabled = true)
        {
            InMemoryOptionsExtension inMemoryOptionsExtension = OptionsBuilder.Options.FindExtension<InMemoryOptionsExtension>() ?? new InMemoryOptionsExtension();
            inMemoryOptionsExtension = inMemoryOptionsExtension.WithNullabilityCheckEnabled(nullChecksEnabled);
            ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(inMemoryOptionsExtension);
            return this;
        }

        //
        // 摘要:
        //     Returns a string that represents the current object.
        //
        // 返回结果:
        //     A string that represents the current object.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string? ToString()
        {
            return base.ToString();
        }

        //
        // 摘要:
        //     Determines whether the specified object is equal to the current object.
        //
        // 参数:
        //   obj:
        //     The object to compare with the current object.
        //
        // 返回结果:
        //     true if the specified object is equal to the current object; otherwise, false.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        //
        // 摘要:
        //     Serves as the default hash function.
        //
        // 返回结果:
        //     A hash code for the current object.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
