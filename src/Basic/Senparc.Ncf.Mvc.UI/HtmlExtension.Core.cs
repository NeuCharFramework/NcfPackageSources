/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HtmlExtension.Core.cs
    File description: HtmlExtension.Core related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Senparc.Ncf.Core.Utility;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Senparc.Ncf.Mvc.UI
{
    public  static partial class HtmlExtension
    {
        //#region CustomSelect
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //public static HtmlString CustomDropDownListFormEnum(this HtmlHelper htmlHelper, string name, Type enumType, object selectedValue = null, string optionLabel = null, object htmlAttributes = null, bool useDescription = false, bool addBlankOption = false, string blankOptionText = null)
        //{
        //    var selectedList = GetSelectListFromEnum(enumType, selectedValue, useDescription, addBlankOption, blankOptionText);
        //    StringBuilder result = new StringBuilder();
        //    foreach (var item in selectedList)
        //    {
        //        result.AppendFormat("<img src=\"/Images/up_un.gif\" />");
        //    }
        //    return htmlHelper.DropDownList(name, selectedList, optionLabel, htmlAttributes);
        //}
        //#endregion

        #region DropDownList

        /// <summary>
        /// Renders a strongly typed dropdown whose options come from an enum type.
        /// </summary>
        /// <typeparam name="TModel">The view-model type.</typeparam>
        /// <typeparam name="TProperty">The selected enum-property type.</typeparam>
        /// <param name="htmlHelper">The MVC HTML helper.</param>
        /// <param name="expression">The selected-value expression.</param>
        /// <param name="enumType">The enum type used to build options.</param>
        /// <param name="selectedValue">The initially selected value.</param>
        /// <param name="optionLabel">Optional placeholder label.</param>
        /// <param name="htmlAttributes">Optional HTML attributes.</param>
        /// <param name="useDescription">Whether enum descriptions are used as labels.</param>
        /// <param name="addBlankOption">Whether to add a blank option.</param>
        /// <returns>The generated dropdown HTML.</returns>
        public static IHtmlContent DropDownListFormEnumFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, Type enumType, object selectedValue = null, string optionLabel = null, object htmlAttributes = null, bool useDescription = false, bool addBlankOption = false)
        {
            var selectedList = GetSelectListFromEnum(enumType, selectedValue, useDescription, addBlankOption);
            return htmlHelper.DropDownListFor(expression, selectedList, optionLabel, htmlAttributes);
        }
        /// <summary>
        /// Renders a named dropdown whose options come from an enum type.
        /// </summary>
        /// <param name="htmlHelper">The MVC HTML helper.</param>
        /// <param name="name">The generated form field name.</param>
        /// <param name="enumType">The enum type used to build options.</param>
        /// <param name="selectedValue">The initially selected value.</param>
        /// <param name="optionLabel">Optional placeholder label.</param>
        /// <param name="htmlAttributes">Optional HTML attributes.</param>
        /// <param name="useDescription">Whether enum descriptions are used as labels.</param>
        /// <param name="addBlankOption">Whether to add a blank option.</param>
        /// <param name="blankOptionText">Optional text for the blank option.</param>
        /// <returns>The generated dropdown HTML.</returns>
        public static IHtmlContent DropDownListFormEnum(this HtmlHelper htmlHelper, string name, Type enumType, object selectedValue = null, string optionLabel = null, object htmlAttributes = null, bool useDescription = false, bool addBlankOption = false, string blankOptionText = null)
        {
            var selectedList = GetSelectListFromEnum(enumType, selectedValue, useDescription, addBlankOption, blankOptionText);
            return htmlHelper.DropDownList(name, selectedList, optionLabel, htmlAttributes);
        }

        /// <summary>Gets an enum description and wraps it as HTML.</summary>
        /// <param name="htmlHelper">The MVC HTML helper.</param>
        /// <param name="enumType">The enum type.</param>
        /// <param name="item">The numeric enum value.</param>
        /// <returns>The description as an HTML string.</returns>
        public static HtmlString GetDescriptionForEnum(this HtmlHelper htmlHelper, Type enumType, int item)
        {
            return new HtmlString(enumType.GetDescriptionForEnum(item));
        }

        /// <summary>Builds a select list from enum values and optional descriptions.</summary>
        private static SelectList GetSelectListFromEnum(Type enumType, object selectedValue, bool useDescription = false, bool addBlankOption = false, string blankOptionText = null)
        {
            var dic = Extensions.GetDictionaryForEnums(enumType, useDescription, addBlankOption, blankOptionText);
            return GetSelectListFromDictionary(dic, selectedValue);
        }

        /// <summary>Converts option dictionary entries into an MVC select list.</summary>
        private static SelectList GetSelectListFromDictionary(Dictionary<string, string> dic, object selectedValue)
        {
            return new SelectList(dic, "Key", "Value", selectedValue);
        }
        #endregion


        #region Link
        //public static IHtmlString ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, bool requireAbsoluteUrl)
        //{
        //    return htmlHelper.ActionLink(linkText, actionName, controllerName, new RouteValueDictionary(), new RouteValueDictionary(), requireAbsoluteUrl);
        //}

        // more of these 

        //public static IHtmlContent ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, object routeValues, object htmlAttributes, bool requireAbsoluteUrl)
        //{
        //    if (requireAbsoluteUrl)
        //    {
        //        HttpContextBase currentContext = new HttpContextWrapper(HttpContext.Current);
        //        RouteData routeData = RouteTable.Routes.GetRouteData(currentContext);

        //        routeData.Values["controller"] = controllerName;
        //        routeData.Values["action"] = actionName;

        //        DomainRoute domainRoute = routeData.Route as DomainRoute;
        //        if (domainRoute != null)
        //        {
        //            DomainData domainData = domainRoute.GetDomainData(new RequestContext(currentContext, routeData), routeData.Values);
        //            return htmlHelper.ActionLink(linkText, actionName, controllerName, domainData.Protocol, domainData.HostName, domainData.Fragment, routeData.Values, null);
        //        }
        //    }
        //    return htmlHelper.ActionLink(linkText, actionName, controllerName, routeValues, htmlAttributes);
        //}

        //#region directly referenced code (test only)：http://www.veryhuo.com/a/view/7590.html

        //public static IHtmlContent ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, bool requireAbsoluteUrl)
        //{
        //    return htmlHelper.ActionLink(linkText, actionName, controllerName, new RouteValueDictionary(), new RouteValueDictionary(), requireAbsoluteUrl);
        //}

        //// more of these

        //public static IHtmlContent ActionLink(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes, bool requireAbsoluteUrl)
        //{
        //    if (requireAbsoluteUrl)
        //    {
        //        HttpContextBase currentContext = new HttpContextWrapper(HttpContext.Current);
        //        RouteData routeData = RouteTable.Routes.GetRouteData(currentContext);

        //        routeData.Values["controller"] = controllerName;
        //        routeData.Values["action"] = actionName;

        //        DomainRoute domainRoute = routeData.Route as DomainRoute;
        //        if (domainRoute != null)
        //        {
        //            DomainData domainData = domainRoute.GetDomainData(new RequestContext(currentContext, routeData), routeData.Values);
        //            return htmlHelper.ActionLink(linkText, actionName, controllerName, domainData.Protocol, domainData.HostName, domainData.Fragment, routeData.Values, null);
        //        }
        //    }
        //    return htmlHelper.ActionLink(linkText, actionName, controllerName, routeValues, htmlAttributes);
        //}

        //#endregion

        #endregion

    }
}
