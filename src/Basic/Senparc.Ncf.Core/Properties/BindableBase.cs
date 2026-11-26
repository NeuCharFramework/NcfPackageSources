/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BindableBase.cs
    File description: BindableBase related implementation

    Created by: Senparc - 20201116

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Senparc.Ncf.Core.Entities
{
    /// <summary>
    /// Provides change notification and equality-aware property assignment for bindable models.
    /// </summary>
    [Serializable]
    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the supplied or compiler-inferred property name.
        /// </summary>
        /// <param name="propertyName">The changed property name.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }

        }

        /// <summary>
        /// Assigns a property value and raises <see cref="PropertyChanged"/> only when it changes.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="storage">The backing field.</param>
        /// <param name="value">The candidate value.</param>
        /// <param name="propertyName">The property name supplied by the compiler.</param>
        /// <returns><see langword="true"/> when the backing value changed.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
