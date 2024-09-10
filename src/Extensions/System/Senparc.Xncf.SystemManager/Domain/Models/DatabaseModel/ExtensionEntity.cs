using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Senparc.Xncf.SystemManager.Domain.Models
{
    #region 省、市、区XML数据格式
    [DataContract]
    [Serializable]
    public class AreaXML_Provinces
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string ProvinceName { get; set; }

        /// <summary>
        /// 地区代码
        /// </summary>
        [DataMember]
        public string DivisionsCode { get; set; }

        /// <summary>
        /// 缩写（去掉“省”“市”“自治区”等）
        /// </summary>
        [DataMember]
        public string ShortName { get; set; }

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

        public AreaXML_Districts(int id, int cID, string districtName)
        {
            this.ID = id;
            this.CID = cID;
            this.DistrictName = districtName;
        }
    }

    #endregion
}
