/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：EnumType.cs
    文件功能描述：EnumType 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// 枚举类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumType<T> where T : struct
    {
        public T Value { get; set; }
        public EnumType() { }

        public EnumType(T value)
        {
            Value = value;
        }
    }

}
