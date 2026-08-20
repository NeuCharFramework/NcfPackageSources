/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：EntityTypeProvider.cs
    文件功能描述：EntityTypeProvider 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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