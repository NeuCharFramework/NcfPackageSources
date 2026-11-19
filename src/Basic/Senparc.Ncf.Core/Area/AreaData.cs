/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AreaData.cs
    File description: AreaData related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using Microsoft.AspNetCore.Http;

namespace Senparc.Ncf.Core.Area
{
    /// <summary>
    /// District Information
    /// </summary>
    public class AreaData
    {
        private const string PROVINCES_XML_PATH = "~/App_Data/AreaData/Provinces.xml";
        private const string CITIES_XML_PATH = "~/App_Data/AreaData/Cities.xml";
        private const string DISTRICTS_XML_PATH = "~/App_Data/AreaData/Districts.xml";

        private XElement GetXmlElement(string xmlApplicationPath)
        {
            return XElement.Load(SenparcHttpContext.MapPath(xmlApplicationPath));
        }

        #region State

        /// <summary>
        /// getallprovince data (get from cache)
        /// </summary>
        /// <returns></returns>
        public List<AreaXML_Provinces> GetProvincesData()
        {
            //Check cache
            AreaDataCache_Province cacheData = new AreaDataCache_Province();

            if (cacheData.Data == null)
            {
                XElement doc = this.GetXmlElement(PROVINCES_XML_PATH);
                List<AreaXML_Provinces> dataList = (from p in doc.Elements()
                                                    //orderby p.Attribute("ID").Value
                                                    select new AreaXML_Provinces(int.Parse(p.Attribute("ID").Value),
                                                        p.Attribute("ProvinceName").Value,
                                                        p.Attribute("DivisionsCode").Value,
                                                        p.Attribute("ShortName").Value)
                ).ToList();

                cacheData.Data = dataList;
            }
            return cacheData.Data;
        }

        /// <summary>
        /// getsingleprovince information
        /// </summary>
        /// <param name="provinceName"></param>
        /// <returns></returns>
        public AreaXML_Provinces GetProvinceData(string provinceName)
        {
            return GetProvincesData().FirstOrDefault(z => z.ProvinceName == provinceName);
        }
        /// <summary>
        /// getsingleprovince information
        /// </summary>
        /// <param name="provinceID"></param>
        /// <returns></returns>
        public AreaXML_Provinces GetProvinceData(int provinceID)
        {
            return GetProvincesData().FirstOrDefault(z => z.ID == provinceID);
        }

        /// <summary>
        /// get the province information to which the specified CityName (city) belongs
        /// </summary>
        /// <param name="cityName">City cityname</param>
        /// <returns></returns>
        public AreaXML_Provinces GetProvinceDataByCityName(string cityName)
        {
            var cityInfo = this.GetCityData(cityName);
            return GetProvincesData().FirstOrDefault(z => z.ID == cityInfo.PID);
        }

        #endregion


        #region City
        /// <summary>
        /// getallcitydistrictdata (get from cache)
        /// </summary>
        /// <returns></returns>
        public List<AreaXML_Cities> GetCitiesData()
        {
            //Check cache
            AreaDataCache_City cacheData = new AreaDataCache_City();
            if (cacheData.Data == null)
            {
                XElement doc = this.GetXmlElement(CITIES_XML_PATH);
                List<AreaXML_Cities> dataList = (from p in doc.Elements()
                                                 select new AreaXML_Cities(int.Parse(p.Attribute("ID").Value),
                                                     int.Parse(p.Attribute("PID").Value),
                                                     p.Attribute("CityName").Value,
                                                     p.Attribute("ZipCode").Value,
                                                     p.Attribute("CityCode").Value,
                                                    int.Parse(p.Attribute("MaxShopId").Value))
                ).ToList();

                cacheData.Data = dataList; //Update cache
            }
            return cacheData.Data;
        }

        /// <summary>
        /// get specified PID citydata
        /// </summary>
        /// <param name="pID"></param>
        /// <returns></returns>
        public List<AreaXML_Cities> GetCitiesData(int pID)
        {
            List<AreaXML_Cities> fullCitiesData = GetCitiesData();

            return (from p in fullCitiesData
                    where (pID > 0 ? p.PID == pID : true)
                    select p).ToList();
        }

