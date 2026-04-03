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
        Chinese = 0,
        English = 1,
    }

    /// <summary>
    /// Cultural help category
    /// </summary>
    public class GlobalCulture
    {
        private static SystemLanguage? _currentSystemLanguage = null;

        private SystemLanguage _defaultLanguage;
        private Dictionary<SystemLanguage, Action> _languageActionCollection = new Dictionary<SystemLanguage, Action>();

        /// <summary>
        /// The language currently used by the system
        /// </summary>
        public static SystemLanguage CurrentLanguage
        {
            get
            {
                if (_currentSystemLanguage == null)
                {
                    CultureInfo currentCulture = CultureInfo.CurrentCulture;
                    CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
                    if (currentCulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        _currentSystemLanguage = SystemLanguage.Chinese;
                    }
                    else //if (currentCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase))
                    {
                        _currentSystemLanguage = SystemLanguage.English;
                    }
                }
                return _currentSystemLanguage.Value;
            }
            set { _currentSystemLanguage = value; }
        }

        private GlobalCulture(SystemLanguage defaultLanguage = SystemLanguage.English)
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

            if (language == CurrentLanguage)
            {
                action.Invoke();
                _invoked = true;
            }
            else
            {
                _languageActionCollection[language] = action;
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

        /// <summary>
        /// If none of the previous languages ​​match, perform the default language setting
        /// </summary>
        /// <param name="throwIfNothingIsSet">Throws an exception if no language is set</param>
        /// <param name="throwIfNotAllIsSet">How to throw an exception if all languages ​​are not set</param>
        /// <exception cref="Exception"></exception>
        public void InvokeDefault(bool throwIfNothingIsSet = false, bool throwIfNotAllIsSet = false)
        {
            if (_invoked)
            {
                return;
            }

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


            if (_languageActionCollection.ContainsKey(_defaultLanguage))
            {
                //Use default language
                _languageActionCollection[_defaultLanguage].Invoke();
            }
            else
            {
                //The default language is also not specified, and the first one currently set is taken.
                _languageActionCollection.Values.First().Invoke();
            }
        }
    }

}