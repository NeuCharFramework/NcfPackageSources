/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFunctionRequest.cs
    File description: MyFunctionRequest related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.FileManager.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(FileManagerResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(FileManagerResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(FileManagerResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(FileManagerResource), "Parameter.Sample.Operator")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", FileManagerResource.Get("Parameter.Operator.Add"), FileManagerResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", FileManagerResource.Get("Parameter.Operator.Subtract"), FileManagerResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", FileManagerResource.Get("Parameter.Operator.Multiply"), FileManagerResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", FileManagerResource.Get("Parameter.Operator.Divide"), FileManagerResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(FileManagerResource), "Parameter.Sample.Power")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", FileManagerResource.Get("Parameter.Power.Square"), FileManagerResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", FileManagerResource.Get("Parameter.Power.Cube"), FileManagerResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
