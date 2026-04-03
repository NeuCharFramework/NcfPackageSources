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
        // summary:
        //     Clones the configuration in this builder.
        //
        // Return results:
        //     The cloned configuration.
        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

        //DbContextOptionsBuilder IInMemoryDbContextOptionsBuilderInfrastructure.OptionsBuilder => OptionsBuilder;

        //
        // summary:
        //     Initializes a new instance of the Microsoft.EntityFrameworkCore.Infrastructure.InMemoryDbContextOptionsBuilder
        //     class.
        //
        // parameter:
        //   optionsBuilder:
        //     The options builder.
        public InMemoryDbContextOptionsBuilderForNcf(DbContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
            OptionsBuilder = optionsBuilder;
        }

        //
        // summary:
        //     Enables nullability check for all properties across all entities within the in-memory
        //     database.
        //
        // parameter:
        //   nullChecksEnabled:
        //     If true, then nullability check is enforced.
        //
        // Return results:
        //     The same builder instance so that multiple calls can be chained.
        //
        // Speech:
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
        // summary:
        //     Returns a string that represents the current object.
        //
        // Return results:
        //     A string that represents the current object.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string? ToString()
        {
            return base.ToString();
        }

        //
        // summary:
        //     Determines whether the specified object is equal to the current object.
        //
        // parameter:
        //   obj:
        //     The object to compare with the current object.
        //
        // Return results:
        //     true if the specified object is equal to the current object; otherwise, false.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        //
        // summary:
        //     Serves as the default hash function.
        //
        // Return results:
        //     A hash code for the current object.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
