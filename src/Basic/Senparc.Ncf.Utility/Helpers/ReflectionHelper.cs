/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ReflectionHelper.cs

    File description: ReflectionHelper related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Reflection;

namespace Senparc.Ncf.Utility.Helpers
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static T CreateInstance<T>(string assemblyName, string nameSpace, string className)
        {
            try
            {
                var ect = CreateInstance(assemblyName, nameSpace, className);
                return (T)ect;//Legacy comment translated to English.
            }
            catch
            {
                //Legacy comment translated to English.
                return default(T);
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static object CreateInstance(string assemblyName, string nameSpace, string className)
        {
            try
            {
                string fullName = nameSpace + "." + className;//Legacy comment translated to English.
                //Legacy comment translated to English.
                object ect = Assembly.Load(assemblyName).CreateInstance(fullName);//Legacy comment translated to English.
                return ect;//return
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
            }
            catch
            {
                //Legacy comment translated to English.
                return null;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="nameSpace"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        public static Type GetTypeFromName(string assemblyName, string nameSpace, string className)
        {
            string fullName = nameSpace + "." + className;//Legacy comment translated to English.
            string path = fullName + "," + assemblyName;//Legacy comment translated to English.
            Type o = Type.GetType(path);//Legacy comment translated to English.
            return o;
        }
    }
}