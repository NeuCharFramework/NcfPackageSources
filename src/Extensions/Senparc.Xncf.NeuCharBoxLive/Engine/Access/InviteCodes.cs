/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：InviteCodes.cs
    文件功能描述：邀请码格式（NCB-XXXX-XXXX）的生成、规范化与校验


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>Invite code format (spec 7.1, contract Transport): NCB-XXXX-XXXX, 8 random characters from an unambiguous alphabet.</summary>
public static class InviteCodes
{
    /// <summary>No 0, 1, I, L or O.</summary>
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    /// <summary>A new canonical code, e.g. NCB-7KQ4-M2XD: 31^8 (about 8.5e11) possible codes from a CSPRNG.</summary>
    public static string Generate()
    {
        var sb = new StringBuilder("NCB-", 13);
        for (var i = 0; i < 8; i++)
        {
            if (i == 4) sb.Append('-');
            sb.Append(Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// The form codes are compared in (contract: case-insensitive, spaces stripped): upper case (invariant), with
    /// whitespace and dashes removed. Any Unicode dash counts, so a code pasted with an en dash still matches.
    /// </summary>
    public static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            if (!char.IsWhiteSpace(ch) && char.GetUnicodeCategory(ch) != UnicodeCategory.DashPunctuation)
                sb.Append(char.ToUpperInvariant(ch));
        return sb.ToString();
    }
}
