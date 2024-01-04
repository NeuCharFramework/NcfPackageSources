
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Senparc.AI;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    public class PagedResponse<T>
    {
        public int Total { get; set; }
        public List<T> Data { get; set; }
        public PagedResponse( List<T> data,int total)
        {
            Total = total;
            Data = data;
        }
    }

    public class PagedRequest
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public string Order { get; set; } = "Id desc";
    }
    public class AIModel_GetListRequest:PagedRequest
    {

      public string Name { get; set; }

      public string Endpoint { get; set; }

      public AiPlatform AiPlatform { get; set; }

      public string OrganizationId { get; set; }

      public string ApiKey { get; set; }

      public string ApiVersion { get; set; }

      public string Note { get; set; }

      public int MaxToken { get; set; }

      public bool Show { get; set; }

    }
    public class AIModel_CreateOrEditRequest
    {

      public string Name { get; set; }

      public string Endpoint { get; set; }

      public AiPlatform AiPlatform { get; set; }

      public string OrganizationId { get; set; }

      public string ApiKey { get; set; }

      public string ApiVersion { get; set; }

      public string Note { get; set; }

      public int MaxToken { get; set; }

      public bool Show { get; set; }

        public AIModel_CreateOrEditRequest() { }

    }
  //  [ExcelImporter(IsLabelingError = true)]
 //  [ExcelExporter(Name = "AIModel", TableStyle = TableStyles.Light10, AutoFitAllColumn = true)]
  //  public class AIModel_ImportExportResponse
  //  {

    //     [ExporterHeader(DisplayName = "Name")]
    //     [ImporterHeader(Name = "Name", IsAllowRepeat = true)]
    //     public string Name { get; set; }

    //     [ExporterHeader(DisplayName = "Endpoint")]
    //     [ImporterHeader(Name = "Endpoint", IsAllowRepeat = true)]
    //     public string Endpoint { get; set; }

    //     [ExporterHeader(DisplayName = "AiPlatform")]
    //     [ImporterHeader(Name = "AiPlatform", IsAllowRepeat = true)]
    //     public AiPlatform AiPlatform { get; set; }

    //     [ExporterHeader(DisplayName = "OrganizationId")]
    //     [ImporterHeader(Name = "OrganizationId", IsAllowRepeat = true)]
    //     public string OrganizationId { get; set; }

    //     [ExporterHeader(DisplayName = "ApiKey")]
    //     [ImporterHeader(Name = "ApiKey", IsAllowRepeat = true)]
    //     public string ApiKey { get; set; }

    //     [ExporterHeader(DisplayName = "ApiVersion")]
    //     [ImporterHeader(Name = "ApiVersion", IsAllowRepeat = true)]
    //     public string ApiVersion { get; set; }

    //     [ExporterHeader(DisplayName = "Note")]
    //     [ImporterHeader(Name = "Note", IsAllowRepeat = true)]
    //     public string Note { get; set; }

    //     [ExporterHeader(DisplayName = "MaxToken")]
    //     [ImporterHeader(Name = "MaxToken", IsAllowRepeat = true)]
    //     public int MaxToken { get; set; }

    //     [ExporterHeader(DisplayName = "Show")]
    //     [ImporterHeader(Name = "Show", IsAllowRepeat = true)]
    //     public bool Show { get; set; }

       
       
 //   }

    public class AIModel_Response
    {
        public int Id { get; set; }

       public string Name { get; set; }

       public string Endpoint { get; set; }

       public AiPlatform AiPlatform { get; set; }

       public string OrganizationId { get; set; }

       public string ApiKey { get; set; }

       public string ApiVersion { get; set; }

       public string Note { get; set; }

       public int MaxToken { get; set; }

       public bool Show { get; set; }

        public AIModel_Response() { }
    }
}


