/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EnumType.cs
    File description: EnumType related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// Wraps an enum-like value for function parameter binding.
    /// </summary>
    /// <typeparam name="T">The value type, normally an enum.</typeparam>
    public class EnumType<T> where T : struct
    {
        public T Value { get; set; }
        /// <summary>Initializes the wrapper with the default value of <typeparamref name="T"/>.</summary>
        public EnumType() { }

        /// <summary>
        /// Initializes the wrapper with an explicit value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public EnumType(T value)
        {
            Value = value;
        }
    }

}
