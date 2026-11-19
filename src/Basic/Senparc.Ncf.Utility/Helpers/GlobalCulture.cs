/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: GlobalCulture.cs
    File description: GlobalCulture related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.21.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

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
    ///Legacy comment translated to English.
    /// </summary>
    public class GlobalCulture
    {
        private static readonly AsyncLocal<SystemLanguage?> _currentSystemLanguage = new();

        private SystemLanguage _defaultLanguage;
        private Dictionary<SystemLanguage, Action> _languageActionCollection = new Dictionary<SystemLanguage, Action>();

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
                //Legacy comment translated to English.
                _languageActionCollection[_defaultLanguage].Invoke();
            }
            else
            {
                //Legacy comment translated to English.
                _languageActionCollection.Values.First().Invoke();
            }
        }
    }

}
