/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicSynData.cs
    File description: SenMapicSynData related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    public class SenMapicSynData
    {
        /// <summary>
        /// Initializes shared SenMapic crawl-state storage.
        /// </summary>
        public SenMapicSynData()
        {
            CurrentCrawlingUrlList = new CurrentCrawlingUrlList();
        }

        /// <summary>
        /// Gets the process-wide shared crawl-state instance.
        /// </summary>
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
            /// <summary>
            /// Holds the lazily initialized singleton instance.
            /// </summary>
            internal static readonly SenMapicSynData instance = new SenMapicSynData();
        }

        public CurrentCrawlingUrlList CurrentCrawlingUrlList { get; set; }

    }
}
