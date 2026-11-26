/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Validator.cs
    File description: Validator related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.27.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.Core.Validator
{
    /// <summary>
    /// Carries a value through fluent validation rules while collecting errors in MVC ModelState.
    /// </summary>
    public class ValidatorContainer<T>
    {
        //private List<string> _errorList;

        public T ValidatorObject { get; set; }
        public string ValueName { get; set; }
        public bool Stopped { get; set; }
        public bool IsValid { get; set; }
        public string HtmlName { get; set; }
        public ModelStateDictionary ModelState { get; set; }
        public IValidatorEnvironment ValidatorEnvironment { get; set; }

        //public List<string> ErrorList
        //{
        //    get { return _errorList; }
        //    set
        //    {
        //        _errorList = value;
        //        this.IsValid = false;
        //    }
        //}

        /// <summary>
        /// Initializes a validation container for one value and ModelState field.
        /// </summary>
        /// <param name="validatorEnvironment">The validation environment and ModelState owner.</param>
        /// <param name="validatorEnvironmentObject">The value being validated.</param>
        /// <param name="valueName">The display name used in messages.</param>
        /// <param name="htmlName">The ModelState key.</param>
        public ValidatorContainer(IValidatorEnvironment validatorEnvironment, T validatorEnvironmentObject, string valueName, string htmlName)
        {
            ValidatorObject = validatorEnvironmentObject;
            ValueName = valueName;
            HtmlName = htmlName;
            ModelState = validatorEnvironment.ModelState;
            ValidatorEnvironment = validatorEnvironment;
            IsValid = true;
            //ErrorList = new List<string>();
        }

        /// <summary>
        /// Adds a ModelState error and marks this validation container invalid.
        /// </summary>
        /// <param name="errorMessage">The message recorded for the field.</param>
        public void AddError(string errorMessage)
        {
            this.ModelState.AddModelError(this.HtmlName, errorMessage);
            this.IsValid = false;
        }
    }

    public static class ValidatorExtension
    {
        /// <summary>
        /// Creates a validation container without optional null/empty short-circuit behavior.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="validatorEnvironment">The validation environment.</param>
        /// <param name="validatorEnvironmentObject">The value to validate.</param>
        /// <param name="valueName">The display name used in errors.</param>
        /// <param name="htmlName">The ModelState key.</param>
        /// <returns>A validation container.</returns>
        public static ValidatorContainer<T> Validator<T>(this IValidatorEnvironment validatorEnvironment, T validatorEnvironmentObject, string valueName, string htmlName)
        {
            return Validator<T>(validatorEnvironment, validatorEnvironmentObject, valueName, htmlName, null);
        }

        /// <summary>
        /// Creates a validation container and optionally treats null/empty values as an immediate condition.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="validatorEnvironment">The validation environment.</param>
        /// <param name="validatorEnvironmentObject">The value to validate.</param>
        /// <param name="valueName">The display name used in errors.</param>
        /// <param name="htmlName">The ModelState key.</param>
        /// <param name="nullOrEmptyable">When true, returns null for empty values; when false, adds a required error.</param>
        /// <returns>A validation container, or null when the configured short-circuit applies.</returns>
        public static ValidatorContainer<T> Validator<T>(this IValidatorEnvironment validatorEnvironment, T validatorEnvironmentObject, string valueName, string htmlName, bool? nullOrEmptyable = null)
        {
            if (validatorEnvironment.ModelState[htmlName] != null
                && validatorEnvironment.ModelState[htmlName].Errors.Count > 0)
            {
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                validatorEnvironment.ModelState[htmlName].Errors.Clear();
            }
            if (nullOrEmptyable != null && (validatorEnvironmentObject == null || string.IsNullOrEmpty(validatorEnvironmentObject.ToString().Trim())))
            {
                if (nullOrEmptyable == true)//Legacy comment translated to English.
                {
                    return null;//Legacy comment translated to English.
                }
                else//Legacy comment translated to English.
                {
                    var container = new ValidatorContainer<T>(validatorEnvironment, validatorEnvironmentObject, valueName, htmlName);
                    return container.NotNullOrEmpty(true);
                }
            }
            else
            {
                return new ValidatorContainer<T>(validatorEnvironment, validatorEnvironmentObject, valueName, htmlName);//notcheckwhetherforempty
            }
        }


        /// <summary>
        /// Requires the container value to be non-null and non-whitespace.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <returns>The container, or null when validation fails with stop enabled.</returns>
        public static ValidatorContainer<T> NotNullOrEmpty<T>(this ValidatorContainer<T> container)
        {
            return NotNullOrEmpty(container, true);
        }

        /// <summary>
        /// Requires the container value to be non-null and non-whitespace.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after adding an error.</param>
        /// <returns>The container, or null when <paramref name="stopWhileFail"/> is true and validation fails.</returns>
        public static ValidatorContainer<T> NotNullOrEmpty<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (container.ValidatorObject == null || string.IsNullOrEmpty(container.ValidatorObject.ToString().Trim()))
            {
                string errorMessage = string.Format(NcfCoreResource.Get("Validator.Required", "提示： {0}!"), container.ValueName);
                //container.ErrorList.Add(errorMessage);
                container.AddError(errorMessage);

                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }


        /// <summary>
        /// Adds an error when the supplied predicate returns false.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="func">The predicate evaluated against the container value.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsTrue<T>(this ValidatorContainer<T> container, Func<T, bool> func, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (!func.Invoke(container.ValidatorObject))
            {
                string errorMessage = string.Format(failMessageFormat, container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }

            return container;
        }

        /// <summary>
        /// Adds an error when the supplied predicate returns true.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="func">The predicate evaluated against the container value.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsFalse<T>(this ValidatorContainer<T> container, Func<T, bool> func, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (func.Invoke(container.ValidatorObject))
            {
                string errorMessage = string.Format(failMessageFormat, container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }

            return container;
        }

        /// <summary>
        /// Unconditionally adds a formatted validation error.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> Fail<T>(this ValidatorContainer<T> container, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            string errorMessage = string.Format(failMessageFormat, container.ValueName);
            container.AddError(errorMessage);
            if (stopWhileFail)
            {
                return null;
            }

            return container;
        }

        /// <summary>
        /// Validates the value against a case-insensitive single-line regular expression.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="expression">The regular expression.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> Regex<T>(this ValidatorContainer<T> container, string expression, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            Regex emailExpression = new Regex(expression, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!emailExpression.IsMatch((container.ValidatorObject.ToString())))
            {
                string errorMessage = string.Format(failMessageFormat, container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Rejects characters commonly treated as unsafe in SQL-like input.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsSafeSqlString<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            Regex emailExpression = new Regex(@"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (emailExpression.IsMatch((container.ValidatorObject.ToString())))
            {
                string errorMessage = string.Format(NcfCoreResource.Get("Validator.InvalidCharacters", "{0}中存在非法字符！"), container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Rejects unsafe account/user-name characters and reserved identity words.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsSafeUserInfoString<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            Regex emailExpression = new Regex(@"^\s*$|^c:\\con\\con$|[%,\*" + "\"" + @"\s\t\<\>\&]|游客|管理员|^Guest|admin", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (emailExpression.IsMatch((container.ValidatorObject.ToString())))
            {
                string errorMessage = string.Format(NcfCoreResource.Get("Validator.InvalidCharacters", "{0}中存在非法字符！"), container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Applies the legacy user-name rules for full-width spaces, spaces, colons,
        /// and disallowed punctuation.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsValidateUserInfoName<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }
            string userName = container.ValidatorObject.ToString();
            if (userName.IndexOf("　") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1 || userName.IndexOf("") != -1)
            {
                container.AddError(NcfCoreResource.Get("Validator.FullWidthSpace", "用户名中不允许包含全角空格符"));
                if (stopWhileFail)
                {
                    return null;
                }
            }
            if (userName.IndexOf(" ") != -1)
            {
                container.AddError(NcfCoreResource.Get("Validator.Space", "用户名中不允许包含空格"));
                if (stopWhileFail)
                {
                    return null;
                }
            }
            if (userName.IndexOf(":") != -1)
            {
                container.AddError(NcfCoreResource.Get("Validator.Colon", "用户名中不允许包含冒号"));
                if (stopWhileFail)
                {
                    return null;
                }
            }

            string invalidateUserName = "`~!@#$%^&*()+-=;':\",./<>?|\\";//Legacy comment translated to English.
            foreach (var item in invalidateUserName)
            {
                if (userName.Contains(item))
                {
                    container.AddError(string.Format(NcfCoreResource.Get("Validator.UsernameSpecial", "用户名中可以使用中文、英文或数字及下划线_，但不允许包含特殊符号：{0}"), invalidateUserName));
                    if (stopWhileFail)
                    {
                        return null;
                    }
                }
            }

            return container;
        }

        /// <summary>
        /// Validates the value using the built-in email format rule.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <returns>The container, or null when the default rule stops the chain.</returns>
        public static ValidatorContainer<T> IsEmail<T>(this ValidatorContainer<T> container)
        {
            return IsEmail(container, true);
        }
        /// <summary>
        /// Validates the value using the built-in email format rule.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsEmail<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            return Regex(container,
                @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                NcfCoreResource.Get("Validator.Email", "请在{0}中填写正确的Email地址！"), stopWhileFail);
        }

        /// <summary>
        /// Validates the value as a mobile phone number using the built-in format rule.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsMobile<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.

            //Legacy comment translated to English.

            return Regex(container, @"^(13[0-9]|15[012356789]|17[678]|18[0-9]|14[57])[0-9]{8}$", NcfCoreResource.Get("Validator.Phone", "请填写正确的电话号码！"), stopWhileFail);
        }

        /// <summary>
        /// Validates the value as an IP address.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <returns>The container, or null when the default rule stops the chain.</returns>
        public static ValidatorContainer<T> IsIPAddress<T>(this ValidatorContainer<T> container)
        {
            return IsIPAddress(container, true);
        }

        /// <summary>
        /// Validates the value as an IP address.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsIPAddress<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            return Regex<T>(container,
                @"(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])",
                NcfCoreResource.Get("Validator.Ip", "请在{0}中填写正确的IP地址！"), stopWhileFail);
        }

        /// <summary>
        /// Validates that the value is an available URL under the configured URL rule.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsAvailableUrl<T>(this ValidatorContainer<T> container, bool stopWhileFail)
        {
            return Regex(container, @"HTTP(S)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", NcfCoreResource.Get("Validator.Url", "请在{0}中填写正确的Url地址！"), stopWhileFail);
        }


        /// <summary>
        /// Requires the UTF-8 byte length of the value to be at most <paramref name="maxByte"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="maxByte">The maximum UTF-8 byte count.</param>
        /// <returns>The container, or null when the default rule stops the chain.</returns>
        public static ValidatorContainer<T> MaxByte<T>(this ValidatorContainer<T> container, int maxByte)
        {
            return MaxByte(container, maxByte, true);
        }
        /// <summary>
        /// Requires the UTF-8 byte length of the value to be at most <paramref name="maxByte"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="maxByte">The maximum UTF-8 byte count.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> MaxByte<T>(this ValidatorContainer<T> container, int maxByte, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (System.Text.Encoding.Default.GetByteCount(container.ValidatorObject.ToString()) > maxByte)
            {
                string errorMessage = string.Format(NcfCoreResource.Get("Validator.MaxBytes", "{0}中最多只能输入{1}字节的内容（对应{2}汉字）"), container.ValueName, maxByte, Convert.ToInt32(maxByte / 2));
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Requires the string representation length to be at most <paramref name="maxLength"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="maxLength">The maximum character length.</param>
        /// <returns>The container, or null when the default rule stops the chain.</returns>
        public static ValidatorContainer<T> MaxLength<T>(this ValidatorContainer<T> container, int maxLength)
        {
            return MaxLength(container, maxLength, true);
        }
        /// <summary>
        /// Requires the string representation length to be at most <paramref name="maxLength"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="maxLength">The maximum character length.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> MaxLength<T>(this ValidatorContainer<T> container, int maxLength, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (container.ValidatorObject.ToString().Length > maxLength)
            {
                string errorMessage = string.Format(NcfCoreResource.Get("Validator.MaxLength", "{0}中最多只能输入{1}个字符"), container.ValueName, maxLength);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }


        /// <summary>
        /// Requires the UTF-8 byte length of the value to be at least <paramref name="minByte"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="minByte">The minimum UTF-8 byte count.</param>
        /// <returns>The container, or null when the default rule stops the chain.</returns>
        public static ValidatorContainer<T> MinByte<T>(this ValidatorContainer<T> container, int minByte)
        {
            return MinByte(container, minByte, true);
        }
        /// <summary>
        /// Requires the UTF-8 byte length of the value to be at least <paramref name="minByte"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="minByte">The minimum UTF-8 byte count.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> MinByte<T>(this ValidatorContainer<T> container, int minByte, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (System.Text.Encoding.Default.GetByteCount(container.ValidatorObject.ToString()) < minByte)
            {
                string errorMessage = string.Format("{0}中至少需要入{1}字节的内容（对应{2}汉字）", container.ValueName, minByte, Convert.ToInt32(minByte / 2));
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }


        /// <summary>
        /// Requires the string representation length to be at least <paramref name="minLength"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="minLength">The minimum character length.</param>
        /// <returns>The container, or null when the default rule stops the chain.</returns>
        public static ValidatorContainer<T> MinLength<T>(this ValidatorContainer<T> container, int minLength)
        {
            return MinLength(container, minLength, true);
        }
        /// <summary>
        /// Requires the string representation length to be at least <paramref name="minLength"/>.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="minLength">The minimum character length.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> MinLength<T>(this ValidatorContainer<T> container, int minLength, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (container.ValidatorObject.ToString().Length < minLength)
            {
                string errorMessage = string.Format("{0}中至少需要输入{1}个字符", container.ValueName, minLength);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }


        /// <summary>
        /// Requires the validated value to equal the supplied comparison value.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="obj">The comparison value.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsEqual<T>(this ValidatorContainer<T> container, T obj, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (!container.ValidatorObject.Equals(obj))
            {
                string errorMessage = string.Format(failMessageFormat, container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Requires the validated value not to equal the supplied comparison value.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="obj">The comparison value.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsNotEqual<T>(this ValidatorContainer<T> container, T obj, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (container.ValidatorObject.Equals(obj))
            {
                string errorMessage = string.Format(failMessageFormat, container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Rejects values containing any of the supplied excluded substrings.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="excludeStrings">Substrings that must not occur in the value.</param>
        /// <returns>The container, or null when the default rule stops the chain.</returns>
        public static ValidatorContainer<T> Exclude<T>(this ValidatorContainer<T> container, string[] excludeStrings)
        {
            return Exclude(container, excludeStrings, true);
        }
        /// <summary>
        /// Rejects values containing any of the supplied excluded substrings.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="excludeStrings">Substrings that must not occur in the value.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> Exclude<T>(this ValidatorContainer<T> container, string[] excludeStrings, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            foreach (var str in excludeStrings)
            {
                if (container.ValidatorObject.ToString().Contains(str))
                {
                    string charts = string.Join(",", excludeStrings).Replace(" ", "空格").Replace("　", "全角空格");
                    string errorMessage = string.Format("{0}中包含了不允许使用的特殊字符：{1}", container.ValueName, str);
                    container.AddError(errorMessage);
                    if (stopWhileFail)
                    {
                        return null;
                    }
                }
                return container;
            }
            return container;
        }

        /// <summary>
        /// Validates that the value parses as a number inside optional inclusive bounds.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="min">Optional minimum numeric value.</param>
        /// <param name="max">Optional maximum numeric value.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsNumber<T>(this ValidatorContainer<T> container, float? min, float? max, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }
            min = min ?? float.MinValue;
            max = max ?? float.MaxValue;
            float tryFloat;
            if (!float.TryParse(container.ValidatorObject.ToString(), out tryFloat) || tryFloat < (float)min || tryFloat > (float)max)
            {
                string errorMessage = string.Format("请输入正确的{0}", container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Validates a check-code value using the configured check-code kind.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="checkCodeKind">The expected check-code kind.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> ValidateCheckCode<T>(this ValidatorContainer<T> container, CheckCodeKind checkCodeKind, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (container.ValidatorObject == null)
            {
                container.AddError("请输入验证码");
                if (stopWhileFail)
                {
                    return null;
                }
            }

            CheckCodeHandle checkCodeHandle = new CheckCodeHandle(checkCodeKind, container.ValidatorEnvironment.HttpContext);
            if (!checkCodeHandle.ValidateCheckCode(container.ValidatorObject.ToString()))
            {
                container.AddError("请输入正确的验证码");
                if (stopWhileFail)
                {
                    return null;
                }
            }

            return container;
        }


        /// <summary>
        /// Requires the supplied object to be null.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="obj">The object whose null state is checked.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsNull<T>(this ValidatorContainer<T> container, object obj, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (obj != null)
            {
                string errorMessage = string.Format(failMessageFormat, container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }

        /// <summary>
        /// Requires the supplied object not to be null.
        /// </summary>
        /// <typeparam name="T">The validated value type.</typeparam>
        /// <param name="container">The validation container.</param>
        /// <param name="obj">The object whose null state is checked.</param>
        /// <param name="failMessageFormat">The format string used for the error.</param>
        /// <param name="stopWhileFail">Whether to stop the fluent chain after failure.</param>
        /// <returns>The container, or null when stopping after failure.</returns>
        public static ValidatorContainer<T> IsNotNull<T>(this ValidatorContainer<T> container, object obj, string failMessageFormat, bool stopWhileFail)
        {
            if (container == null)
            {
                return null;
            }

            if (obj == null)
            {
                string errorMessage = string.Format(failMessageFormat, container.ValueName);
                container.AddError(errorMessage);
                if (stopWhileFail)
                {
                    return null;
                }
            }
            return container;
        }
    }
}
