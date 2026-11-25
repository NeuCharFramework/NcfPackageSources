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
    /// Provides a fluent way to register language-specific actions and invoke the action
    /// matching the current system language.
    /// </summary>
    public class GlobalCulture
    {
        private static readonly AsyncLocal<SystemLanguage?> _currentSystemLanguage = new();

        private SystemLanguage _defaultLanguage;
        private Dictionary<SystemLanguage, Action> _languageActionCollection = new Dictionary<SystemLanguage, Action>();

        /// <summary>
        /// Gets or sets the language used by the current asynchronous flow.
        /// When unset, the value is inferred from the current UI culture.
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
        /// Clears the language override for the current asynchronous flow.
        /// </summary>
        public static void ResetCurrentLanguage()
        {
            _currentSystemLanguage.Value = null;
        }

        /// <summary>
        /// Initializes a culture action registry with a fallback language.
        /// </summary>
        /// <param name="defaultLanguage">The language used by <see cref="InvokeDefault"/> when registered.</param>
        private GlobalCulture(SystemLanguage defaultLanguage = SystemLanguage.English)
        {
            _defaultLanguage = defaultLanguage;
        }

        private bool _invoked = false;

        /// <summary>
        /// Registers an action for a language and invokes it immediately when that language
        /// is the current language and no action has already been invoked.
        /// </summary>
        /// <param name="language">The language associated with <paramref name="action"/>.</param>
        /// <param name="action">The action to register and potentially invoke.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
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

        /// <summary>
        /// Creates a fluent language-action registry.
        /// </summary>
        /// <param name="defaultLanguage">The language used when no current-language action has been invoked.</param>
        /// <returns>A new culture action registry.</returns>
        public static GlobalCulture Create(SystemLanguage defaultLanguage = SystemLanguage.English)
        {
            return new GlobalCulture(defaultLanguage);
        }

        /// <summary>Registers an action for Chinese and returns this registry.</summary>
        /// <param name="action">The action to register.</param>
        /// <returns>This registry for fluent chaining.</returns>
        public GlobalCulture SetChinese(Action action)
        {
            CheckAndRun(SystemLanguage.Chinese, action);
            return this;
        }

        /// <summary>Registers an action for English and returns this registry.</summary>
        /// <param name="action">The action to register.</param>
        /// <returns>This registry for fluent chaining.</returns>
        public GlobalCulture SetEnglish(Action action)
        {
            CheckAndRun(SystemLanguage.English, action);
            return this;
        }

        /// <summary>Registers an action for Japanese and returns this registry.</summary>
        /// <param name="action">The action to register.</param>
        /// <returns>This registry for fluent chaining.</returns>
        public GlobalCulture SetJapanese(Action action)
        {
            CheckAndRun(SystemLanguage.Japanese, action);
            return this;
        }

        /// <summary>Registers an action for French and returns this registry.</summary>
        /// <param name="action">The action to register.</param>
        /// <returns>This registry for fluent chaining.</returns>
        public GlobalCulture SetFrench(Action action)
        {
            CheckAndRun(SystemLanguage.French, action);
            return this;
        }

        /// <summary>Registers an action for Spanish and returns this registry.</summary>
        /// <param name="action">The action to register.</param>
        /// <returns>This registry for fluent chaining.</returns>
        public GlobalCulture SetSpanish(Action action)
        {
            CheckAndRun(SystemLanguage.Spanish, action);
            return this;
        }

        /// <summary>Registers an action for Russian and returns this registry.</summary>
        /// <param name="action">The action to register.</param>
        /// <returns>This registry for fluent chaining.</returns>
        public GlobalCulture SetRussian(Action action)
        {
            CheckAndRun(SystemLanguage.Russian, action);
            return this;
        }

        /// <summary>
        /// Invokes the action for the configured default language, or the first registered language
        /// when the default has not been registered. Invocation occurs at most once.
        /// </summary>
        /// <param name="throwIfNothingIsSet">Whether to throw when no language action has been registered.</param>
        /// <param name="throwIfNotAllIsSet">Whether to throw unless every <see cref="SystemLanguage"/> has an action.</param>
        /// <exception cref="Exception">Thrown when a requested completeness condition is not met.</exception>
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
                _languageActionCollection[_defaultLanguage].Invoke();
            }
            else
            {
                _languageActionCollection.Values.First().Invoke();
            }
        }
    }

}
