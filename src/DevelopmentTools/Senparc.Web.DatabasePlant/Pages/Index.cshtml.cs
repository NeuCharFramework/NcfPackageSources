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

        /// <summary>
        /// Handles the database-plant page GET request.
        /// </summary>
        public void OnGet()
        {

        }

        /// <summary>
        /// Generates Entity Framework migration commands for selected projects and
        /// exposes the resulting command text to the page.
        /// </summary>
        /// <param name="srcRootDir">The repository source-root directory.</param>
        /// <param name="projects">Newline-separated project paths.</param>
        /// <param name="note">The migration name passed to the EF CLI.</param>
        /// <param name="databaseName">The database provider suffix used for the context and output path.</param>
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
