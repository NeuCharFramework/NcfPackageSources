/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CommonVD.cs
    File description: CommonVD related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Ncf.Core.Models.VD
{
    /// <summary>
    /// View model containing province, city, and district lists plus the user's current
    /// selections and optional “all”/placeholder controls.
    /// </summary>
    public class Common_AreaListVD : Base_AreaXmlVD
    {
        /// <summary>Gets or sets the prefix used when rendering the area controls.</summary>
        public string Prefixes { get; set; }

        public string UserProvince { get; set; }

        public string UserCity { get; set; }

        public string UserDistrict { get; set; }

        public AreaXML_Provinces TopProvince { get; set; }

        public AreaXML_Cities TopCity { get; set; }

        public AreaXML_Districts TopDistrict { get; set; }

        public bool HideDistrict { get; set; }

        public bool ShowProvinceAllButton { get; set; }

        public bool ShowCityAllButton { get; set; }

        public bool ShowDistrictAllButton { get; set; }

        /// <summary>Initializes an empty area-list view model.</summary>
        public Common_AreaListVD() { }

        /// <summary>
        /// Initializes an area-list view model from existing area data and current selections.
        /// </summary>
        /// <param name="areaInfo">The source province/city/district lists.</param>
        /// <param name="userProvince">The selected province.</param>
        /// <param name="userCity">The selected city.</param>
        /// <param name="userDistrict">The selected district.</param>
        public Common_AreaListVD(Base_AreaXmlVD areaInfo, string userProvince, string userCity, string userDistrict)
            : this(null, areaInfo, userProvince, userCity, userDistrict)
        { }

        /// <summary>
        /// Initializes an area-list view model with optional display prefix, placeholder entries,
        /// district visibility, and province/city “all” buttons.
        /// </summary>
        /// <param name="prefixes">Optional field-name or rendering prefix.</param>
        /// <param name="areaInfo">The source province/city/district lists.</param>
        /// <param name="userProvince">The selected province.</param>
        /// <param name="userCity">The selected city.</param>
        /// <param name="userDistrict">The selected district.</param>
        /// <param name="topProvince">Optional first province item.</param>
        /// <param name="topCity">Optional first city item.</param>
        /// <param name="topDistrict">Optional first district item.</param>
        /// <param name="hideDistrict">Whether district selection should be hidden.</param>
        /// <param name="showProvinceAllButton">Whether to show a province-wide option.</param>
        /// <param name="showCityAllButton">Whether to show a city-wide option.</param>
        public Common_AreaListVD(string prefixes, Base_AreaXmlVD areaInfo, string userProvince, string userCity, string userDistrict, AreaXML_Provinces topProvince = null, AreaXML_Cities topCity = null, AreaXML_Districts topDistrict = null, bool hideDistrict = false, bool showProvinceAllButton = false, bool showCityAllButton = false)
        {
            Prefixes = prefixes;

            Provinces = areaInfo.Provinces;
            Cities = areaInfo.Cities;
            Districts = areaInfo.Districts;

            UserProvince = userProvince;
            UserCity = userCity;
            UserDistrict = userDistrict;

            //Legacy comment translated to English.
            topProvince = topProvince ?? new AreaXML_Provinces(-1, "", "", "");
            topCity = topCity ?? new AreaXML_Cities(-1, 0, "", "", "", 0);
            topDistrict = topDistrict ?? new AreaXML_Districts(-1, 0, "");

            TopProvince = topProvince;
            TopCity = topCity;
            TopDistrict = topDistrict;

            HideDistrict = hideDistrict;
            ShowProvinceAllButton = showProvinceAllButton;
            ShowCityAllButton = showCityAllButton;
        }
    }


    public class Common_AccountTypeListVD
    {
        public decimal AgentAreaRate { get; set; }

        public int AccountId { get; set; }

        //public List<FullAccountType> UserTypeList { get; set; }

        public int AccountTypeId { get; set; }

        public decimal AccountTypePrice { get; set; }

        public DateTime AccountTypeExpireTime { get; set; }

        public bool ShowUpgradeButton { get; set; }

        public bool AccountTypeOnOffer { get; set; }

        public string PostAction { get; set; }

        public string PostController { get; set; }

        public int MaxAppCount { get; set; }
    }
}
