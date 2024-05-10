using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Data;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

using Environment = System.Framework.Environment;

namespace PHStatistics {
    /// <summary>
    /// 用戶資料
    /// </summary>
    [Description("用戶資料"), DataContract(IsReference = true)]
    public class User : IUserData {
        #region IUserData 成員

        #region IEntityData 成員

        object IEntityData.Id => Id;

        #endregion

        INaturalPersonData IUserData.Person => Person;
        ICollection<IRoleData> IUserData.Roles => new List<IRoleData>(Roles ?? new List<Role>()).AsReadOnly();
        IPictureData IUserData.Photo => Photo;
        ISessionData IUserData.Session => null;
        ICollection<IPermissionData> IUserData.Permissions => new List<IPermissionData>((Roles ?? new List<Role>()).SelectMany(e => e.Permissions).Distinct()).AsReadOnly();

        #endregion

        private IEnumerable<Role> roles = null;

        /// <summary>
        /// 用戶與角色之關聯
        /// </summary>
        [Display(Name = "用戶與角色之關聯")]
        public ICollection<UserRole> UserRoles { get; set; }

        /// <summary>
        /// 載入相關角色
        /// </summary>
        /// <param name="context">資料脈絡</param>
        public User LoadRoles(DataContext context) {
            UserRoles = context.UserRole.Where(e => e.UserId == this.Id).ToList();
            return this;
        }

        /// <summary>
        /// 相關角色
        /// </summary>
        [Display(Name = "相關角色")]
        [NotMapped]
        public IEnumerable<Role> Roles => UserRoles?.Select(e => e.Role ?? new Role { Id = e.RoleId }) ?? roles ?? Array.Empty<Role>();

        /// <summary>
        /// 相關角色
        /// </summary>
        [Display(Name = "相關角色"), DataMember]
        [NotMapped]
        public Guid[] RoleIds {
            get => Roles.Select(e => e.Id).ToArray();
            set {
                if (UserRoles != null) {
                    UserRoles.Clear();
                    foreach (var roleId in value) UserRoles.Add(new UserRole { UserId = Id, RoleId = roleId });
                } else roles = value.Select(e => new Role { Id = e });
            }
        }

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間"), DataMember]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [Display(Name = "更新時間"), DataMember]
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 資料模式
        /// </summary>
        [Display(Name = "資料模式"), DataMember]
        public DataMode DataMode { get; set; }

        /// <summary>
        /// 照片
        /// </summary>
        [Display(Name = "照片"), DataMember]
        public Picture Photo { get; set; }

        /// <summary>
        /// 個資識別碼
        /// </summary>
        [Display(Name = "個資識別碼"), DataMember]
        public Guid? PersonId { get; set; }

        /// <summary>
        /// 個人資料
        /// </summary>
        [Display(Name = "個人資料"), DataMember]
        public Person Person { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

        /// <summary>
        /// 帳號
        /// </summary>
        [Display(Name = "帳號"), DataMember]
        [Unicode(false), MaxLength(128)]
        public string Account { get; set; }

        /// <summary>
        /// 密碼
        /// </summary>
        [Display(Name = "密碼")]
        [Unicode(false), MaxLength(128), DataMember, DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// 密碼變更策略
        /// </summary>
        [Display(Name = "密碼變更策略"), DataMember]
        public int? PasswordExpirationPolicy { get; set; }

        /// <summary>
        /// 密碼變更時間
        /// </summary>
        [Display(Name = "密碼變更時間"), DataMember]
        public DateTime? PasswordChangedTime { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "Email"), DataMember]
        [MaxLength(320)]
        public string Email { get; set; }

        /// <summary>
        /// 通行令牌
        /// </summary>
        [Display(Name = "通行令牌"), DataMember]
        [Unicode(false), MaxLength(128)]
        public string Token { get; set; }

        /// <summary>
        /// 登入時間
        /// </summary>
        [Display(Name = "登入時間"), DataMember]
        public DateTime? LoginTime { get; set; }

        /// <summary>
        /// 登出時間
        /// </summary>
        [Display(Name = "登出時間"), DataMember]
        public DateTime? LogoutTime { get; set; }

        /// <summary>
        /// 訪問時間
        /// </summary>
        [Display(Name = "訪問時間"), DataMember]
        public DateTime? LastVisitedTime { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        [Display(Name = "狀態"), DataMember]
        public UserStatus Status { get; set; }

        /// <summary>
        /// 登入狀態
        /// </summary>
        [Display(Name = "登入狀態"), DataMember]
        public bool Online => LoginTime.HasValue
            && LoginTime > (LogoutTime ?? DateTime.MinValue)
            && LastVisitedTime.HasValue
            && Environment.AuthenticationKeepalive > DateTime.Now - LastVisitedTime.Value;
    }
}