/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AssembleScanItem.cs
    File description: AssembleScanItem related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.Core.AssembleScan
{
    public class AssembleScanItem
    {
        /// <summary>
        /// Gets or sets a value indicating whether this scan item has completed processing.
        /// </summary>
        public bool ScanFinished { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the callback completed successfully.
        /// </summary>
        public bool ScanSuccessed { get; set; }

        private Action<Assembly> _action;

        /// <summary>
        /// Initializes a scan item with the assembly callback to execute.
        /// </summary>
        /// <param name="action">The callback invoked for the scanned assembly.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        public AssembleScanItem(Action<Assembly> action)
        {
            _action = action ?? throw new ArgumentNullException("action");
        }

        private object _lock { get; set; } = new object();
        /// <summary>
        /// Executes the scan callback once under the item's synchronization lock.
        /// Exceptions mark the scan as unsuccessful and are recorded through the NCF exception path.
        /// </summary>
        /// <param name="assembly">The assembly to inspect.</param>
        public void Run(Assembly assembly)
        {
            lock (_lock)
            {
                try
                {
                    _action(assembly);
                    ScanSuccessed &= true;
                }
                catch (Exception ex)
                {
                    ScanSuccessed = false;
                    new NcfExceptionBase("执行程序集扫描出错", ex);
                }
                finally
                {
                }
            }
        }

        /// <summary>
        /// Marks the scan item as finished.
        /// </summary>
        public void Finished()
        {
            ScanFinished = true;
        }
    }
}
