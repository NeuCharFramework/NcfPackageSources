/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFunctionRequest.cs
    File description: MyFunctionRequest related implementation

    Created by: Senparc - 20211031

    Modified by: Senparc - 20260717
    Change description: v1.1.0 update feature parameters and page localization for the sample XNCF module

    Modified by: Senparc - 20260724
    Change description: v1.1.0 improve multilingual support for XNCF template pages and resources

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.Core.AppServices;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Template_OrgName.Xncf.Template_XncfName.Application.DTOs.Request
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Operator")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", Template_XncfNameResource.Get("Parameter.Operator.Add"), Template_XncfNameResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", Template_XncfNameResource.Get("Parameter.Operator.Subtract"), Template_XncfNameResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", Template_XncfNameResource.Get("Parameter.Operator.Multiply"), Template_XncfNameResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", Template_XncfNameResource.Get("Parameter.Operator.Divide"), Template_XncfNameResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(Template_XncfNameResource), "Parameter.Sample.Power")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", Template_XncfNameResource.Get("Parameter.Power.Square"), Template_XncfNameResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", Template_XncfNameResource.Get("Parameter.Power.Cube"), Template_XncfNameResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }

    /// <summary>
    /// Marks the template EventBus round-trip Function request.
    /// </summary>
    public class EventBusRoundTripRequest : FunctionAppRequestBase
    {
    }
}
