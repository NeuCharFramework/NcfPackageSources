/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AssembleScanItem.cs
    文件功能描述：AssembleScanItem 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        /// 扫描是否结束
        /// </summary>
        public bool ScanFinished { get; set; }

        /// <summary>
        /// 扫描是否成功
        /// </summary>
        public bool ScanSuccessed { get; set; }

        private Action<Assembly> _action;

        public AssembleScanItem(Action<Assembly> action)
        {
            _action = action ?? throw new ArgumentNullException("action");
        }

        private object _lock { get; set; } = new object();
        /// <summary>
        /// 执行扫描
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
