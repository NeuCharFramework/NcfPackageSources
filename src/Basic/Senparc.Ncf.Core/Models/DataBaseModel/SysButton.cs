using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// button corresponding to the menu
    /// </summary>
    [Table("SysButtons")]
    public class SysButton : EntityBase<string>
    {
        public SysButton()
        {
            Id = Guid.NewGuid().ToString();
            AddTime = DateTime.Now;
            LastUpdateTime = AddTime;
        }

        public SysButton(SysButtonDto sysButtonDto) : this()
        {
            MenuId = sysButtonDto.MenuId;
            ButtonName = sysButtonDto.ButtonName;
            OpearMark = sysButtonDto.OpearMark;
            Url = sysButtonDto.Url;
        }

        /// <summary>
        /// menu id
        /// </summary>
        [MaxLength(150)]
        public string MenuId { get; set; }

        /// <summary>
        /// operation name
        /// </summary>
        [MaxLength(150)]
        [Required]
        public string ButtonName { get; set; }

        /// <summary>
        /// operation identifier
        /// </summary>
        [MaxLength(150)]
        public string OpearMark { get; set; }

        public void Update(SysButtonDto item)
        {
            ButtonName = item.ButtonName;
            OpearMark = item.OpearMark;
            Url = item.Url;
            LastUpdateTime = DateTime.Now;
        }

        /// <summary>
        ///Request address corresponding to the button
        /// </summary>
        [MaxLength(500)]
        public string Url { get; set; }
    }
}