        /// <summary>
        /// get to define allcitydistrictname under province
        /// </summary>
        /// <param name="provinceName"></param>
        /// <returns></returns>
        public List<AreaXML_Cities> GetCitiesData(string provinceName)
        {
            if (!string.IsNullOrEmpty(provinceName))
            {
                List<AreaXML_Provinces> fullProvincesData = GetProvincesData();
                var provinceData = fullProvincesData.Where(p => p.ProvinceName == provinceName).FirstOrDefault();// (from p in doc.Elements() where  select p.Attribute("PID").Value).First();

                if (provinceData != null)
                    return GetCitiesData(provinceData.ID);//Province is stored in . Query the cities under it.
                else
                    return new List<AreaXML_Cities>();
            }
            else
            {
                return new List<AreaXML_Cities>();
            }
        }


        private AreaXML_Cities GetCityData(string attributeName, string value)
        {
            List<AreaXML_Cities> fullCitiesData = GetCitiesData();

            AreaXML_Cities city = (from c in fullCitiesData
                                   where c.GetType().GetProperty(attributeName).GetValue(c, null).ToString() == value//Apply to Reflection
                                   select c).FirstOrDefault();
            return city;
        }

        public AreaXML_Cities GetCityData(string cityName)
        {
            return this.GetCityData("CityName", cityName);
        }

        public AreaXML_Cities GetCityData(int cityCode)
        {
            return this.GetCityData("CityCode", cityCode.ToString());
        }

        public AreaXML_Cities GetCityDataById(int id)
        {
            return this.GetCityData("ID", id.ToString());
        }

        //public AreaXML_Cities GetCityData(string procinceName,string cityName)
        //{
        //    return this.GetCitiesData(procinceName).FirstOrDefault(z => z.CityName == cityName);
        //}


        /// <summary>
        /// Update the district number and postal code of the city
        /// </summary>
        /// <param name="cityName">City cityname</param>
        /// <param name="cityCode">District No.</param>
        /// <param name="zipCode">Postal Code</param>
        /// <returns></returns>
        public bool UpdateCityCodeAndZipCode(string cityName, int cityCode, int zipCode)
        {
            try
            {
                //TODO: It is best to apply the singleton pattern.
                XElement doc = this.GetXmlElement(CITIES_XML_PATH);//getXML Document
                string xmlFilePath = SenparcHttpContext.MapPath(CITIES_XML_PATH);//path
                string backUpXmlFilePath = xmlFilePath + "." + DateTime.Now.ToString().Replace(":", "_") + ".更新区号邮编（" + cityName + "）.bak";//Backup filepath

                var cityData = (from c in doc.Elements() where c.Attribute("CityName").Value == cityName select c).Single();
                //Backup currentdata
                //File.Copy(xmlFilePath, backUpXmlFilePath);
                cityData.Save(backUpXmlFilePath);//Backup single piece of information

                cityData.SetAttributeValue("CityCode", cityCode.ToString());//updatedistrict number
                cityData.SetAttributeValue("ZipCode", zipCode.ToString());//Update ZIP code

                //save
                doc.Save(xmlFilePath);

                //Update cache
                AreaDataCache_City areaData = new AreaDataCache_City();
                areaData.Data.Clear();//clear
                GetCitiesData();//useinvoke, update cache

                return true;
            }
            catch { return false; }


        }

        /// <summary>
        /// View cities with undefined ZIP codes
        /// </summary>
        /// <returns></returns>
        public List<AreaXML_Cities> GetWrongCityCode()
        {
            var cities = this.GetCitiesData();
            List<AreaXML_Cities> wrongCodeList = new List<AreaXML_Cities>();
            int outint = 0;
            foreach (var city in cities)
            {
                if (!int.TryParse(city.CityCode, out outint))
                {
                    wrongCodeList.Add(city);
                }
            }

            return wrongCodeList;
        }

