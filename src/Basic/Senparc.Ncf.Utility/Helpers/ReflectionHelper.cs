/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ReflectionHelper.cs
    File description: ReflectionHelper related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Reflection;

namespace Senparc.Ncf.Utility.Helpers
{
    /// <summary>
    /// Provides best-effort helpers for creating types and instances from assembly-qualified names.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Creates an instance from the named assembly, namespace, and class, then casts it
        /// to <typeparamref name="T"/>. Any loading, construction, or cast failure is converted
        /// to the default value for the requested type.
        /// </summary>
        /// <typeparam name="T">The type to which the created instance is cast.</typeparam>
        /// <param name="assemblyName">The assembly name passed to <see cref="Assembly.Load(string)"/>.</param>
        /// <param name="nameSpace">The namespace containing the target type.</param>
        /// <param name="className">The target class name without its namespace.</param>
        /// <returns>The created instance cast to <typeparamref name="T"/>, or its default value on failure.</returns>
        public static T CreateInstance<T>(string assemblyName, string nameSpace, string className)
        {
            try
            {
                var ect = CreateInstance(assemblyName, nameSpace, className);
                return (T)ect;
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Creates an instance from the named assembly, namespace, and class.
        /// Any assembly loading, type lookup, constructor, or reflection failure is suppressed.
        /// </summary>
        /// <param name="assemblyName">The assembly name passed to <see cref="Assembly.Load(string)"/>.</param>
        /// <param name="nameSpace">The namespace containing the target type.</param>
        /// <param name="className">The target class name without its namespace.</param>
        /// <returns>The created object, or <see langword="null"/> when creation fails or the type cannot be found.</returns>
        public static object CreateInstance(string assemblyName, string nameSpace, string className)
        {
            try
            {
                string fullName = nameSpace + "." + className;
                object ect = Assembly.Load(assemblyName).CreateInstance(fullName);
                return ect;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves a type from an assembly-qualified name assembled from the supplied components.
        /// Unlike <see cref="CreateInstance(string, string, string)"/>, this method does not
        /// catch resolution failures; the reflection API determines the returned result.
        /// </summary>
        /// <param name="assemblyName">The assembly name portion of the qualified type name.</param>
        /// <param name="nameSpace">The namespace containing the target type.</param>
        /// <param name="className">The target class name without its namespace.</param>
        /// <returns>The resolved type, or <see langword="null"/> when the runtime cannot resolve it.</returns>
        public static Type GetTypeFromName(string assemblyName, string nameSpace, string className)
        {
            string fullName = nameSpace + "." + className;
            string path = fullName + "," + assemblyName;
            Type o = Type.GetType(path);
            return o;
        }
    }
}
