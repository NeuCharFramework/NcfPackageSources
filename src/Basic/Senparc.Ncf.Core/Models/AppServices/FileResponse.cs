using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Ncf.Core.Models.AppServices
{
    public class FileResponse
    {
        public Stream Stream { get; set; }
        public string ContentType { get; set; }
    }
}
