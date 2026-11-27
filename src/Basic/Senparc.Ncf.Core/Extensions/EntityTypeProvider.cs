/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EntityTypeProvider.cs
    File description: EntityTypeProvider related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Defines a provider that discovers entity types derived directly from a requested base type.
    /// </summary>
    public interface IEntityTypeProvider
    {
        /// <summary>
        /// Returns discovered types whose direct base type matches the requested type.
        /// </summary>
        /// <param name="type">The base type whose direct subclasses should be returned.</param>
        /// <returns>The matching entity types.</returns>
        /// <remarks>
        /// Implementations may cache the result. The returned sequence should be treated
        /// as read-only by callers.
        /// </remarks>
        IEnumerable<Type> GetEntityTypes(Type type);
    }

    /// <summary>
    /// Discovers concrete types from runtime dependency assemblies and caches the result
    /// for subsequent lookups.
    /// </summary>
    public class DefaultEntityTypeProvider : IEntityTypeProvider
    {
        private static IList<Type> _entityTypeCache;
        /// <inheritdoc />
        public IEnumerable<Type> GetEntityTypes(Type type)
        {
            if (_entityTypeCache != null)
            {
                return _entityTypeCache.ToList();
            }

            _entityTypeCache = (from assembly in GetReferencingAssemblies()
                                from definedType in assembly.DefinedTypes
                                where definedType.BaseType == type
                                select definedType.AsType()).ToList();
            return _entityTypeCache;
        }

        /// <summary>
        /// Loads runtime-library assemblies available from the dependency context.
        /// </summary>
        /// <returns>The loaded dependency assemblies.</returns>
        /// <exception cref="FileLoadException">
        /// Propagated when a dependency assembly cannot be loaded by its assembly name.
        /// </exception>
        /// <exception cref="BadImageFormatException">
        /// Propagated when a runtime dependency is not a valid managed assembly.
        /// </exception>
        private static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            return from library in dependencies
                   select Assembly.Load(new AssemblyName(library.Name));
        }
    }
}
