/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.FileManager
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        public string HomeUrl => "/Admin/FileManager/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         //Legacy comment translated to English.
                         new AreaPageMenuItem(GetAreaUrl($"/Admin/FileManager/Index"), FileManagerResource.Get("Area.FileManager.Files", "File resource management"),"fa fa-file")
        };

        /// <summary>
        /// Configures Razor Page authorization hooks for the FileManager area.
        /// </summary>
        /// <param name="builder">The MVC builder receiving the area configuration.</param>
        /// <param name="env">The hosting environment used by MVC configuration.</param>
        /// <returns>The configured MVC builder.</returns>
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("FileManager startup", "Completed Area:Senparc.Xncf.FileManager registration");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.FileManager"));
        #endregion
    }
}
