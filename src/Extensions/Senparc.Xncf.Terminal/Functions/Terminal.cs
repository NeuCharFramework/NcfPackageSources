using Senparc.CO2NET.Extensions;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Senparc.Xncf.Terminal.Functions
{
    public class Terminal_Parameters : IFunctionParameter
    {
        [MaxLength(300)]
        [Description("> 命令||命令行，如：dir /?")]
        public string CommandLine { get; set; }
    }

    public class Terminal : FunctionBase
    {
        //注意：Name 必须在单个 Xncf 模块中唯一！
        public override string Name => "命令提示符";

        public override string Description => "输入Windows命令提示符中的命令,即可返回相应的结果。请注意：命令将在服务器系统中执行！";

        public override Type FunctionParameterType => typeof(Terminal_Parameters);

        public Terminal(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            var typeParam = param as Terminal_Parameters;

            FunctionResult result = new FunctionResult()
            {
                Success = true
            };

            StringBuilder sb = new StringBuilder();
            base.RecordLog(sb, "开始运行 Terminal");

            var upperCmd = typeParam.CommandLine.ToUpper();
            switch (upperCmd)
            {
                case "NCF RELEASE"://切换到发布状态

                    break;
                case "NCF DEVELOP"://切换到开发状态
                    break;
                default:
                    //TODO:需要限制一下执行的命令
                    if (!CommandFilter(typeParam.CommandLine))
                    {
                        sb.AppendLine("命令不在允许范围之内");
                        result.Log = sb.ToString();
                        result.Message = "操作失败！";
                        return result;
                    }

                    string strExecRes = string.Empty;
                    if (!string.IsNullOrEmpty(typeParam.CommandLine))
                    {
                        strExecRes = ExeCommand($"{typeParam.CommandLine}");
                    }
                    else
                    {
                        strExecRes = ExeCommand($"dir");
                    }

                    sb.AppendLine(strExecRes);
                    result.Log = sb.ToString();
                    result.Message = "操作成功！";

                    if (!string.IsNullOrEmpty(strExecRes))
                    {
                        result.Message += Environment.CommandLine + strExecRes;
                    }

                    break;
            }
            return result;
        }

        private bool CommandFilter(string commandText)
        {
            bool isRes = false;
            List<string> lstWhiteList = new List<string>
            {
                //"ASSOC",          //Displays or modifies file extension associations.
                //"ATTRIB",         //Displays or changes file attributes.
                //"BREAK",          //Sets or clears extended CTRL+C checking.
                //"BCDEDIT",        //Sets properties in boot database to control boot loading.
                //"CACLS",          //Displays or modifies access control lists (ACLs) of files.
                //"CALL",           //Calls one batch program from another.
                "CD",             //Displays the name of or changes the current directory.
                //"CHCP",           //Displays or sets the active code page number.
                "CHDIR",          //Displays the name of or changes the current directory.
                "CHKDSK",         //Checks a disk and displays a status report.
                //"CHKNTFS",        //Displays or modifies the checking of disk at boot time.
                "CLS",            //Clears the screen.
                "CMD",            //Starts a new instance of the Windows command interpreter.
                "COLOR",          //Sets the default console foreground and background colors.
                "COMP",           //Compares the contents of two files or sets of files.
                //"COMPACT",        //Displays or alters the compression of files on NTFS partitions.
                //"CONVERT",        //Converts FAT volumes to NTFS.  You cannot convert the current drive.
                "COPY",           //Copies one or more files to another location.
                "DATE",           //Displays or sets the date.
                //"DEL",            //Deletes one or more files.
                "DIR",            //Displays a list of files and subdirectories in a directory.
                "DISKPART",       //Displays or configures Disk Partition properties.
                //"DOSKEY",         //Edits command lines, recalls Windows commands, and creates macros.
                //"DRIVERQUERY",    //Displays current device driver status and properties.
                "ECHO",           //Displays messages, or turns command echoing on or off.
                //"ENDLOCAL",       //Ends localization of environment changes in a batch file.
                //"ERASE",          //Deletes one or more files.
                "EXIT",           //Quits the CMD.EXE program (command interpreter).
                "FC",             //Compares two files or sets of files, and displays the differences between them.
                "FIND",           //Searches for a text string in a file or files.
                "FINDSTR",        //Searches for strings in files.
                //"FOR",            //Runs a specified command for each file in a set of files.
                //"FORMAT",         //Formats a disk for use with Windows.
                //"FSUTIL",         //Displays or configures the file system properties.
                //"FTYPE",          //Displays or modifies file types used in file extension associations.
                //"GOTO",           //Directs the Windows command interpreter to a labeled line in a batch program.
                "GPRESULT",       //Displays Group Policy information for machine or user.
                "GRAFTABL",       //Enables Windows to display an extended character set in graphics mode.
                "HELP",           //Provides Help information for Windows commands.
                "ICACLS",         //Display, modify, backup, or restore ACLs for files and directories.
                //"IF",             //Performs conditional processing in batch programs.
                //"LABEL",          //Creates, changes, or deletes the volume label of a disk.
                "MD",             //Creates a directory.
                "MKDIR",          //Creates a directory.
                "MKLINK",         //Creates Symbolic Links and Hard Links
                //"MODE",           //Configures a system device.
                "MORE",           //Displays output one screen at a time.
                "MOVE",           //Moves one or more files from one directory to another directory.
                //"OPENFILES",      //Displays files opened by remote users for a file share.
                //"PATH",           //Displays or sets a search path for executable files.
                "PAUSE",          //Suspends processing of a batch file and displays a message.
                //"POPD",           //Restores the previous value of the current directory saved by PUSHD.
                "PRINT",          //Prints a text file.
                "PROMPT",         //Changes the Windows command prompt.
                //"PUSHD",          //Saves the current directory then changes it.
                //"RD",             //Removes a directory.
                "RECOVER",        //Recovers readable information from a bad or defective disk.
                "REM",            //Records comments(remarks) in batch files or CONFIG.SYS.
                "REN",            //Renames a file or files.
                "RENAME",         //Renames a file or files.
                "REPLACE",        //Replaces files.
                //"RMDIR",          //Removes a directory.
                "ROBOCOPY",       //Advanced utility to copy files and directory trees
                //"SET",            //Displays, sets, or removes Windows environment variables.
                //"SETLOCAL",       //Begins localization of environment changes in a batch file.
                //"SC",             //Displays or configures services(background processes).
                //"SCHTASKS",       //Schedules commands and programs to run on a computer.
                //"SHIFT",          //Shifts the position of replaceable parameters in batch files.
                //"SHUTDOWN",       //Allows proper local or remote shutdown of machine.
                //"SORT",           //Sorts input.
                "START",          //Starts a separate window to run a specified program or command.
                //"SUBST",          //Associates a path with a drive letter.
                "SYSTEMINFO",     //Displays machine specific properties and configuration.
                "TASKLIST",       //Displays all currently running tasks including services.
                //"TASKKILL",       //Kill or stop a running process or application.
                "TIME",           //Displays or sets the system time.
                "TITLE",          //Sets the window title for a CMD.EXE session.
                "TREE",           //Graphically displays the directory structure of a drive or path.
                "TYPE",           //Displays the contents of a text file.
                "VER",            //Displays the Windows version.
                //"VERIFY",         //Tells Windows whether to verify that your files are written correctly to a disk.
                "VOL",            //Displays a disk volume label and serial number.
                "XCOPY",          //Copies files and directory trees.
                "WMIC"           //Displays WMI information inside interactive command shell.
            };

            foreach (string item in lstWhiteList)
            {
                if (commandText.ToUpper().Contains(item))
                {
                    isRes = true;
                    break;
                }
                else
                {
                    continue;
                }
            }
            return isRes;
        }

        /// <summary>
        /// 执行cmd.exe命令
        /// </summary>
        /// <param name="commandText">命令文本</param>
        /// <returns>命令输出文本</returns>
        private string ExeCommand(string commandText)
        {
            return ExeCommand(new string[] { commandText });
        }
        /// <summary>
        /// 执行多条cmd.exe命令
        /// </summary>
        /// <param name="commandTexts">命令文本数组</param>
        /// <returns>命令输出文本</returns>
        private string ExeCommand(string[] commandTexts)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            string strOutput = null;
            try
            {
                p.Start();
                foreach (string item in commandTexts)
                {
                    p.StandardInput.WriteLine(item);
                }
                p.StandardInput.WriteLine("exit");
                strOutput = p.StandardOutput.ReadToEnd();
                //strOutput = Encoding.UTF8.GetString(Encoding.Default.GetBytes(strOutput));
                p.WaitForExit();
                p.Close();
            }
            catch (Exception e)
            {
                strOutput = e.Message;
            }
            return strOutput;
        }
    }
}
