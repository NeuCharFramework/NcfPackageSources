/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ObjectExtensions.cs

    File description: ObjectExtensions related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNull(object obj)
        {
            return obj == null;
        }
    }
}