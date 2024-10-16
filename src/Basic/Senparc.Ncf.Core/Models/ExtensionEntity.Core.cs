using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

//using WURFL;

namespace Senparc.Ncf.Core.Models
{

    #region 全局

    /// <summary>
    /// 分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedList<T> : List<T> //where T : class /*,new()*/
    {
        public PagedList(List<T> list, int pageIndex, int pageCount, int totalCount) : this(list, pageIndex, pageCount, totalCount, null) { }

        public PagedList(List<T> list, int pageIndex, int pageCount, int totalCount, int? skipCount = null)
        {
            AddRange(list);
            PageIndex = pageIndex;
            PageCount = pageCount;
            TotalCount = totalCount < 0 ? list.Count : totalCount;
            SkipCount = skipCount ?? Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
        }

        public int PageIndex { get; set; }

        public int PageCount { get; set; }

        public int TotalCount { get; set; }

        public int SkipCount { get; set; }

        public int TotalPageNumber => Convert.ToInt32((TotalCount - 1) / PageCount) + 1;
    }

    /// <summary>
    /// 网页Meta标签集合
    /// </summary>
    public class MetaCollection : Dictionary<MetaType, string>
    {
        //new public string this[MetaType metaType]
        //{
        //    get
        //    {
        //        if (!this.ContainsKey(metaType))
        //        {
        //            this.Add(metaType, null);
        //        }
        //        return this[metaType];
        //    }
        //    set { this[metaType] = value; }
        //}
    }

    /// <summary>
    /// 首页图片切换
    /// </summary>
    public class HomeSlider
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string Pic { get; set; }

        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// 系统配置文件
    /// </summary>
    [Serializable]
    public class SenparcConfig
    {
        public int Id { get; set; }

        public string Name { get; set; }

        //public string Host { get; set; }

        //public string DataBase { get; set; }

        //public string UserName { get; set; }

        //public string Password { get; set; }

        //public string Provider { get; set; }

        //public string ConnectionString { get; set; }

        public string ConnectionStringFull { get; set; }

        public string ApplicationPath { get; set; }
    }

    /// <summary>
    /// 全局提示消息
    /// </summary>
    [Serializable]
    public class Messager
    {
        public MessageType MessageType { get; set; }

        public string MessageText { get; set; }

        public bool ShowClose { get; set; }

        public Messager(MessageType messageType, string messageText, bool showClose = true)
        {
            MessageType = messageType;
            MessageText = messageText;
            ShowClose = showClose;
        }
    }

    /// <summary>
    /// 日志
    /// </summary>
    public class WebLog
    {
        public DateTime DateTime { get; set; }

        public string Level { get; set; }

        public string LoggerName { get; set; }

        public string Message { get; set; }

        public string Details { get; set; }

        public string ThreadName { get; set; }

        public int PageIndex { get; set; }

        public int Line { get; set; }
    }

    #endregion

    #region 数据库实体扩展

    #region FullEntity相关

    public interface IBaseFullEntity<in TEntity>
    {
        void CreateEntity(TEntity entity);
    }

    [Serializable]
    public abstract class BaseFullEntity<TEntity> : IBaseFullEntity<TEntity>
    {
        public virtual string Key
        {
            get;
        }

        public virtual void CreateEntity(TEntity entity)
        {
            FullEntityCache.SetFullEntityCache(this, entity);
        }

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="entity">实体实例</param>
        /// <returns></returns>
        public static T CreateEntity<T>(TEntity entity)
        where T : BaseFullEntity<TEntity>,
        new()
        {
            T obj = new T();
            obj.CreateEntity(entity);
            return obj;
        }

        /// <summary>
        /// 创建对象列表
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <typeparam name="TEntity">试题类型</typeparam>
        /// <param name="entityList">实体列表</param>
        /// <returns></returns>
        public static List<T> CreateList<T>(IEnumerable<TEntity> entityList)
        where T : BaseFullEntity<TEntity>,
        new()
        {
            var result = new List<T>();
            foreach (var item in entityList)
            {
                T obj = BaseFullEntity<TEntity>.CreateEntity<T>(item);
                result.Add(obj);
            }
            return result;
        }
    }

    #endregion


    [Serializable]
    public partial class FullSystemConfigBase : BaseFullEntity<SystemConfig>
    {
        [AutoSetCache]
        public string SystemName { get; set; }

        public override void CreateEntity(SystemConfig entity)
        {
            base.CreateEntity(entity);
        }
    }

    [Serializable]
    public class FullSystemConfig : FullSystemConfigBase
    {
        [AutoSetCache]
        public string MchId { get; set; }
        [AutoSetCache]
        public string MchKey { get; set; }
        [AutoSetCache]
        public string TenPayAppId { get; set; }
        [AutoSetCache]
        public bool? HideModuleManager { get; set; }

        [AutoSetCache]
        public int NeuCharDeveloperId { get; set; }

        [AutoSetCache]
        public string NeuCharAppKey { get; set; }

        [AutoSetCache]
        public string NeuCharAppSecret { get; set; }

        public override void CreateEntity(SystemConfig entity)
        {
            base.CreateEntity(entity);
        }
    }


    [Serializable]
    public partial class FullXncfModule : BaseFullEntity<XncfModule>
    {
        [AutoSetCache]
        public int Id { get; set; }
        [AutoSetCache]
        public string Name { get; set; }
        [AutoSetCache]
        public string Uid { get; set; }
        [AutoSetCache]
        public string MenuName { get; set; }
        [AutoSetCache]
        public string Version { get; set; }
        [AutoSetCache]
        public string Description { get; set; }
        [AutoSetCache]
        public string UpdateLog { get; set; }
        [AutoSetCache]
        public bool AllowRemove { get; set; }
        [AutoSetCache]
        public string MenuId { get; set; }
        [AutoSetCache]
        public XncfModules_State State { get; set; }
        [AutoSetCache]
        public DateTime AddTime { get; set; }
        [AutoSetCache]
        public DateTime LastUpdateTime { get; set; }

        public override void CreateEntity(XncfModule entity)
        {
            base.CreateEntity(entity);
        }
    }

    #endregion


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

