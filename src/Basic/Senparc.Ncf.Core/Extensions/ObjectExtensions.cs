/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ObjectExtensions.cs
    File description: ObjectExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Determines whether a reference is <see langword="null"/>.
        /// </summary>
        /// <param name="obj">The reference to inspect.</param>
        /// <returns><see langword="true"/> when <paramref name="obj"/> is <see langword="null"/>.</returns>
        public static bool IsNull(object obj)
        {
            return obj == null;
        }
    }
}
