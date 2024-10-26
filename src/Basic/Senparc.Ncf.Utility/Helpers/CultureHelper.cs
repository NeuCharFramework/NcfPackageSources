using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Utility.Helpers
{
    public enum SystemLanguage
    {
        Chinese = 1,
        English = 2,
    }

    public class LauguageActionContainer
    {
        public LauguageActionContainer(SystemLanguage language, Action action)
        {
            Language = language;
            Action = action;
        }

        public SystemLanguage Language { get; set; }
        public Action Action { get; set; }
    }

    /// <summary>
    /// 文化帮助类
    /// </summary>
    public class CultureHelper
    {
        private static SystemLanguage? CurrentSystemLanguage = null;

        private SystemLanguage _defaultLanguage;
        private Dictionary<SystemLanguage, Action> _languageActionCollection = new Dictionary<SystemLanguage, Action>();

        /// <summary>
        /// 当前系统使用的语言
        /// </summary>
        public static SystemLanguage Language
        {
            get
            {
                if (CurrentSystemLanguage == null)
                {
                    CultureInfo currentCulture = CultureInfo.CurrentCulture;
                    CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
                    if (currentCulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentSystemLanguage = SystemLanguage.Chinese;
                    }
                    else //if (currentCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentSystemLanguage = SystemLanguage.English;
                    }
                }
                return CurrentSystemLanguage.Value;
            }
        }

        internal CultureHelper(SystemLanguage defaultLanguage = SystemLanguage.English)
        {
            _defaultLanguage = defaultLanguage;
        }
        
        private bool _invoked = false;

        private void CheckAndRun(SystemLanguage language, Action action)
        {
            if (_invoked)
            {
                return;
            }

            if (language == Language)
            {
                action.Invoke();
                _invoked = true;
            }
            else
            {
                _languageActionCollection[language] = action;
            }
        }

        public CultureHelper SetChinese(Action action)
        {
            CheckAndRun(SystemLanguage.Chinese, action);
            return this;
        }

        public CultureHelper SetEnglish(Action action)
        {
            CheckAndRun(SystemLanguage.English, action);
            return this;
        }

        /// <summary>
        /// 执行默认语言设置
        /// </summary>
        /// <param name="throwIfNothingIsSet">如果未设置任何语言，则抛出异常</param>
        /// <param name="throwIfNotAllIsSet">如何未设置全所有语言，则抛出异常</param>
        /// <exception cref="Exception"></exception>
        public void InvokeDefault(bool throwIfNothingIsSet = false, bool throwIfNotAllIsSet = false)
        {
            if (_languageActionCollection.Count == 0 )
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