using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Senparc.Mvc.CustomActionResults;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Mvc
{
    public static class ActionControllerExtensions
    {
        /// <summary>
        /// Creates a ZIP action result from existing file paths without additional virtual files.
        /// </summary>
        /// <param name="controller">The controller receiving the extension call.</param>
        /// <param name="zipFilename">The download file name.</param>
        /// <param name="filenames">The files to include.</param>
        /// <returns>A ZIP action result.</returns>
        public static ZipResult Zip(this Controller controller, string zipFilename, IEnumerable<string> filenames)
        {
            return Zip(controller, zipFilename, filenames, null);
        }

        /// <summary>
        /// Creates a ZIP action result from files and optional in-memory file contents.
        /// </summary>
        /// <param name="controller">The controller receiving the extension call.</param>
        /// <param name="zipFilename">The download file name.</param>
        /// <param name="filenames">The files to include.</param>
        /// <param name="newFilesFromString">Optional archive-entry names and text contents.</param>
        /// <returns>A ZIP action result.</returns>
        public static ZipResult Zip(this Controller controller, string zipFilename, IEnumerable<string> filenames, Dictionary<string, string> newFilesFromString)
        {
            return new ZipResult(null,zipFilename, filenames, newFilesFromString,null);
        }

        /// <summary>
        /// Creates a legacy captcha image action result for the selected code kind.
        /// </summary>
        /// <param name="controller">The controller receiving the extension call.</param>
        /// <param name="checoCodeKind">The captcha generation mode.</param>
        /// <returns>The captcha action result.</returns>
        public static ActionResult CheckCode(this Controller controller, CheckCodeKind checoCodeKind)
        {
            return new CheckCodeResult(checoCodeKind);
        }
    }
}
