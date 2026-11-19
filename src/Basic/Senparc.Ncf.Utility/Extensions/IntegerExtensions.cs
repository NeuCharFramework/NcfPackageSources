/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IntegerExtensions.cs
    File description: IntegerExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Extensions
{
    public static class IntegerExtensions
    {
        public static string ThousandChange(this int num)
        {
            if (num > 999)
            {
                num = num / 1000;
                return num.ToString() + "K";
            }
            else
            {
                return num.ToString();
            }
        }
    }
}
