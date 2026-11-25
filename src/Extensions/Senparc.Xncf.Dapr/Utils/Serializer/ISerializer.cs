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
    /// Defines the JSON serialization boundary used by the Dapr integration.
    /// Implementations may provide custom converters and naming rules, but must
    /// preserve the null/default behavior documented by each member.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes a value to JSON.
        /// </summary>
        /// <typeparam name="T">The runtime type of the value being serialized.</typeparam>
        /// <param name="input">The value to serialize.</param>
        /// <param name="IngoreOptions">When <see langword="true"/>, bypasses implementation-specific serializer options.</param>
        /// <returns>The serialized JSON text, or the default string value when serialization fails.</returns>
        string SerializesJson<T>(T input, bool IngoreOptions = false);

        /// <summary>
        /// Deserializes JSON text into a statically selected type.
        /// </summary>
        /// <typeparam name="T">The type to which the JSON payload is converted.</typeparam>
        /// <param name="input">The JSON payload to deserialize.</param>
        /// <returns>The deserialized value, or the default value when the input is empty or invalid.</returns>
        T DeserializesJson<T>(string input);

        /// <summary>
        /// Deserializes JSON text into a runtime-supplied type.
        /// </summary>
        /// <param name="type">The runtime type to which the JSON payload is converted.</param>
        /// <param name="input">The JSON payload to deserialize.</param>
        /// <returns>The deserialized object, or <see langword="null"/> when the input is empty or invalid.</returns>
        object DeserializesJson(Type type, string input);
    }
}
