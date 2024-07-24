using Snowflake.Core;

using System;

namespace Senparc.Ncf.Core.Utility;

/// <summary>
/// 雪花Id，分布式唯一Id
/// </summary>
public static class SnowflakeUtility
{
    private static readonly IdWorker idWorker = new IdWorker(1, 1);

    /// <summary>
    /// 生成雪花Id
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <returns></returns>
    public static T NextId<T>()
    {
        long id = idWorker.NextId();

        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Int64:
                return (T)(object)id;

            case TypeCode.String:
                return (T)(object)id.ToString();

            default:
                return (T)(object)id;
        }
    }
    /// <summary>
    /// 生成雪花id
    /// </summary>
    /// <returns></returns>
    public static long NextId()
    {
        return idWorker.NextId();
    }
}
