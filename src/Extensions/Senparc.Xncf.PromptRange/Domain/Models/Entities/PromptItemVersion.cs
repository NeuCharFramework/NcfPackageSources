using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Models
{
    public record class PromptItemVersion
    {
        public string RangeName { get; set; }
        public string Tactic { get; set; }
        public int Aim { get; set; }

        public PromptItemVersion(string rangeName, string tactic, int aim)
        {
            RangeName = rangeName;
            Tactic = tactic;
            Aim = aim;
        }
    }
}
