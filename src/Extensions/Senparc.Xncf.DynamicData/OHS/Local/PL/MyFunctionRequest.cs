/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: MyFunctionRequest.cs

    File description: MyFunctionRequest related implementation

    

    

    Created by: Senparc - 20240718

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260717

    Change description: v0.6.0-preview2 integrate shared resource localization and improve feature text for the DynamicData module



----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.DynamicData.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Operator")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", DynamicDataResource.Get("Parameter.Operator.Add"), DynamicDataResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", DynamicDataResource.Get("Parameter.Operator.Subtract"), DynamicDataResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", DynamicDataResource.Get("Parameter.Operator.Multiply"), DynamicDataResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", DynamicDataResource.Get("Parameter.Operator.Divide"), DynamicDataResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(DynamicDataResource), "Parameter.Sample.Power")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", DynamicDataResource.Get("Parameter.Power.Square"), DynamicDataResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", DynamicDataResource.Get("Parameter.Power.Cube"), DynamicDataResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
