/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenMapicSynData.cs
    文件功能描述：SenMapicSynData 相关实现
    
    
    创建标识：Senparc - 20250113
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    public class SenMapicSynData
    {
        public SenMapicSynData()
        {
            CurrentCrawlingUrlList = new CurrentCrawlingUrlList();
        }

        //静态SearchCache
        public static SenMapicSynData Instance
        {
            get
            {
                return Nested.instance;//返回Nested类中的静态成员instance
            }
        }

        class Nested
        {
            static Nested()
            {
            }
            //将instance设为一个初始化的SearchCache新实例
            internal static readonly SenMapicSynData instance = new SenMapicSynData();
        }

        public CurrentCrawlingUrlList CurrentCrawlingUrlList { get; set; }

    }
}