        #endregion


        #region Area
        public List<AreaXML_Districts> GetDistrictsData()
        {
            //Check cache
            AreaDataCache_District cacheData = new AreaDataCache_District();
            if (cacheData.Data == null)
            {
                XElement doc = this.GetXmlElement(DISTRICTS_XML_PATH);
                List<AreaXML_Districts> dataList = (from p in doc.Elements()
                                                    select new AreaXML_Districts(
                                                        int.Parse(p.Attribute("ID").Value),
                                                        int.Parse(p.Attribute("CID").Value),
                                                        p.Attribute("DistrictName").Value
                                                    )
                ).ToList();

                cacheData.Data = dataList;
            }
            return cacheData.Data;
        }

        /// <summary>
        /// get all district data under city CID
        /// </summary>
        /// <param name="cID"></param>
        /// <returns></returns>
        public List<AreaXML_Districts> GetDistrictsData(int cID)
        {
            List<AreaXML_Districts> fullDictrictsData = GetDistrictsData();

            List<AreaXML_Districts> dictData = (from d in fullDictrictsData
                                                where (cID > 0 ? d.CID == cID : true)
                                                //orderby p.Attribute("ID")
                                                select d).ToList();

            //TODO: Add "Other District". It can be added in XML, but it doesn't seem necessary. To be discussed further.   ——5/25/2008 By TNT2
            if (dictData.Count > 0)
                dictData.Add(new AreaXML_Districts(-1, cID, "其他区"));

            return dictData;
        }

        /// <summary>
        /// get the district data for all districts under the city citynameCityName
        /// </summary>
        /// <param name="cityName"></param>
        /// <returns></returns>
        public List<AreaXML_Districts> GetDistrictsData(string cityName)
        {
            if (!string.IsNullOrEmpty(cityName))
            {
                List<AreaXML_Cities> fullCitiesData = GetCitiesData();
                AreaXML_Cities cityDta = fullCitiesData.Where(c => c.CityName == cityName).FirstOrDefault();

                if (cityDta != null)
                    return GetDistrictsData(cityDta.ID);
                else
                    return new List<AreaXML_Districts>();
            }
            else
            {
                return new List<AreaXML_Districts>();
            }
        }

        public AreaXML_Districts GetDistrictData(string cityName, string districtName)
        {
            return this.GetDistrictsData(cityName).FirstOrDefault(z => z.DistrictName == districtName);
        }

        #endregion

        /// <summary>
        /// getdefault, or the list of districts where the user is located, not the first default item in settings
        /// </summary>
        /// <param name="provinceName"></param>
        /// <param name="cityName"></param>
        /// <returns></returns>
        public Base_AreaXmlVD GetAreaDataByProvinceAndCity(string provinceName, string cityName, string districtName)
        {
            return GetAreaDataByProvinceAndCity(provinceName, cityName, districtName, null, null, null);
        }


        /// <summary>
        /// getdefault, or a table listing the districts where the user is located
        /// </summary>
        /// <param name="provinceName"></param>
        /// <param name="cityName"></param>
        /// <param name="TopProvince">Province First Item</param>
        /// <param name="TopCities">Cities First Item</param>
        /// <param name="TopDistricts">Districts First Item</param>
        /// <returns></returns>
        public Base_AreaXmlVD GetAreaDataByProvinceAndCity(string provinceName, string cityName, string districtName, AreaXML_Provinces TopProvince, AreaXML_Cities TopCities, AreaXML_Districts TopDistricts)
        {
            var vd = new Base_AreaXmlVD()
            {
                Provinces = this.GetProvincesData(),
                Cities = this.GetCitiesData(provinceName),
                Districts = this.GetDistrictsData(cityName),

                CurrentProvince = provinceName ?? "",
                CurrentCity = cityName ?? "",
                CurrentDistrict = districtName ?? ""
            };

            //Add a prompt to the first line
            if (string.IsNullOrEmpty(provinceName))
            {
                vd.Cities = this.GetCitiesData("北京市");
                vd.Districts = this.GetDistrictsData("北京市");
            }

            //Add the first item
            if (TopProvince != null)
                vd.Provinces.Insert(0, TopProvince);

            if (TopCities != null)
                vd.Cities.Insert(0, TopCities);

            if (TopDistricts != null)
                vd.Districts.Insert(0, TopDistricts);


            return vd;
        }


