/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MyFunctionRequest.cs
    File description: MyFunctionRequest related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(KnowledgeBaseResource), "Parameter.Sample.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(KnowledgeBaseResource), "Parameter.Sample.Number1")]
        public int Number1 { get; set; }


        [Required]
        [LocalizedDescription(typeof(KnowledgeBaseResource), "Parameter.Sample.Number2")]
        public int Number2 { get; set; }

        [LocalizedDescription(typeof(KnowledgeBaseResource), "Parameter.Sample.Operator")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.DropDownList, nameof(OperatorOptions))]
           public string Operator { get; set; }

           [JsonIgnore]
           public SelectionList OperatorOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+", KnowledgeBaseResource.Get("Parameter.Operator.Add"), KnowledgeBaseResource.Get("Parameter.Operator.Add.Help"), false),
                 new SelectionItem("-", KnowledgeBaseResource.Get("Parameter.Operator.Subtract"), KnowledgeBaseResource.Get("Parameter.Operator.Subtract.Help"), true),
                 new SelectionItem("×", KnowledgeBaseResource.Get("Parameter.Operator.Multiply"), KnowledgeBaseResource.Get("Parameter.Operator.Multiply.Help"), false),
                 new SelectionItem("÷", KnowledgeBaseResource.Get("Parameter.Operator.Divide"), KnowledgeBaseResource.Get("Parameter.Operator.Divide.Help"), false)
            });

        [LocalizedDescription(typeof(KnowledgeBaseResource), "Parameter.Sample.Power")]//Legacy comment translated to English.
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(PowerOptions))]
           public string[] Power { get; set; }

           [JsonIgnore]
           public SelectionList PowerOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2", KnowledgeBaseResource.Get("Parameter.Power.Square"), KnowledgeBaseResource.Get("Parameter.Power.Square.Help"), false),
                 new SelectionItem("3", KnowledgeBaseResource.Get("Parameter.Power.Cube"), KnowledgeBaseResource.Get("Parameter.Power.Cube.Help"), false)
            });
    }
}
