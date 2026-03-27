using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework.Data;
using System.Runtime.Serialization;
using PHStatistics.Community;

namespace PHStatistics.Content {
    /// <summary>
    /// 人員角色關聯
    /// </summary>
    [Description("人員角色關聯"), DataContract(IsReference = true)]
    public class MemberRole {
        /// <summary>
        /// 分校人員識別碼
        /// </summary>
        [Display(Name = "分校人員識別碼"), DataMember]
        public Guid MemberId { get; set; }

        /// <summary>
        /// 分校人員
        /// </summary>
        [Display(Name = "分校人員"), DataMember]
        public Member Member { get; set; }

        /// <summary>
        /// 角色識別碼
        /// </summary>
        [Display(Name = "角色識別碼"), DataMember]
        public Guid RoleId { get; set; }

        /// <summary>
        /// 角色
        /// </summary>
        [Display(Name = "角色"), DataMember]
        public Role Role { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間")]
        public DateTime? CreatedTime { get; set; }
    }
}
