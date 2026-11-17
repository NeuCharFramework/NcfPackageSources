/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SenMapicSynData.cs

    File description: SenMapicSynData related implementation

    

    

    Created by: Senparc - 20250113

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

        //Legacy comment translated to English.
        public static SenMapicSynData Instance
        {
            get
            {
                return Nested.instance;//Legacy comment translated to English.
            }
        }

        class Nested
        {
            static Nested()
            {
            }
            //Legacy comment translated to English.
            internal static readonly SenMapicSynData instance = new SenMapicSynData();
        }

        public CurrentCrawlingUrlList CurrentCrawlingUrlList { get; set; }

    }
}
