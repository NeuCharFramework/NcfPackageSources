/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ExtensionEntity.Core.cs
    File description: ExtensionEntity.Core related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.28.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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

    #region Legacy section

    /// <summary>
    /// List implementation carrying page index, page size, total count, and skip metadata.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public class PagedList<T> : List<T> //where T : class /*,new()*/
    {
        /// <summary>
        /// Initializes a page and derives skip metadata from the page index and size.
        /// </summary>
        /// <param name="list">The page items.</param>
        /// <param name="pageIndex">The page index.</param>
        /// <param name="pageCount">The page size.</param>
        /// <param name="totalCount">The total matching item count.</param>
        public PagedList(List<T> list, int pageIndex, int pageCount, int totalCount) : this(list, pageIndex, pageCount, totalCount, null) { }

        /// <summary>
        /// Initializes a page with explicit or derived skipped-record metadata.
        /// </summary>
        /// <param name="list">The page items.</param>
        /// <param name="pageIndex">The page index.</param>
        /// <param name="pageCount">The page size.</param>
        /// <param name="totalCount">The total matching item count.</param>
        /// <param name="skipCount">Optional explicit skipped-record count.</param>
        public PagedList(List<T> list, int pageIndex, int pageCount, int totalCount, int? skipCount = null)
        {
            AddRange(list);
            PageIndex = pageIndex;
            PageCount = pageCount;
            TotalCount = totalCount < 0 ? list.Count : totalCount;
            SkipCount = skipCount ?? Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
        }

        /// <summary>Gets or sets the zero-based page index.</summary>
        public int PageIndex { get; set; }

        /// <summary>Gets or sets the page size used for the query.</summary>
        public int PageCount { get; set; }

        /// <summary>Gets or sets the total matching item count.</summary>
        public int TotalCount { get; set; }

        /// <summary>Gets or sets the number of records skipped before this page.</summary>
        public int SkipCount { get; set; }

        /// <summary>Gets the calculated number of pages.</summary>
        public int TotalPageNumber => Convert.ToInt32((TotalCount - 1) / PageCount) + 1;
    }

    /// <summary>
    /// Stores metadata values keyed by the NCF metadata category.
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
    /// Represents one home-page carousel item.
    /// </summary>
    public class HomeSlider
    {
        /// <summary>Gets or sets the slide identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the slide title.</summary>
        public string Title { get; set; }

        /// <summary>Gets or sets the target URL.</summary>
        public string Url { get; set; }

        /// <summary>Gets or sets the image URL.</summary>
        public string Pic { get; set; }

        /// <summary>Gets or sets the display order.</summary>
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Stores one named database connection configuration loaded from XML.
    /// </summary>
    [Serializable]
    public class SenparcConfig
    {
        /// <summary>Gets or sets the configuration identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the logical configuration name.</summary>
        public string Name { get; set; }

        //public string Host { get; set; }

        //public string DataBase { get; set; }

        //public string UserName { get; set; }

        //public string Password { get; set; }

        //public string Provider { get; set; }

        //public string ConnectionString { get; set; }

        /// <summary>Gets or sets the complete connection string.</summary>
        public string ConnectionStringFull { get; set; }

        /// <summary>Gets or sets the application path associated with the configuration.</summary>
        public string ApplicationPath { get; set; }
    }

    /// <summary>
    /// Represents a user-facing message with a type and optional close action.
    /// </summary>
    [Serializable]
    public class Messager
    {
        /// <summary>Gets or sets the message category.</summary>
        public MessageType MessageType { get; set; }

        /// <summary>Gets or sets the message text.</summary>
        public string MessageText { get; set; }

        /// <summary>Gets or sets whether the UI should show a close action.</summary>
        public bool ShowClose { get; set; }

        /// <summary>Initializes a message notification.</summary>
        public Messager(MessageType messageType, string messageText, bool showClose = true)
        {
            MessageType = messageType;
            MessageText = messageText;
            ShowClose = showClose;
        }
    }

    /// <summary>
    /// Represents one diagnostic log entry exposed to administrative views.
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

    #region Legacy section

    #region Legacy section

    /// <summary>Defines the operation used to populate a cache-backed full-entity wrapper.</summary>
    /// <typeparam name="TEntity">The source entity type.</typeparam>
    public interface IBaseFullEntity<in TEntity>
    {
        /// <summary>Populates the wrapper from the supplied entity.</summary>
        /// <param name="entity">The source entity.</param>
        void CreateEntity(TEntity entity);
    }

    [Serializable]
    public abstract class BaseFullEntity<TEntity> : IBaseFullEntity<TEntity>
    {
        public virtual string Key
        {
            get;
        }

        /// <summary>
        /// Populates this full-entity wrapper from the supplied entity through the shared full-entity cache.
        /// </summary>
        /// <param name="entity">The entity whose values should be cached.</param>
        public virtual void CreateEntity(TEntity entity)
        {
            FullEntityCache.SetFullEntityCache(this, entity);
        }

        /// <summary>
        /// Creates a new full-entity wrapper and populates it from the supplied entity.
        /// </summary>
        /// <typeparam name="T">The full-entity wrapper type to instantiate.</typeparam>
        /// <param name="entity">The entity whose values should be cached.</param>
        /// <returns>A populated full-entity wrapper.</returns>
        public static T CreateEntity<T>(TEntity entity)
        where T : BaseFullEntity<TEntity>,
        new()
        {
            T obj = new T();
            obj.CreateEntity(entity);
            return obj;
        }

        /// <summary>
        /// Creates wrappers for each source entity and populates them from the full-entity cache.
        /// </summary>
        /// <typeparam name="T">The wrapper type.</typeparam>
        /// <param name="entityList">The source entities.</param>
        /// <returns>The populated wrapper list.</returns>
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

        [AutoSetCache]
        public string FooterContent { get; set; }

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


    #region Legacy section
    [DataContract]
    [Serializable]
    public class AreaXML_Provinces
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string ProvinceName { get; set; }

        /// <summary>Gets or sets the administrative division code.</summary>
        [DataMember]
        public string DivisionsCode { get; set; }

        /// <summary>Gets or sets the abbreviated province name.</summary>
        [DataMember]
        public string ShortName { get; set; }

        /// <summary>
        /// Initializes a province area-data record.
        /// </summary>
        /// <param name="id">The province identifier.</param>
        /// <param name="provinceName">The province display name.</param>
        /// <param name="divisionsCode">The administrative division code.</param>
        /// <param name="shortName">The province short name.</param>
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
        /// Initializes a city area-data record.
        /// </summary>
        /// <param name="id">The city identifier.</param>
        /// <param name="pID">The parent province identifier.</param>
        /// <param name="cityName">The city display name.</param>
        /// <param name="zipCode">The postal code.</param>
        /// <param name="cityCode">The city code.</param>
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
        /// Initializes a district area-data record.
        /// </summary>
        /// <param name="id">The district identifier.</param>
        /// <param name="cID">The parent city identifier.</param>
        /// <param name="districtName">The district display name.</param>
        public AreaXML_Districts(int id, int cID, string districtName)
        {
            this.ID = id;
            this.CID = cID;
            this.DistrictName = districtName;
        }
    }

    #endregion
}

