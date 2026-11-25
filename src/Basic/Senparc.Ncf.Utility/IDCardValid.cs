/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IDCardValid.cs
    File description: IDCardValid related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Utility
{
    /// <summary>
    /// Validates the structure and checksum of supported Chinese resident identity-card numbers.
    /// </summary>
    public class IDCardValid
    {
        /// <summary>
        /// Initializes the stateless validator.
        /// </summary>
        public IDCardValid()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        /// Determines whether an identity-card number is a valid 15-digit legacy number
        /// or an 18-digit number with a valid address prefix, birth date, and checksum.
        /// </summary>
        /// <param name="Id">The identity-card number to validate.</param>
        /// <returns><see langword="true"/> when the number passes the applicable format checks; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="NullReferenceException">Thrown when <paramref name="Id"/> is <see langword="null"/>.</exception>
        public static bool CheckIDCard(string Id)
        {
            if (Id.Length == 18)
            {
                bool check = CheckIDCard18(Id);
                return check;
            }
            else if (Id.Length == 15)
            {
                bool check = CheckIDCard15(Id);
                return check;
            }
            else
            {
                return false;
            }
        }

        #region Legacy section

        /// <summary>
        /// Validates an 18-digit identity-card number, including numeric prefix, birth date,
        /// administrative-area prefix, and the final checksum character.
        /// </summary>
        /// <param name="Id">The 18-digit identity-card number.</param>
        /// <returns><see langword="true"/> when all checks pass; otherwise, <see langword="false"/>.</returns>
        private static bool CheckIDCard18(string Id)
        {
            long n = 0;
            if (long.TryParse(Id.Remove(17), out n) == false || n < Math.Pow(10, 16) || long.TryParse(Id.Replace('x', '0').Replace('X', '0'), out n) == false)
            {
                return false;
            }
            string address = "11x22x35x44x53x12x23x36x45x54x13x31x37x46x61x14x32x41x50x62x15x33x42x51x63x21x34x43x52x64x65x71x81x82x91";
            if (address.IndexOf(Id.Remove(2)) == -1)
            {
                return false;
            }
            string birth = Id.Substring(6, 8).Insert(6, "-").Insert(4, "-");
            DateTime time = new DateTime();
            if (DateTime.TryParse(birth, out time) == false)
            {
                return false;
            }
            string[] arrVarifyCode = ("1,0,x,9,8,7,6,5,4,3,2").Split(',');
            string[] Wi = ("7,9,10,5,8,4,2,1,6,3,7,9,10,5,8,4,2").Split(',');
            char[] Ai = Id.Remove(17).ToCharArray();
            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                sum += int.Parse(Wi[i]) * int.Parse(Ai[i].ToString());
            }
            int y = -1;
            Math.DivRem(sum, 11, out y);
            if (arrVarifyCode[y] != Id.Substring(17, 1).ToLower())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates a 15-digit legacy identity-card number, including numeric content,
        /// administrative-area prefix, and birth date.
        /// </summary>
        /// <param name="Id">The 15-digit identity-card number.</param>
        /// <returns><see langword="true"/> when all checks pass; otherwise, <see langword="false"/>.</returns>
        private static bool CheckIDCard15(string Id)
        {
            long n = 0;
            if (long.TryParse(Id, out n) == false || n < Math.Pow(10, 14))
            {
                return false;
            }
            string address = "11x22x35x44x53x12x23x36x45x54x13x31x37x46x61x14x32x41x50x62x15x33x42x51x63x21x34x43x52x64x65x71x81x82x91";
            if (address.IndexOf(Id.Remove(2)) == -1)
            {
                return false;
            }
            string birth = Id.Substring(6, 6).Insert(4, "-").Insert(2, "-");
            DateTime time = new DateTime();
            if (DateTime.TryParse(birth, out time) == false)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
