/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EntityTypeProvider.cs
    File description: EntityTypeProvider related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Senparc.Ncf.Core.Models
{
    public interface IEntityTypeProvider
    {
        IEnumerable<Type> GetEntityTypes(Type type);
    }

    public class DefaultEntityTypeProvider : IEntityTypeProvider
    {
        private static IList<Type> _entityTypeCache;
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

        private static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            return from library in dependencies
                   select Assembly.Load(new AssemblyName(library.Name));
        }
    }
}