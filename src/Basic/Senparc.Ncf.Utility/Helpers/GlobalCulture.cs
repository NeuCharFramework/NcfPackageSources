/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：GlobalCulture.cs
    文件功能描述：GlobalCulture 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.21.0-preview2 增强全局文化配置与本地化选项支持

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Senparc.Ncf.Utility.Helpers
{
    public enum SystemLanguage
    {
        Chinese = 0,
        English = 1,
        Japanese = 2,
        French = 3,
        Spanish = 4,
        Russian = 5,
    }

    /// <summary>
    /// 文化帮助类
    /// </summary>
    public class GlobalCulture
    {
        private static readonly AsyncLocal<SystemLanguage?> _currentSystemLanguage = new();

        private SystemLanguage _defaultLanguage;
        private Dictionary<SystemLanguage, Action> _languageActionCollection = new Dictionary<SystemLanguage, Action>();

        /// <summary>
        /// 当前系统使用的语言
        /// </summary>
        public static SystemLanguage CurrentLanguage
        {
            get
            {
                return _currentSystemLanguage.Value
                       ?? NcfLocalizationOptions.GetSystemLanguage(CultureInfo.CurrentUICulture);
            }
            set { _currentSystemLanguage.Value = value; }
        }

        /// <summary>
        /// 清除当前异步上下文中的显式语言覆盖，恢复为跟随 CurrentUICulture。
        /// </summary>
        public static void ResetCurrentLanguage()
        {
            _currentSystemLanguage.Value = null;
        }

        private GlobalCulture(SystemLanguage defaultLanguage = SystemLanguage.English)
        {
            _defaultLanguage = defaultLanguage;
        }

        private bool _invoked = false;

        private void CheckAndRun(SystemLanguage language, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            _languageActionCollection[language] = action;

            if (_invoked)
            {
                return;
            }

            if (language == CurrentLanguage)
            {
                action.Invoke();
                _invoked = true;
            }
        }

        public static GlobalCulture Create(SystemLanguage defaultLanguage = SystemLanguage.English)
        {
            return new GlobalCulture(defaultLanguage);
        }

        public GlobalCulture SetChinese(Action action)
        {
            CheckAndRun(SystemLanguage.Chinese, action);
            return this;
        }

        public GlobalCulture SetEnglish(Action action)
        {
            CheckAndRun(SystemLanguage.English, action);
            return this;
        }

        public GlobalCulture SetJapanese(Action action)
        {
            CheckAndRun(SystemLanguage.Japanese, action);
            return this;
        }

        public GlobalCulture SetFrench(Action action)
        {
            CheckAndRun(SystemLanguage.French, action);
            return this;
        }

        public GlobalCulture SetSpanish(Action action)
        {
            CheckAndRun(SystemLanguage.Spanish, action);
            return this;
        }

        public GlobalCulture SetRussian(Action action)
        {
            CheckAndRun(SystemLanguage.Russian, action);
            return this;
        }

        /// <summary>
        /// 如果之前的语言都不匹配，则执行默认语言设置
        /// </summary>
        /// <param name="throwIfNothingIsSet">如果未设置任何语言，则抛出异常</param>
        /// <param name="throwIfNotAllIsSet">如何未设置全所有语言，则抛出异常</param>
        /// <exception cref="Exception"></exception>
        public void InvokeDefault(bool throwIfNothingIsSet = false, bool throwIfNotAllIsSet = false)
        {
            if (_languageActionCollection.Count == 0)
            {
                if (throwIfNothingIsSet)
                {
                    throw new Exception("Please set at least one language!");
                }
                else
                {
                    return;
                }
            }

            if (throwIfNotAllIsSet && _languageActionCollection.Count != Enum.GetNames<SystemLanguage>().Length)
            {
                throw new Exception("Please set all languages!");
            }

            if (_invoked)
            {
                return;
            }


            if (_languageActionCollection.ContainsKey(_defaultLanguage))
            {
                //使用默认语言
                _languageActionCollection[_defaultLanguage].Invoke();
            }
            else
            {
                //默认语言也未指定，取当前设定的第一个
                _languageActionCollection.Values.First().Invoke();
            }
        }
    }

}
