/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XmlDataContext.cs
    File description: XmlDataContext related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Utilities;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Provides reflection-based CRUD access to simple XML-backed entity files.
    /// </summary>
    public partial class XmlDataContext
    {
        public string DatabaseDictionary { get; set; }

        //private const string RESET_PASSWORD_CODE = "ResetPasswordCode.xml";

        /// <summary>
        /// Gets or sets the optional file-name override used when resolving an entity XML file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Initializes a context using the default application database directory.
        /// </summary>
        public XmlDataContext()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes an XML data context with a directory and optional file-name override.
        /// </summary>
        /// <param name="databaseDictionary">The application-relative directory containing XML files.</param>
        /// <param name="fileName">An optional file name used instead of the entity type name.</param>
        public XmlDataContext(string databaseDictionary, string fileName = null)
        {
            DatabaseDictionary = string.IsNullOrEmpty(databaseDictionary) ? "~/App_Data/Database/" : databaseDictionary;
            if (!DatabaseDictionary.EndsWith("/"))
            {
                DatabaseDictionary += "/";
            }
            if (!string.IsNullOrEmpty(fileName))
            {
                FileName = fileName;
            }
        }

        /// <summary>
        /// Loads an XML document from an application-relative path.
        /// </summary>
        /// <param name="path">The application-relative XML path.</param>
        /// <returns>The loaded XML root element.</returns>
        private XElement GetXElement(string path)
        {
            return XElement.Load(this.GetMapPath(path));//path.Replace("~/", HttpRuntime.AppDomainAppPath));
        }

        /// <summary>
        /// Maps an application-relative path to the host file-system path.
        /// </summary>
        /// <param name="path">The application-relative path.</param>
        /// <returns>The mapped file-system path.</returns>
        private string GetMapPath(string path)
        {
            return ServerUtility.ContentRootMapPath(path);//path.Replace("~/", HttpRuntime.AppDomainAppPath);// _context.Server.MapPath(path);
        }



        /// <summary>
        /// Resolves the XML configuration path for an entity type, preferring a development
        /// variant when the ASP.NET Core environment is Development and that file exists.
        /// </summary>
        /// <param name="entityName">The entity type name used as the default file name.</param>
        /// <returns>The application-relative XML path.</returns>
        private string GetXmlFullApplicationPath(string entityName)
        {
            Func<string, string> getFilePath = (string fileName) => DatabaseDictionary + fileName + ".config";//Legacy comment translated to English.

            var fileName = FileName.IsNullOrEmpty() ? entityName : FileName;
            var origionalPath = getFilePath(fileName);

            //Legacy comment translated to English.
            if (System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                var tryEntityName = entityName + ".Development";
                var tryPath = getFilePath(tryEntityName);

                if (File.Exists(this.GetMapPath(tryPath)))
                {
                    origionalPath = tryPath;
                }

                //Legacy comment translated to English.
            }

            return origionalPath;
        }

        /// <summary>
        /// Loads all XML elements named after <typeparamref name="TEntity"/> and converts
        /// each element to a new entity instance.
        /// </summary>
        /// <typeparam name="TEntity">The entity type represented by the XML elements.</typeparam>
        /// <returns>The converted entity list.</returns>
        public List<TEntity> GetXmlList<TEntity>() where TEntity : class, new()
        {
            string entityName = typeof(TEntity).Name;
            string filePath = GetXmlFullApplicationPath(entityName);

            XElement xml = this.GetXElement(filePath);
            List<TEntity> results = new List<TEntity>();

            foreach (var x in xml.Elements(entityName))
            {
                TEntity result = ConvertXmlToEntity<TEntity>(x);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Converts one XML element to an entity by matching property names and parsing
        /// supported primitive, date, time, Boolean, and enum types.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to construct.</typeparam>
        /// <param name="x">The XML element containing property child elements.</param>
        /// <returns>The populated entity.</returns>
        private TEntity ConvertXmlToEntity<TEntity>(XElement x) where TEntity : class, new()
        {
            TEntity result = new TEntity();
            foreach (var prop in result.GetType().GetProperties())
            {
                string value = x.Element(prop.Name).Value;
                //prop.SetValue(result, value, null);
                switch (prop.PropertyType.Name)
                {
                    case "DateTime":
                        prop.SetValue(result, DateTime.Parse(value), null);
                        break;
                    case "TimeOnly":
                        prop.SetValue(result, TimeOnly.Parse(value), null);
                        break;
                    case "DateOnly":
                        prop.SetValue(result, DateOnly.Parse(value), null);
                        break;
                    case "Int32":
                        prop.SetValue(result, int.Parse(value), null);
                        break;
                    case "Int64":
                        prop.SetValue(result, long.Parse(value), null);
                        break;
                    case "Single":
                    case "float":
                        prop.SetValue(result, float.Parse(value), null);
                        break;
                    case "Double":
                        prop.SetValue(result, double.Parse(value), null);
                        break;
                    case "Boolean":
                        prop.SetValue(result, bool.Parse(value), null);
                        break;
                    default:
                        //Legacy comment translated to English.
                        if (prop.PropertyType.IsEnum)
                        {
                            prop.SetValue(result, Enum.Parse(prop.PropertyType, value), null);
                        }
                        else
                        {
                            prop.SetValue(result, value, null);
                        }
                        break;
                }
            }
            return result;
        }


        // The legacy ResetPasswordCodes XML collection was removed from the active context.
        //public List<ResetPasswordCode> ResetPasswordCodes
        //{
        //    get
        //    {
        //        return this.GetXmlList<ResetPasswordCode>();
        //    }
        //}
    }


    public partial class XmlDataContext
    {
        //public bool Save<TEntity>(IEnumerable<TEntity> entityList) where TEntity : class
        //{
        //    try
        //    {
        //        string entityName = typeof(TEntity).Name;
        //        XElement xml = new XElement(entityName + "s");
        //        var props = typeof(TEntity).GetProperties();

        //        foreach (var entity in entityList)
        //        {
        //            XElement xe = new XElement(entityName);

        //            foreach (var prop in props)
        //            {
        //                switch (prop.PropertyType.Name)
        //                {
        //                    case "DateTime":
        //                        xe.Add(new XElement(prop.Name, prop.GetValue(entity, null).ToString()));
        //                        break;
        //                    default:
        //                        xe.Add(new XElement(prop.Name, prop.GetValue(entity, null)));
        //                        break;
        //                }
        //            }

        //            xml.Add(xe);
        //        }

        //        xml.Save(GetMapPath(GetXmlFullApplicationPath(entityName)));

        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        /// <summary>
        /// Updates existing XML records by matching each entity's <c>Id</c> property.
        /// </summary>
        /// <typeparam name="TEntity">The entity type stored in the XML file.</typeparam>
        /// <param name="entities">The existing entities to update.</param>
        /// <returns><see langword="true"/> when all updates are saved; otherwise, <see langword="false"/>.</returns>
        public bool Save<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            try
            {
                string entityName = typeof(TEntity).Name;
                XElement xml = this.GetXElement(GetXmlFullApplicationPath(entityName));

                var props = typeof(TEntity).GetProperties();
                var idProp = typeof(TEntity).GetProperty("Id");
                if (idProp == null)
                {
                    throw new Exception("Id属性不存在！");
                }

                foreach (var entity in entities)
                {
                    int id = (int)idProp.GetValue(entity, null);

                    //Legacy comment translated to English.
                    XElement oldElement = xml.Elements(entityName).FirstOrDefault(z => z.Element("Id").Value == id.ToString());
                    if (oldElement == null)
                    {
                        throw new Exception("待更新数据不存在！");
                    }

                    //startupdate
                    foreach (var prop in props)
                    {
                        if (prop.Name == "Id")
                        {
                            continue;
                        }
                        XElement valueElement = oldElement.Element(prop.Name);
                        if (valueElement == null)
                        {
                            continue;
                        }

                        switch (prop.PropertyType.Name)
                        {
                            case "DateTime":
                            case "Int32":
                                valueElement.Value = prop.GetValue(entity, null).ToString();
                                break;
                            default:
                                valueElement.RemoveAll();
                                valueElement.Add(new XCData(prop.GetValue(entity, null).ToString()));
                                break;
                        }
                    }
                }

                //saveupdate
                xml.Save(this.GetMapPath(this.GetXmlFullApplicationPath(entityName)));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Inserts an entity into its XML file, assigning and updating the file-level maximum
        /// identifier when an <c>Id</c> property is available.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to insert.</typeparam>
        /// <param name="entity">The entity to serialize.</param>
        /// <returns><see langword="true"/> when the entity is saved; otherwise, <see langword="false"/>.</returns>
        public bool Insert<TEntity>(TEntity entity) where TEntity : class
        {
            try
            {
                string entityName = typeof(TEntity).Name;
                XElement xml = this.GetXElement(GetXmlFullApplicationPath(entityName));

                //Legacy comment translated to English.
                int maxId = -1;
                XAttribute maxIdAttr = xml.Attribute("maxId");
                if (maxIdAttr != null)
                {
                    maxId = Convert.ToInt32(maxIdAttr.Value);
                    maxId++;
                    maxIdAttr.Value = maxId.ToString();

                }

                var props = typeof(TEntity).GetProperties();
                XElement xe = new XElement(entityName);
                foreach (var prop in props)
                {
                    string name = prop.Name;
                    object value = prop.GetValue(entity, null).ToString();
                    if (maxId >= 0 && name == "Id")
                    {
                        value = maxId;
                        maxIdAttr.Value = maxId.ToString();
                        prop.SetValue(entity, maxId, null); //Legacy comment translated to English.
                    }

                    switch (prop.PropertyType.Name)
                    {
                        case "DateTime":
                        case "Int32":
                            xe.Add(new XElement(name, value));
                            break;
                        default:
                            xe.Add(new XElement(name, new XCData(value.ToString())));
                            break;
                    }
                }
                xml.Add(xe);
                xml.Save(this.GetMapPath(this.GetXmlFullApplicationPath(entityName)));
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Deletes the XML record matching an entity's <c>Id</c> property.
        /// </summary>
        /// <typeparam name="TEntity">The entity type stored in the XML file.</typeparam>
        /// <param name="entity">The entity whose identifier selects the record.</param>
        /// <returns><see langword="true"/> when the record is absent or successfully deleted; otherwise, <see langword="false"/>.</returns>
        public bool Delete<TEntity>(TEntity entity) where TEntity : class
        {
            try
            {
                string entityName = typeof(TEntity).Name;
                XElement xml = this.GetXElement(GetXmlFullApplicationPath(entityName));//Legacy comment translated to English.

                var idProp = typeof(TEntity).GetProperty("Id");
                if (idProp == null)
                {
                    throw new Exception("Id属性不存在！");
                }

                int id = (int)idProp.GetValue(entity, null);
                //Legacy comment translated to English.
                XElement delElement = xml.Elements(entityName).FirstOrDefault(z => z.Element("Id").Value == id.ToString());
                if (delElement == null)
                {
                    return true;//Legacy comment translated to English.
                }
                delElement.Remove();//Legacy comment translated to English.

                xml.Save(this.GetMapPath(this.GetXmlFullApplicationPath(entityName)));
                return true;
            }
            catch //(Exception e)
            {
                //XmlDataContext ctx = new XmlDataContext(_context);
                //AutoSendEmail error = new AutoSendEmail
                //{
                //Legacy comment translated to English.
                //    Body = e.Message + "\r\n" + e.StackTrace,
                //    LastSendTime = DateTime.Now,
                //    UserName = "System",
                //    SendCount = 505,
                //    Address = "szw2003@163.com",
                //};
                //ctx.Insert(error);

                return false;
            }
        }

        /// <summary>
        /// Gets one XML record by its <c>Id</c> value.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to construct.</typeparam>
        /// <param name="id">The identifier to match.</param>
        /// <returns>The converted entity, or <see langword="null"/> when no record exists or conversion fails.</returns>
        public TEntity GetItem<TEntity>(object id) where TEntity : class, new()
        {
            try
            {
                string entityName = typeof(TEntity).Name;
                XElement xml = this.GetXElement(GetXmlFullApplicationPath(entityName));//Legacy comment translated to English.

                var idProp = typeof(TEntity).GetProperty("Id");
                if (idProp == null)
                {
                    throw new Exception("Id属性不存在！");
                }

                //Legacy comment translated to English.
                XElement item = xml.Elements(entityName).FirstOrDefault(z => z.Element("Id").Value == id.ToString());
                if (item == null)
                {
                    return null;//Legacy comment translated to English.
                }

                TEntity result = ConvertXmlToEntity<TEntity>(item);
                return result;
            }
            catch //(Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Retains only the newest requested number of records in an entity XML file.
        /// </summary>
        /// <typeparam name="TEntity">The entity type stored in the XML file.</typeparam>
        /// <param name="retainItemCount">The number of newest records to keep.</param>
        public void RetainItems<TEntity>(int retainItemCount) where TEntity : class, new()
        {
            string entityName = typeof(TEntity).Name;
            XElement xml = this.GetXElement(GetXmlFullApplicationPath(entityName));//Legacy comment translated to English.

            var elements = xml.Elements(entityName);
            if (elements.Count() >= retainItemCount)
            {
                elements.Take(elements.Count() - retainItemCount).Remove();
            }
            xml.Save(this.GetMapPath(this.GetXmlFullApplicationPath(entityName)));
        }

        /// <summary>
        /// Creates an empty XML database file for an entity type when the mapped file is absent.
        /// </summary>
        /// <typeparam name="TEntity">The entity type whose XML file should be created.</typeparam>
        public void TryCreateDataBase<TEntity>()
        {
            string entityName = typeof(TEntity).Name;

            //Legacy comment translated to English.
            var filePath = GetXmlFullApplicationPath(entityName);
            if (!File.Exists(filePath))
            {
                var xml = new XElement(entityName + "s", new XAttribute("maxId", 0));
                xml.Save(this.GetMapPath(this.GetXmlFullApplicationPath(entityName)));
            }
        }
    }


    #region Legacy section

    // The former ResetPasswordCode entity is retained only in migration history.
    //public partial class ResetPasswordCode
    //{
    //    public string UserName { get; set; }
    //    public string Code { get; set; }
    //    public DateTime CreateTime { get; set; }
    //}

    #endregion
}
