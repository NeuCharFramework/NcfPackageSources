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
        None,
        Chinese = 1,
        English = 2,
    }

    /// <summary>
    /// 文化帮助类
    /// </summary>
    public class CultureHelper : IDisposable
    {
        private static SystemLanguage CurrentLanguage = SystemLanguage.None;

        private SystemLanguage _defaultLanguage;
        private Dictionary<SystemLanguage, Action> _languageActionCollection = new Dictionary<SystemLanguage, Action>();

        /// <summary>
        /// 当前系统使用的语言
        /// </summary>
        public static SystemLanguage Language
        {
            get
            {
                if (CurrentLanguage == SystemLanguage.None)
                {
                    CultureInfo currentCulture = CultureInfo.CurrentCulture;
                    CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
                    if (currentCulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentLanguage = SystemLanguage.Chinese;
                    }
                    else //if (currentCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentLanguage = SystemLanguage.English;
                    }
                }
                return CurrentLanguage;
            }
        }

        public CultureHelper(SystemLanguage defaultLanguage)
        {
            _defaultLanguage = defaultLanguage;
        }

        /// <summary>
        /// 启动全球化输出
        /// </summary>
        /// <param name="defaultLanguage"></param>
        /// <returns></returns>
        public static CultureHelper Global(SystemLanguage defaultLanguage = SystemLanguage.English)
        {
            return new CultureHelper(defaultLanguage);
        }

        public CultureHelper SetChinese(Action action)
        {
            _languageActionCollection[SystemLanguage.Chinese] = action;
            return this;
        }

        public CultureHelper SetEnglish(Action action)
        {
            _languageActionCollection[SystemLanguage.English] = action;
            return this;
        }

        private void Invoke(SystemLanguage language, bool throwIfNothingIsSet = false)
        {
            if (_languageActionCollection.Count == 0)
            {
                if (!throwIfNothingIsSet)
                {
                    throw new Exception($"Please set at least one language:{language}");
                }
                else
                {
                    return;
                }
            }

            if (_languageActionCollection.ContainsKey(language))
            {
                //当前语言已设置，可以使用
                _languageActionCollection[language].Invoke();
            }
            else if (_languageActionCollection.ContainsKey(_defaultLanguage))
            {
                //当前语言未设置，使用默认语言
                _languageActionCollection[_defaultLanguage].Invoke();
            }
            else
            {
                //默认语言也未指定，取当前设定的第一个
                _languageActionCollection.Values.First().Invoke();
            }
        }

        public void Dispose()
        {
            Invoke(Language);
        }
    }

}