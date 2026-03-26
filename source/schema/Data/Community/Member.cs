using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Business;
using System.Framework.Community;
using System.Framework.Data;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Linq;
using PHStatistics.Content;

using IndexAttribute = System.Framework.Data.IndexAttribute;

namespace PHStatistics.Community {
    /// <summary>
    /// 會員資料
    /// </summary>
    [Description("會員資料"), DataContract(IsReference = true)]
    public class Member : IMemberData, IUserData, ICustomerData {
        #region IMemberData 成員

        #region IEntityData 成員

        object IEntityData.Id => Id;

        string IEntityData.Name => Person?.Name ?? Nickname;

        #endregion

        INaturalPersonData IMemberData.Person => Person;
        int IMemberData.Bonus => 0;

        #endregion

        #region IUserData 成員

        string IUserData.Account => Email;
        IPictureData IUserData.Photo => Person?.Photo;
        INaturalPersonData IUserData.Person => Person;
        ICollection<IRoleData> IUserData.Roles => new List<IRoleData>().AsReadOnly();
        UserStatus IUserData.Status => Status == MemberStatus.Expired ? UserStatus.Disabled : (UserStatus)Status;
        ISessionData IUserData.Session => null;
        ICollection<IPermissionData> IUserData.Permissions =>
            MemberRoles == null
                ? new List<IPermissionData>().AsReadOnly()
                : new List<IPermissionData>(MemberRoles.SelectMany(mr => mr.Role?.Permissions ?? Array.Empty<Permission>())).AsReadOnly();

        #endregion

        #region IUserData 成員

        CustomerType ICustomerData.Type => CustomerType.NaturalPerson;
        DateTime? ICustomerData.StartTime => null;
        DateTime? ICustomerData.EndTime => null;
        IGradeData ICustomerData.Grade => null;
        int ICustomerData.Bonus => 0;
        IPersonData ICustomerData.Person => Person;
        ICollection<IContactData> ICustomerData.Contacts => Array.Empty<IContactData>();
        CustomerStatus ICustomerData.Status { get { try { return (CustomerStatus)Status; } catch { return CustomerStatus.Other; } } }

        #endregion

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
        /// 編號
        /// </summary>
        [Display(Name = "編號"), DataMember]
        [MaxLength(16), Index(IsUnique = true)]
        public string Number { get; set; }

        /// <summary>
        /// 暱稱
        /// </summary>
        [Display(Name = "暱稱"), DataMember]
        public string Nickname { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "Email"), DataMember]
        [MaxLength(320)]
        public string Email { get; set; }

        /// <summary>
        /// 帳號
        /// </summary>
        [Display(Name = "帳號"), DataMember]
        [MaxLength(32)]
        public string Account { get; set; }

        /// <summary>
        /// 密碼
        /// </summary>
        [Display(Name = "密碼"), DataMember, DataType(DataType.Password)]
        [Unicode(false), MaxLength(128)]
        public string Password { get; set; }

        /// <summary>
        /// 密碼變更策略
        /// </summary>
        [Display(Name = "密碼變更策略")]
        public int? PasswordExpirationPolicy { get; set; }

        /// <summary>
        /// 密碼變更時間
        /// </summary>
        [Display(Name = "密碼變更時間")]
        public DateTime? PasswordChangedTime { get; set; }

        /// <summary>
        /// 令牌
        /// </summary>
        [Display(Name = "令牌"), DataMember]
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
        /// 最後訪問時間
        /// </summary>
        [Display(Name = "訪問時間"), DataMember]
        public DateTime? LastVisitedTime { get; set; }

        /// <summary>
        /// Facebook ID
        /// </summary>
        [Display(Name = "Facebook ID"), DataMember]
        [Unicode(false), MaxLength(128)]
        public string FacebookId { get; set; }

        /// <summary>
        /// Google ID
        /// </summary>
        [Display(Name = "Google ID"), DataMember]
        [Unicode(false), MaxLength(128)]
        public string GoogleId { get; set; }

        /// <summary>
        /// 照片
        /// </summary>
        [Display(Name = "照片"), DataMember]
        public Picture Photo { get; set; }

        /// <summary>
        /// 個資外鍵
        /// </summary>
        [Display(Name = "個人資料"), DataMember]
        public Guid? PersonId { get; set; }

        /// <summary>
        /// 個人資料
        /// </summary>
        [Display(Name = "個人資料"), DataMember]
        public Person Person { get; set; }

        /// <summary>
        /// 狀態
        /// </summary>
        [Display(Name = "狀態"), DataMember]
        public MemberStatus Status { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [Display(Name = "備註"), DataMember]
        [MaxLength(512)]
        public string Remark { get; set; }

        /// <summary>
        /// 分校操作人員
        /// </summary>
        [Display(Name = "分校操作人員"), DataMember]
        public ICollection<SchoolAssignment> SchoolAssignment { get; set; }

        /// <summary>
        /// 角色關聯
        /// </summary>
        [Display(Name = "角色關聯"), DataMember]
        public ICollection<MemberRole> MemberRoles { get; set; }
    }
}