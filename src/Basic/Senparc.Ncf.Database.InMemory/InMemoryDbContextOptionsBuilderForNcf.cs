/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InMemoryDbContextOptionsBuilderForNcf.cs
    File description: InMemoryDbContextOptionsBuilderForNcf related implementation

    Created by: Senparc - 20240721

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

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
        // Summary:
        //     Clones the configuration in this builder.
        //
        // returnresult:
        //     The cloned configuration.
        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

        //DbContextOptionsBuilder IInMemoryDbContextOptionsBuilderInfrastructure.OptionsBuilder => OptionsBuilder;

        //
        // Summary:
        //     Initializes a new instance of the Microsoft.EntityFrameworkCore.Infrastructure.InMemoryDbContextOptionsBuilder
        //     class.
        //
        // parameters:
        //   optionsBuilder:
        //     The options builder.
        /// <summary>
        /// Initializes an NCF in-memory options builder over an EF Core options builder.
        /// </summary>
        /// <param name="optionsBuilder">The EF Core options builder to configure.</param>
        public InMemoryDbContextOptionsBuilderForNcf(DbContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
            OptionsBuilder = optionsBuilder;
        }

        //
        // Summary:
        //     Enables nullability check for all properties across all entities within the in-memory
        //     database.
        //
        // parameters:
        //   nullChecksEnabled:
        //     If true, then nullability check is enforced.
        //
        // returnresult:
        //     The same builder instance so that multiple calls can be chained.
        //
        //Legacy comment translated to English.
        //     See Using DbContextOptions, and The EF Core in-memory database provider for more
        //     information and examples.
        /// <summary>
        /// Enables or disables nullability checks for entities in the in-memory database.
        /// </summary>
        /// <param name="nullChecksEnabled">Whether nullability checks should be enabled.</param>
        /// <returns>This builder for fluent configuration.</returns>
        public virtual InMemoryDbContextOptionsBuilderForNcf EnableNullChecks(bool nullChecksEnabled = true)
        {
            InMemoryOptionsExtension inMemoryOptionsExtension = OptionsBuilder.Options.FindExtension<InMemoryOptionsExtension>() ?? new InMemoryOptionsExtension();
            inMemoryOptionsExtension = inMemoryOptionsExtension.WithNullabilityCheckEnabled(nullChecksEnabled);
            ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(inMemoryOptionsExtension);
            return this;
        }

        //
        // Summary:
        //     Returns a string that represents the current object.
        //
        // returnresult:
        //     A string that represents the current object.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string? ToString()
        {
            return base.ToString();
        }

        //
        // Summary:
        //     Determines whether the specified object is equal to the current object.
        //
        // parameters:
        //   obj:
        //     The object to compare with the current object.
        //
        // returnresult:
        //     true if the specified object is equal to the current object; otherwise, false.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        //
        // Summary:
        //     Serves as the default hash function.
        //
        // returnresult:
        //     A hash code for the current object.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
