/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminUserInfo.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.NeuChar.App.AppStore;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using static Dm.net.buffer.ByteArrayBuffer;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>Persists administrator credentials, profile data, and login timestamps.</summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminUserInfo) + "s")]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public partial class AdminUserInfo : EntityBase<int>
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string PasswordSalt { get; private set; }
        public string RealName { get; private set; }
        public string Phone { get; private set; }
        public string Note { get; private set; }
        public DateTime ThisLoginTime { get; private set; }
        public string ThisLoginIp { get; private set; }
        public DateTime LastLoginTime { get; private set; }
        public string LastLoginIp { get; private set; }

        /// <summary>
        /// Hashes a password using the legacy SHA-512-compatible account format.
        /// </summary>
        /// <param name="password">The plaintext or pre-hashed password.</param>
        /// <param name="salt">The account password salt.</param>
        /// <param name="isMD5Password">Whether the input is already pre-hashed.</param>
        /// <returns>The encoded password value.</returns>
        private string GetMD5Password(string password, string salt, bool isMD5Password)
        {
            string md5 = password.ToUpper().Replace("-", "");
            if (!isMD5Password)
            {
                md5 = MD5.GetMD5Code(password, "").Replace("-", ""); //originalMD5
            }
            return MD5.GetMD5Code(md5, salt).Replace("-", ""); //encrypt again
        }

        private AdminUserInfo() { }

        /// <summary>
        /// Initializes an administrator entity from the supplied create/update DTO.
        /// Missing user name, password, and salt values are generated before the password
        /// is hashed and the login timestamps are initialized.
        /// </summary>
        /// <param name="objDto">The administrator values used to initialize the entity.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="objDto"/> is null.</exception>
        public AdminUserInfo(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            objDto.UserName ??= GenerateUserName();//recorduser name
            objDto.Password ??= GeneratePassword();//Legacy comment translated to English.

            UserName = objDto.UserName;
            PasswordSalt = GeneratePasswordSalt();//Legacy comment translated to English.
            Password = GetSHA512Password(objDto.Password, PasswordSalt, true);//Legacy comment translated to English.
            RealName = objDto.RealName;
            Phone = objDto.Phone;
            Note = objDto.Note;

            var now = SystemTime.Now.LocalDateTime;
            AddTime = now;
            ThisLoginTime = now;
            LastLoginTime = now;

            //Legacy comment translated to English.
        }

        /// <summary>
        /// Hashes the supplied password using this entity's salt and compares it with
        /// the persisted password value.
        /// </summary>
        /// <param name="password">The candidate password.</param>
        /// <param name="usePasswordSaltToken">Whether the configured global password token participates in hashing.</param>
        /// <returns><see langword="true"/> when the candidate matches the persisted password.</returns>
        public bool CheckPassword(string password, bool usePasswordSaltToken = true)
        {
            var encodedPassword = GetSHA512Password(password, PasswordSalt, usePasswordSaltToken);
            return encodedPassword.Equals(Password);
        }

        /// <summary>
        /// Generates a short default administrator user name with a two-digit random suffix.
        /// </summary>
        /// <returns>A generated administrator user name.</returns>
        public string GenerateUserName()
        {
            return $"SenparcCoreAdmin{new Random().Next(100).ToString("00")}";
        }

        /// <summary>
        /// Generates a random 16-character password suitable for initial entity creation.
        /// </summary>
        /// <returns>The generated password text.</returns>
        public string GeneratePassword()
        {
            return Guid.NewGuid().ToString("n").Substring(0, 16);
        }

        /// <summary>
        /// Generates the password salt used by the administrator password hashing routine.
        /// </summary>
        /// <returns>The generated salt text.</returns>
        public string GeneratePasswordSalt()
        {
            return DateTime.Now.Ticks.ToString();
        }

        /// <summary>
        /// Produces the administrator password digest using the configured salt-token policy.
        /// Legacy MD5 compatibility is selected when the global password token is unavailable.
        /// </summary>
        /// <param name="password">The plain-text password to digest.</param>
        /// <param name="salt">The per-user salt; it must contain at least 16 characters.</param>
        /// <param name="usePasswordToken">Whether the configured global password token participates.</param>
        /// <returns>The encoded password digest.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when <paramref name="salt"/> is shorter than 16 characters.</exception>
        public string GetSHA512Password(string password, string salt, bool usePasswordToken = true)
        {
            if (salt.Length < 16)
            {
                throw new NcfExceptionBase($"{nameof(salt)} 必须大于 16 位！");
            }

            if (usePasswordToken && Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.PasswordSaltToken.IsNullOrEmpty())
            {
                return GetMD5Password(password, salt, false);
            }

            var passwordToken = usePasswordToken
                                        ? Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.PasswordSaltToken
                                        : "";

            salt += passwordToken;

            var ascii = Encoding.ASCII.GetBytes(MD5.GetMD5Code(salt, "").Replace("-", ""));

            string sha512 = password;

            var splitPoint = usePasswordToken ? Encoding.ASCII.GetBytes(passwordToken).LastOrDefault() : 50;

            for (int i = 0; i < 20; i++)
            {
                if (ascii[i] % 2 == 0)
                {
                    sha512 = EncryptHelper.GetHmacSha256(sha512, salt);
                }
                else
                {
                    if (ascii[i] > splitPoint)
                    {
                        sha512 = EncryptHelper.GetSha1(sha512);
                    }
                    else
                    {
                        sha512 = EncryptHelper.GetSha1(sha512, toUpper: true);
                    }
                }
            }

            return "g01" + sha512;
        }

        /// <summary>
        /// Applies editable administrator fields and, when supplied, replaces the
        /// password hash using the entity's existing salt before updating the timestamp.
        /// </summary>
        /// <param name="objDto">The validated administrator values to apply.</param>
        public void UpdateObject(CreateOrUpdate_AdminUserInfoDto objDto)
        {
            UserName = objDto.UserName;
            if (!objDto.Password.IsNullOrEmpty())
            {
                Password = GetSHA512Password(objDto.Password, this.PasswordSalt, true);
            }

            RealName = objDto.RealName;
            Phone = objDto.Phone;
            Note = objDto.Note;

            base.SetUpdateTime();
        }

    }
}
