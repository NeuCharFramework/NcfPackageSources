/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFunctionRequest.cs
    File description: MyFunctionRequest related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.SenMapic.OHS.Local.PL
{
    public class MyFunction_SenMapicRequest : FunctionAppRequestBase
    {
        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.SenMapic.Url")]
        public string Url { get; set; }

        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.SenMapic.Depth")]
        public int Deepth { get; set; }

        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.SenMapic.PageCount")]
        public int PageNumber { get; set; }

    }
    public class MyFunction_CaculateRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.OperatorRestricted")]//Legacy comment translated to English.
        [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
        public string Operator { get; set; }

        [JsonIgnore]
        public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", SenMapicResource.Get("Parameter.Operator.Add"), SenMapicResource.Get("Parameter.Operator.Add.Help"), true),
                 new SelectionItem("-", SenMapicResource.Get("Parameter.Operator.Subtract"), SenMapicResource.Get("Parameter.Operator.Subtract.Help"), false),
                 new SelectionItem("×", SenMapicResource.Get("Parameter.Operator.Multiply"), SenMapicResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", SenMapicResource.Get("Parameter.Operator.Divide"), SenMapicResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(SenMapicResource), "Parameter.Sample.PowerRestricted")]//Legacy comment translated to English.
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
        public string[] Power { get; set; }

        [JsonIgnore]
        public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", SenMapicResource.Get("Parameter.Power.Square"), SenMapicResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", SenMapicResource.Get("Parameter.Power.Cube"), SenMapicResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
