using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace Senparc.Web.DatabasePlant.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }

        public void OnPostGenerateCode(string srcRootDir, string projects, string note, string databaseName)
        {
            StringBuilder sb = new StringBuilder();
            var startProject = Path.Combine(srcRootDir, "DevelopmentTools", "Senparc.Web.DatabasePlant");
            var projectArr = projects.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var proj in projectArr)
            {
                var xncfName = proj.Split('.').Last();
                var projectPath = Path.Combine(srcRootDir, proj);
                var fullDatabaseName = $"{xncfName}SenparcEntities_{databaseName}";
                //@"dotnet ef migrations add Init -c AdminSenparcEntities_Sqlite -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\Sqlite";

                var cmdStr = @$"cd {projectPath}
dotnet ef migrations add {note} -c {fullDatabaseName} -s ""{startProject}"" -o ""{projectPath}\Domain\Migrations\{databaseName}""
";

                sb.AppendLine(cmdStr);
            }

            ViewData["GenerateCodeResult"] = sb.ToString();
        }
    }
}