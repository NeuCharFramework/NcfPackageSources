namespace Senparc.Xncf.Dapr.Utils.Serializer
{
    /// <summary>
    /// serialization interface
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serialize T to JSON string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="IngoreOptions"></param>
        /// <returns></returns>
        string SerializesJson<T>(T input, bool IngoreOptions = false);

        /// <summary>
        /// Deserialize JSON string to T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        T DeserializesJson<T>(string input);

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        /// <param name="type"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        object DeserializesJson(Type type, string input);
    }
}
