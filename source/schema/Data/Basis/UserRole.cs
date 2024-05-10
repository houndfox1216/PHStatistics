using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

<<<<<<< HEAD
namespace EmptyProject {
=======
namespace PHStatistics {
>>>>>>> origin/develop/schema
    /// <summary>
    /// 用戶與角色之關聯
    /// </summary>
    [Description("用戶與角色之關聯"), DataContract(IsReference = true)]
    public class UserRole {
        /// <summary>
        /// 用戶識別碼
        /// </summary>
        [Display(Name = "用戶識別碼"), DataMember]
        public Guid UserId { get; set; }

        /// <summary>
        /// 用戶
        /// </summary>
        [Display(Name = "用戶"), DataMember]
        public User User { get; set; }

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
    }
}