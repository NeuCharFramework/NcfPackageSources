using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.SqlServer;
using System;
using System.Diagnostics;

namespace Senparc.Ncf.DatabasePlant.Tests
{
    [TestClass]
    public class MigrationTests
    {
        //[TestMethod]
        public void AddMigrationTest()
        {
            var commandTexts = new[] {
                @"cd E:\SenparcÏîÄ¿\NeuCharFramework\NcfPackageSources\src\Extensions\Senparc.Xncf.XncfBuilder\Senparc.Xncf.XncfBuilder",
                @"dir",
                @"dotnet --version",
                @"dotnet ef",
                @"dotnet ef migrations add Int2 --context XncfBuilderEntities_SqlServer --output-dir Migrations/Test",
            };

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
            Console.WriteLine(strOutput);
        }

     
    }
}
