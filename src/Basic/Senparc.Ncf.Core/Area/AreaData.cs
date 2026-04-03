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
    /// Area information
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

        #region Province

        /// <summary>
        /// Get all province data (from cache)
        /// </summary>
        /// <returns></returns>
        public List<AreaXML_Provinces> GetProvincesData()
        {
            // Check cache
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
        /// Get a single province record
        /// </summary>
        /// <param name="provinceName"></param>
        /// <returns></returns>
        public AreaXML_Provinces GetProvinceData(string provinceName)
        {
            return GetProvincesData().FirstOrDefault(z => z.ProvinceName == provinceName);
        }
        /// <summary>
        /// Get a single province record
        /// </summary>
        /// <param name="provinceID"></param>
        /// <returns></returns>
        public AreaXML_Provinces GetProvinceData(int provinceID)
        {
            return GetProvincesData().FirstOrDefault(z => z.ID == provinceID);
        }

        /// <summary>
        /// Get province information for the specified city name
        /// </summary>
        /// <param name="cityName">City name</param>
        /// <returns></returns>
        public AreaXML_Provinces GetProvinceDataByCityName(string cityName)
        {
            var cityInfo = this.GetCityData(cityName);
            return GetProvincesData().FirstOrDefault(z => z.ID == cityInfo.PID);
        }

        #endregion


        #region City
        /// <summary>
        /// Get all city records (from cache)
        /// </summary>
        /// <returns></returns>
        public List<AreaXML_Cities> GetCitiesData()
        {
            // Check cache
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

                cacheData.Data = dataList; // Update cache
            }
            return cacheData.Data;
        }

        /// <summary>
        /// Get city data by specified PID
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
        /// Get all city names under the specified province
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
                    return GetCitiesData(provinceData.ID);//Province exists, query its cities
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
                                   where c.GetType().GetProperty(attributeName).GetValue(c, null).ToString() == value//Applied through reflection
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
        /// Update city area code and zip code
        /// </summary>
        /// <param name="cityName">City name</param>
        /// <param name="cityCode">Area code</param>
        /// <param name="zipCode">Zip code</param>
        /// <returns></returns>
        public bool UpdateCityCodeAndZipCode(string cityName, int cityCode, int zipCode)
        {
            try
            {
                //TODO: ideally use singleton pattern
                XElement doc = this.GetXmlElement(CITIES_XML_PATH);//Get XML document
                string xmlFilePath = SenparcHttpContext.MapPath(CITIES_XML_PATH);//Path
                string backUpXmlFilePath = xmlFilePath + "." + DateTime.Now.ToString().Replace(":", "_") + ".更新区号邮编（" + cityName + "）.bak";//Backup file path

                var cityData = (from c in doc.Elements() where c.Attribute("CityName").Value == cityName select c).Single();
                //Backup current data
                //File.Copy(xmlFilePath, backUpXmlFilePath);
                cityData.Save(backUpXmlFilePath);//Backup single record

                cityData.SetAttributeValue("CityCode", cityCode.ToString());//Update area code
                cityData.SetAttributeValue("ZipCode", zipCode.ToString());//Update zip code

                //Save
                doc.Save(xmlFilePath);

                // Update cache
                AreaDataCache_City areaData = new AreaDataCache_City();
                areaData.Data.Clear();//Clear
                GetCitiesData();//Invoke to refresh cache

                return true;
            }
            catch { return false; }


        }

        /// <summary>
        /// Find cities with undefined zip code
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


        #region District
        public List<AreaXML_Districts> GetDistrictsData()
        {
            // Check cache
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
        /// Get all district records under specified city CID
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

            //TODO: add "Other District". Could be added in XML, but may not be necessary. Revisit.   --2008.5.25 By TNT2
            if (dictData.Count > 0)
                dictData.Add(new AreaXML_Districts(-1, cID, "其他区"));

            return dictData;
        }

        /// <summary>
        /// Get all district records under specified city name
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
        /// Get default or user-area list without setting a default first item
        /// </summary>
        /// <param name="provinceName"></param>
        /// <param name="cityName"></param>
        /// <returns></returns>
        public Base_AreaXmlVD GetAreaDataByProvinceAndCity(string provinceName, string cityName, string districtName)
        {
            return GetAreaDataByProvinceAndCity(provinceName, cityName, districtName, null, null, null);
        }


        /// <summary>
        /// Get default or user-area list
        /// </summary>
        /// <param name="provinceName"></param>
        /// <param name="cityName"></param>
        /// <param name="TopProvince">First province item</param>
        /// <param name="TopCities">First city item</param>
        /// <param name="TopDistricts">First district item</param>
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

            //Add first-row prompt
            if (string.IsNullOrEmpty(provinceName))
            {
                vd.Cities = this.GetCitiesData("北京市");
                vd.Districts = this.GetDistrictsData("北京市");
            }

            //Insert first item
            if (TopProvince != null)
                vd.Provinces.Insert(0, TopProvince);

            if (TopCities != null)
                vd.Cities.Insert(0, TopCities);

            if (TopDistricts != null)
                vd.Districts.Insert(0, TopDistricts);


            return vd;
        }


        /// <summary>
        /// Add area code attribute
        /// </summary>
        //public void AddCityCodeAttrbuite()
        //{
        //    var doc = this.GetXmlElement(CITIES_XML_PATH);

        //    var cityList = doc.Elements();

        //    GLJKDataContext ctx=new GLJKDataContext();
        //    var codeList = ctx.AreaCityCodes.ToList();
        //    foreach (var item in cityList)
        //    {
        //        ////Add attributes
        //        //item.SetAttributeValue("CityCode", "");
        //        //item.SetAttributeValue("MaxShopId", "100000");

        //        //Match city name
        //        //var cities = codeList.Where(s => item.CityName.Contains(s.City.Replace("autonomous region", "").Replace("county", "").Replace("city", ""))).ToList();


        //        var cities = codeList.Where(s => item.Attribute("CityName").Value.Contains(s.City.Replace("Autonomous Region","")));

        //        if (citys.Count() != 0)
        //        {
        //            var city = citys.Last();

        //            item.Attribute("CityCode").Value = city.CityCode;
        //        }
        //        else
        //        {
        //            //City with no area code found
        //            System.Web.HttpContext.Current.Response.Write(
        //                "Province:"+this.GetProvincesData().Single(p=>p.ID==int.Parse(item.Attribute("PID").Value)).ProvinceName+", City: "+ item.Attribute("CityName").Value+"<br />");
        //        }
        //    }
        //    doc.Save(System.Web.HttpContext.Current.Server.MapPath(CITIES_XML_PATH+".bak"));
        //}

        //public string GetCityCode(string provinceName, string cityName)
        //{
        //    var pro=GetAreaDataAll 
        //}


        /// <summary>
        /// Get city area code
        /// </summary>
        /// <param name="cityName"></param>
        /// <param name="format">Whether to format (prefix with 0)</param>
        /// <returns></returns>
        public string GetCityCode(/*string provinceName,*/ string cityName, bool format)
        {
            string cityCode = this.GetCityData(cityName).CityCode;
            return format ? "0" + cityCode : cityCode;//If formatting is required, prefix area code with 0
        }

        //#region ShopId related

        ///// <summary>
        ///// Get an available ShopId
        ///// </summary>
        ///// <param name="cityName"></param>
        ///// <returns></returns>
        //public int GetUseableMaxShopId(string cityName)
        //{
        //    //Get Shop XML settings
        //    var shopConfig = Config.XmlConfig.GetShopConfig();
        //    //Get current ShopId
        //    var cityData = this.GetCityData(cityName);
        //    int currentMaxShopId = cityData.MaxShopId;//Current MaxShopId in XML
        //    int cityCode = int.Parse(cityData.CityCode);//Area code
        //    int maxShopId = currentMaxShopId;

        //    //Find available ShopId
        //    bool findUseableShopId = false;
        //    while (!findUseableShopId)
        //    {
        //        if (shopConfig.ForbidShopIds.Contains(maxShopId.ToString()))
        //        {
        //            //Exists in forbidden ID list, skip and try next Id
        //            maxShopId++;
        //            continue;
        //        }
        //        else
        //        {
        //            //Check whether same ID exists
        //            GLJKDataContext ctx = new GLJKDataContext();
        //            if (ctx.Shop_GetShopByCityCodeAndShopId(cityCode, maxShopId) == null)
        //            {
        //                //ID is available
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
        ///// Update MaxShopId for corresponding city in XML
        ///// </summary>
        ///// <param name="cityName">City name</param>
        ///// <param name="newMaxShopId">Current consumed MaxShopId</param>
        ///// <returns></returns>
        //public bool UpdateMaxShopId(string cityName, int newMaxShopId)
        //{
        //    try
        //    {
        //        //TODO: ideally use singleton pattern
        //        XElement doc = this.GetXmlElement(CITIES_XML_PATH);//Get XML document
        //        string xmlFilePath = System.Web.HttpContext.Current.Server.MapPath(CITIES_XML_PATH);//Path
        //        string backUpXmlFilePath = xmlFilePath + "." + DateTime.Now.ToString().Replace(":", "_") + ".UpdateMaxShopId(" + cityName + ").bak";//Backup file path

        //        var cityData = (from c in doc.Elements() where c.Attribute("CityName").Value == cityName select c).Single();
        //        //int shopId = int.Parse(cityData.Attribute("MaxShopId").Value);

        //        //Backup current data
        //        //File.Copy(xmlFilePath, backUpXmlFilePath);
        //        cityData.Save(backUpXmlFilePath);//Backup single record

        //        cityData.SetAttributeValue("MaxShopId", newMaxShopId + 1);
        //        //Save
        //        doc.Save(xmlFilePath);

        //        // Update cache
        //        CacheData.AreaData_City areaData = new AreaDataCache_City();
        //        areaData.Clear();//Clear
        //        GetCitiesData();//Invoke to refresh cache

        //        return true;
        //    }
        //    catch { return false; }
        //}




        //#endregion


        /// <summary>
        /// Split province/city/district information from IP Country field for lookup
        /// </summary>
        /// <param name="ipCountry"></param>
        /// <param name="provinceName"></param>
        /// <param name="cityName">If null, do not parse city</param>
        /// <param name="districtName">If null, do not parse district</param>
        public static void GetProvinceCityNameFromIPCountry(string ipCountry, ref string provinceName, ref string cityName, ref string districtName)
        {
            int areaStartIndex = 0;
            //Province
            if (provinceName == null)
                return;
            else
                provinceName = GetProvinceAreaNameFromIPCountry(ipCountry, new string[] { "省", "自治区" }, ref areaStartIndex);

            //City
            if (cityName == null)
            {
                return;
            }
            else
            {
                if (ipCountry.IndexOf("市") != -1 && areaStartIndex == 0)
                    provinceName = cityName; //Municipality directly under central government

                cityName = GetProvinceAreaNameFromIPCountry(ipCountry, new string[] { "市", "自治州" }, ref areaStartIndex);
            }

            //District
            if (districtName == null)
                return;
            else
                districtName = GetProvinceAreaNameFromIPCountry(ipCountry, new string[] { "区", "县", "市", "自治县" }, ref areaStartIndex);

        }
        /// <summary>
        /// Parse area name from IP Country field
        /// </summary>
        /// <param name="ipCountry">ipCountry string from IP database</param>
        /// <param name="areaNameList">Area level tokens, such as "province" "city" "district"</param>
        /// <param name="areaStartIndex">Cursor position to start searching</param>
        /// <returns></returns>
        private static string GetProvinceAreaNameFromIPCountry(string ipCountry, string[] areaNameList, ref int areaStartIndex)
        {
            string areaNameResult = string.Empty;
            foreach (var area in areaNameList)
            {
                if (ipCountry.IndexOf(area) != -1)
                {
                    areaNameResult = ipCountry.Substring(areaStartIndex, ipCountry.IndexOf(area) - areaStartIndex + area.Length);//Get area name
                    areaStartIndex += areaNameResult.Length;//String cursor; can be ignored when searching district
                    break;
                }
            }
            return areaNameResult;
        }

    }
}