        /// <summary>
        /// Add district number property
        /// </summary>
        //public void AddCityCodeAttrbuite()
        //{
        //    var doc = this.GetXmlElement(CITIES_XML_PATH);

        //    var cityList = doc.Elements();

        //    GLJKDataContext ctx=new GLJKDataContext();
        //    var codeList = ctx.AreaCityCodes.ToList();
        //    foreach (var item in cityList)
        //    {
        //        ////Add property
        //        //item.SetAttributeValue("CityCode", "");
        //        //item.SetAttributeValue("MaxShopId", "100000");

        //        // City name matching
        //        //var citys = codeList.Where(s => item.CityName.Contains(s.City.Replace("Autonomous District", "").Replace("County", "").Replace("City", ""))).ToList();


        //        var citys = codeList.Where(s => item.Attribute("CityName").Value.Contains(s.City.Replace("Autonomous District","")));

        //        if (citys.Count() != 0)
        //        {
        //            var city = citys.Last();

        //            item.Attribute("CityCode").Value = city.CityCode;
        //        }
        //        else
        //        {
        //            // No city with a district number has been found
        //            System.Web.HttpContext.Current.Response.Write(
        //                "province："+this.GetProvincesData().Single(p=>p.ID==int.Parse(item.Attribute("PID").Value)).ProvinceName+" ,city："+ item.Attribute("CityName").Value+"<br />");
        //        }
        //    }
        //    doc.Save(System.Web.HttpContext.Current.Server.MapPath(CITIES_XML_PATH+".bak"));
        //}

        //public string GetCityCode(string provinceName, string cityName)
        //{
        //    var pro=GetAreaDataAll 
        //}


        /// <summary>
        /// get city/district number
        /// </summary>
        /// <param name="cityName"></param>
        /// <param name="format">whether formatting (preceded by 0)</param>
        /// <returns></returns>
        public string GetCityCode(/*string provinceName,*/ string cityName, bool format)
        {
            string cityCode = this.GetCityData(cityName).CityCode;
            return format ? "0" + cityCode : cityCode;//Needs formatting; then add 0 before the district number.
        }

        //#region ShopIdrelated

        ///// <summary>
        ///// Get an available ShopId
        ///// </summary>
        ///// <param name="cityName"></param>
        ///// <returns></returns>
        //public int GetUseableMaxShopId(string cityName)
        //{
        //    // Get the XML settings of the Shop
        //    var shopConfig = Config.XmlConfig.GetShopConfig();
        //    // Get currentShopId
        //    var cityData = this.GetCityData(cityName);
        //    int currentMaxShopId = cityData.MaxShopId;//MaxShopId of currentXML
        //    int cityCode = int.Parse(cityData.CityCode);//district number
        //    int maxShopId = currentMaxShopId;

        //    // Compare available ShopIds
        //    bool findUseableShopId = false;
        //    while (!findUseableShopId)
        //    {
        //        if (shopConfig.ForbidShopIds.Contains(maxShopId.ToString()))
        //        {
        //            // Store in the disabled ID column table, exit, and try the next ID
        //            maxShopId++;
        //            continue;
        //        }
        //        else
        //        {
        //            // Check whether there is an entry with the same ID in
        //            GLJKDataContext ctx = new GLJKDataContext();
        //            if (ctx.Shop_GetShopByCityCodeAndShopId(cityCode, maxShopId) == null)
        //            {
        //                //ID available
        //                findUseableShopId = true;
        //            }
        //            else
        //            {
        //                maxShopId++;
        //                continue;
        //            }
        //        }
        //    }

