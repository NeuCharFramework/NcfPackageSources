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
