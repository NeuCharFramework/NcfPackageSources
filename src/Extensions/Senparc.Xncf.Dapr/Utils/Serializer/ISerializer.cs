/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ISerializer.cs
    文件功能描述：ISerializer 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Xncf.Dapr.Utils.Serializer
{
    /// <summary>
    /// 序列化接口
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 序列化T为JSON字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="IngoreOptions"></param>
        /// <returns></returns>
        string SerializesJson<T>(T input, bool IngoreOptions = false);

        /// <summary>
        /// 反序列化JSON字符串为T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        T DeserializesJson<T>(string input);

        /// <summary>
        /// 反序列化JSON字符串为object
        /// </summary>
        /// <param name="type"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        object DeserializesJson(Type type, string input);
    }
}