        //    return maxShopId;
        //}
        ///// <summary>
        ///// MaxShopId corresponding to the city in updateXML
        ///// </summary>
        /////<param name="cityName">City cityname</param>
        /////<param name="newMaxShopId">MaxShopId dropped by currentuse</param>
        ///// <returns></returns>
        //public bool UpdateMaxShopId(string cityName, int newMaxShopId)
        //{
        //    try
        //    {
        //        //TODO: It is best to apply the singleton pattern
        //        XElement doc = this.GetXmlElement(CITIES_XML_PATH);//get XML document
        //        string xmlFilePath = System.Web.HttpContext.Current.Server.MapPath(CITIES_XML_PATH);//path
        //        string backUpXmlFilePath = xmlFilePath + "." + DateTime.Now.ToString().Replace(":", "_") + ".updateMaxShopId（" + cityName + "）.bak";//backup filepath

        //        var cityData = (from c in doc.Elements() where c.Attribute("CityName").Value == cityName select c).Single();
        //        //int shopId = int.Parse(cityData.Attribute("MaxShopId").Value);

        //        // Backup currentdata
        //        //File.Copy(xmlFilePath, backUpXmlFilePath);
        //        cityData.Save(backUpXmlFilePath);//Back up a single data item

        //        cityData.SetAttributeValue("MaxShopId", newMaxShopId + 1);
        //        //save
        //        doc.Save(xmlFilePath);

        //        // Update cache
        //        CacheData.AreaData_City areaData = new AreaDataCache_City();
        //        areaData.Clear();//clear
        //        GetCitiesData();//use invoke, update cache

        //        return true;
        //    }
        //    catch { return false; }
        //}




        //#endregion


        /// <summary>
        /// Split province, city, and district information for querying using Country (province, city, districtname) from IPdata
        /// </summary>
        /// <param name="ipCountry"></param>
        /// <param name="provinceName"></param>
        /// <param name="cityName">When fornull is used together with notget,</param>
        /// <param name="districtName">districtNamenotget</param>
        public static void GetProvinceCityNameFromIPCountry(string ipCountry, ref string provinceName, ref string cityName, ref string districtName)
        {
            int areaStartIndex = 0;
            //province
            if (provinceName == null)
                return;
            else
                provinceName = GetProvinceAreaNameFromIPCountry(ipCountry, new string[] { "省", "自治区" }, ref areaStartIndex);

            //city
            if (cityName == null)
            {
                return;
            }
            else
            {
                if (ipCountry.IndexOf("市") != -1 && areaStartIndex == 0)
                    provinceName = cityName; //Municipality

                cityName = GetProvinceAreaNameFromIPCountry(ipCountry, new string[] { "市", "自治州" }, ref areaStartIndex);
            }

            //district
            if (districtName == null)
                return;
            else
                districtName = GetProvinceAreaNameFromIPCountry(ipCountry, new string[] { "区", "县", "市", "自治县" }, ref areaStartIndex);

        }
        /// <summary>
        /// Country (province, city, district name) via IP data
        /// </summary>
        /// <param name="ipCountry">ipCountry string for "to" from the IP database</param>
        /// <param name="areaNameList">District-level designations, such as "province," "city," "district"</param>
        /// <param name="areaStartIndex">startsearch string position cursor</param>
        /// <returns></returns>
        private static string GetProvinceAreaNameFromIPCountry(string ipCountry, string[] areaNameList, ref int areaStartIndex)
        {
            string areaNameResult = string.Empty;
            foreach (var area in areaNameList)
            {
                if (ipCountry.IndexOf(area) != -1)
                {
                    areaNameResult = ipCountry.Substring(areaStartIndex, ipCountry.IndexOf(area) - areaStartIndex + area.Length);//Obtained districtname
                    areaStartIndex += areaNameResult.Length;//String cursor, can be ignored when searching for "district"
                    break;
                }
            }
            return areaNameResult;
        }

    }
}
