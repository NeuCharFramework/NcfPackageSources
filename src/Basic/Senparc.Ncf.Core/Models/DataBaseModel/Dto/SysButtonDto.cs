using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysButtonDto : DtoBase
    {

        //public bool IsDeleted { get; set; }

        public string Id { get; set; }

        /// <summary>
        /// menu id
        /// </summary>
        [MaxLength(50)]
        public string MenuId { get; set; }

        /// <summary>
        /// operation name
        /// </summary>
        [MaxLength(50)]
        //[Required]
        public string ButtonName { get; set; }

        /// <summary>
        /// operation identifier
        /// </summary>
        [MaxLength(50)]
        public string OpearMark { get; set; }

        /// <summary>
        ///Request address corresponding to the button
        /// </summary>
        [MaxLength(350)]
        public string Url { get; set; }
    }
}
