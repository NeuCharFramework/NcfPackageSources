using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Terminal
{
    [XncfRegister]
    public class Register : XncfRegisterBase, IXncfRegister
    {
        public Register()
        { }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Terminal";
        public override string Uid => "600C608A-F99A-4B1B-A18E-8CE69BE8DA92";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.1.6";//必须填写版本号

        public override string MenuName => "终端模块";
        public override string Icon => "fa fa-terminal";
        public override string Description => $"此模块提供给开发者一个可以直接使用终端命令控制系统的模块！" +
                                      $"\r\n目前可以使用的命令如下:" +
                                      $"\r\n'CD'," +
                                      $"\r\n'CHDIR'," +
                                      $"\r\n'CHKDSK'," +
                                      $"\r\n'CLS'," +
                                      $"\r\n'CMD'," +
                                      $"\r\n'COLOR'," +
                                      $"\r\n'COMP'," +
                                      $"\r\n'COPY'," +
                                      $"\r\n'DATE'," +
                                      $"\r\n'DIR'," +
                                      $"\r\n'DISKPART'," +
                                      $"\r\n'ECHO'," +
                                      $"\r\n'EXIT'," +
                                      $"\r\n'FC'," +
                                      $"\r\n'FIND'," +
                                      $"\r\n'FINDSTR'," +
                                      $"\r\n'GPRESULT'," +
                                      $"\r\n'GRAFTABL'," +
                                      $"\r\n'HELP'," +
                                      $"\r\n'ICACLS'," +
                                      $"\r\n'MD'," +
                                      $"\r\n'MKDIR'," +
                                      $"\r\n'MKLINK'," +
                                      $"\r\n'MORE'," +
                                      $"\r\n'MOVE'," +
                                      $"\r\n'PAUSE'," +
                                      $"\r\n'PRINT'," +
                                      $"\r\n'PROMPT'," +
                                      $"\r\n'RECOVER'," +
                                      $"\r\n'REM'," +
                                      $"\r\n'REN'," +
                                      $"\r\n'RENAME'," +
                                      $"\r\n'REPLACE'," +
                                      $"\r\n'ROBOCOPY'," +
                                      $"\r\n'START'," +
                                      $"\r\n'SYSTEMINFO'," +
                                      $"\r\n'TASKLIST'," +
                                      $"\r\n'TIME'," +
                                      $"\r\n'TITLE'," +
                                      $"\r\n'TREE'," +
                                      $"\r\n'TYPE'," +
                                      $"\r\n'VER'," +
                                      $"\r\n'VOL'," +
                                      $"\r\n'XCOPY'," +
                                      $"\r\n'WMIC'";

        ///// <summary>
        ///// 注册当前模块需要支持的功能模块
        ///// </summary>
        //public override IList<Type> Functions => new[] { 
        //    typeof(Functions.Terminal),
        //};

        public override Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion
    }
}
