/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.0.24 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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
                //Legacy comment translated to English.

                var cmdStr = @$"cd {projectPath}
dotnet ef migrations add {note} -c {fullDatabaseName} -s ""{startProject}"" -o ""{projectPath}\Domain\Migrations\{databaseName}""
";

                sb.AppendLine(cmdStr);
            }

            ViewData["GenerateCodeResult"] = sb.ToString();
        }
    }
}