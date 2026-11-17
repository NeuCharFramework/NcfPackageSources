/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AssembleScanItem.cs
    File description: AssembleScanItem related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///Legacy comment translated to English.
        /// </summary>
        public bool ScanFinished { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool ScanSuccessed { get; set; }

        private Action<Assembly> _action;

        public AssembleScanItem(Action<Assembly> action)
        {
            _action = action ?? throw new ArgumentNullException("action");
        }

        private object _lock { get; set; } = new object();
        /// <summary>
        /// Execute scan
        /// </summary>
        /// <param name="assembly"></param>
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

        public void Finished()
        {
            ScanFinished = true;
        }
    }
}
