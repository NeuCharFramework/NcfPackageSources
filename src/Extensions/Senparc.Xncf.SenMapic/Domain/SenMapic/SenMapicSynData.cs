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

        //StaticSearchCache
        public static SenMapicSynData Instance
        {
            get
            {
                return Nested.instance;//Returns the static member instance in the Nested class
            }
        }

        class Nested
        {
            static Nested()
            {
            }
            //Set instance to an initialized new instance of SearchCache
            internal static readonly SenMapicSynData instance = new SenMapicSynData();
        }

        public CurrentCrawlingUrlList CurrentCrawlingUrlList { get; set; }

    }
}
