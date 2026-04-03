using System;
using System.Reflection;

namespace Senparc.Ncf.Utility.Helpers
{
    /// <summary>
    ///Reflection helper class
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        ///Create object instance
        /// </summary>
        /// <typeparam name="T">The type of object to be created</typeparam>
        /// <param name="assemblyName">The name of the assembly where the type is located</param>
        /// <param name="nameSpace">The namespace where the type is located</param>
        /// <param name="className">Type name</param>
        /// <returns></returns>
        public static T CreateInstance<T>(string assemblyName, string nameSpace, string className)
        {
            try
            {
                var ect = CreateInstance(assemblyName, nameSpace, className);
                return (T)ect;//cast type
            }
            catch
            {
                //An exception occurs, returning the default value of the type
                return default(T);
            }
        }

        /// <summary>
        ///Create object instance
        /// </summary>
        /// <param name="assemblyName">The name of the assembly where the type is located</param>
        /// <param name="nameSpace">The namespace where the type is located</param>
        /// <param name="className">Type name</param>
        /// <returns></returns>
        public static object CreateInstance(string assemblyName, string nameSpace, string className)
        {
            try
            {
                string fullName = nameSpace + "." + className;//namespace.typename
                //This is the first way of writing
                object ect = Assembly.Load(assemblyName).CreateInstance(fullName);//Load the assembly and create the namespace.typename instance in the assembly
                return ect;//return
                //The following is the second way of writing
                //string path = fullName + "," + assemblyName;//namespace.typename,assembly
                //Type o = Type.GetType(path); //Load type
                //object obj = Activator.CreateInstance(o, true);//Create an instance based on the type
                //return (T)obj;//Type conversion and return
            }
            catch
            {
                //An exception occurs and null is returned.
                return null;
            }
        }

        /// <summary>
        /// Get the type based on assembly, namespace, and class name
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        public static Type GetTypeFromName(string assemblyName, string nameSpace, string className)
        {
            string fullName = nameSpace + "." + className;//namespace.typename
            string path = fullName + "," + assemblyName;//Namespace.Type name,assembly
            Type o = Type.GetType(path);//Load type
            return o;
        }
    }
}