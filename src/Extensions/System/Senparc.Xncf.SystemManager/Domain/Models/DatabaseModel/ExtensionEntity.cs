/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ExtensionEntity.cs
    File description: ExtensionEntity related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Senparc.Xncf.SystemManager.Domain.Models
{
    #region Legacy section
    [DataContract]
    [Serializable]
    public class AreaXML_Provinces
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string ProvinceName { get; set; }

        /// <summary>
        /// Gets or sets the administrative division code.
        /// </summary>
        [DataMember]
        public string DivisionsCode { get; set; }

        /// <summary>
        /// Gets or sets the abbreviated province name.
        /// </summary>
        [DataMember]
        public string ShortName { get; set; }

        /// <summary>
        /// Initializes one province area record.
        /// </summary>
        /// <param name="id">The province identifier.</param>
        /// <param name="provinceName">The full province name.</param>
        /// <param name="divisionsCode">The administrative division code.</param>
        /// <param name="shortName">The abbreviated name.</param>
        public AreaXML_Provinces(int id, string provinceName, string divisionsCode, string shortName)
        {
            this.ID = id;
            this.ProvinceName = provinceName;
            this.DivisionsCode = divisionsCode;
            this.ShortName = shortName;
        }
    }

    [DataContract]
    [Serializable]
    public class AreaXML_Cities
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int PID { get; set; }

        [DataMember]
        public string CityName { get; set; }

        [DataMember]
        public string ZipCode { get; set; }

        [DataMember]
        public string CityCode { get; set; }

        [DataMember]
        public int MaxShopId { get; set; }

        /// <summary>
        /// Initializes one city area record.
        /// </summary>
        /// <param name="id">The city identifier.</param>
        /// <param name="pID">The parent province identifier.</param>
        /// <param name="cityName">The city name.</param>
        /// <param name="zipCode">The postal code.</param>
        /// <param name="cityCode">The administrative city code.</param>
        /// <param name="maxShopId">The legacy maximum shop identifier.</param>
        public AreaXML_Cities(int id, int pID, string cityName, string zipCode, string cityCode, int maxShopId)
        {
            this.ID = id;
            this.PID = pID;
            this.CityName = cityName;
            this.ZipCode = zipCode;
            this.CityCode = cityCode;
            this.MaxShopId = maxShopId;
        }
    }

    [DataContract]
    [Serializable]
    public class AreaXML_Districts
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int CID { get; set; }

        [DataMember]
        public string DistrictName { get; set; }

        /// <summary>
        /// Initializes one district area record.
        /// </summary>
        /// <param name="id">The district identifier.</param>
        /// <param name="cID">The parent city identifier.</param>
        /// <param name="districtName">The district name.</param>
        public AreaXML_Districts(int id, int cID, string districtName)
        {
            this.ID = id;
            this.CID = cID;
            this.DistrictName = districtName;
        }
    }

    #endregion
}
