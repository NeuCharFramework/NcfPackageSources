/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ISerializer.cs
    File description: ISerializer related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

namespace Senparc.Xncf.Dapr.Utils.Serializer
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="IngoreOptions"></param>
        /// <returns></returns>
        string SerializesJson<T>(T input, bool IngoreOptions = false);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        T DeserializesJson<T>(string input);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        object DeserializesJson(Type type, string input);
    }
}
