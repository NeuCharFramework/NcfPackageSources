/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: MyFunctionRequest.cs

    File description: MyFunctionRequest related implementation

    

    

    Created by: Senparc - 20240311

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260717

    Change description: v0.13.0-preview3 Updated module behavior and related support.



----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.AIAgentsHub.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(AIAgentsHubResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(AIAgentsHubResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(AIAgentsHubResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(AIAgentsHubResource), "Parameter.Sample.Operator")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", AIAgentsHubResource.Get("Parameter.Operator.Add"), AIAgentsHubResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", AIAgentsHubResource.Get("Parameter.Operator.Subtract"), AIAgentsHubResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", AIAgentsHubResource.Get("Parameter.Operator.Multiply"), AIAgentsHubResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", AIAgentsHubResource.Get("Parameter.Operator.Divide"), AIAgentsHubResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(AIAgentsHubResource), "Parameter.Sample.Power")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", AIAgentsHubResource.Get("Parameter.Power.Square"), AIAgentsHubResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", AIAgentsHubResource.Get("Parameter.Power.Cube"), AIAgentsHubResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
