/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseVD.cs
    File description: BaseVD related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace Senparc.Ncf.Core.Models.VD
{
    public interface IBaseUiVD
    {
        string UserName { get; set; }

        bool IsAdmin { get; set; }

        string CurrentMenu { get; set; }

        List<Messager> MessagerList { get; set; }

        MetaCollection MetaCollection { get; set; }

        DateTime PageStartTime { get; set; }

        DateTime PageEndTime { get; set; }
    }


    public class Base_PagerVD
    {
        public int? PageIndex { get; set; }

        public int PageCount { get; set; }

        public int TotalCount { get; set; }

        /// <summary>
        /// Initializes paging metadata for a view model.
        /// </summary>
        /// <param name="pageIndex">The current page index, when available.</param>
        /// <param name="pageCount">The page size/count.</param>
        /// <param name="totalCount">The total matching record count.</param>
        public Base_PagerVD(int? pageIndex, int pageCount, int totalCount)
        {
            PageIndex = pageIndex;
            PageCount = pageCount;
            TotalCount = totalCount;
        }
    }

    public class Base_AreaXmlVD
    {
        public List<AreaXML_Provinces> Provinces { get; set; }

        public List<AreaXML_Cities> Cities { get; set; }

        public List<AreaXML_Districts> Districts { get; set; }

        public string CurrentProvince { get; set; }

        public string CurrentCity { get; set; }

        public string CurrentDistrict { get; set; }
    }


    public class LoginBarVD
    {
        public bool Logined { get; set; }

        public string Email { get; set; }

        public string DisplayName { get; set; }
    }



}
