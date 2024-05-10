using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Framework.Data;
using System.Framework.Security;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject {
    /// <summary>
    /// 角色資料
    /// </summary>
    [Description("角色資料"), DataContract(IsReference = true)]
    public class Role : IRoleData, IOperability {
        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }

        #endregion

        #region IRoleData 成員

        ICollection<IPermissionData> IRoleData.Permissions { get { return new List<IPermissionData>(Permissions).AsReadOnly(); } }

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(32)]
        public string Name { get; set; }

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
        /// 說明
        /// </summary>
        [Display(Name = "說明"), DataMember]
        [MaxLength(512)]
        public string Description { get; set; }

        /// <summary>
        /// 權限值
        /// </summary>
        [Display(Name = "權限值")]
        [MaxLength(256), Unicode(false)]
        public string PermissionValue { get; set; }

        /// <summary>
        /// 權限
        /// </summary>
        [Display(Name = "權限")]
        [NotMapped]
        public ICollection<Permission> Permissions { 
            get {
                try {
                    return Permissions<SystemPermission, Permission>.GetPermissionsByValue(PermissionValue);
                } catch { return new HashSet<Permission>(); } 
            }
            set {
                var permission = new Permission();
                permission.SetValue(new List<IPermissionData>(value).ToArray());
                PermissionValue = permission.Value;
            }
        }

        /// <summary>
        /// 權限值
        /// </summary>
        [Display(Name = "權限值"), DataMember()]
        [NotMapped]
        [SuppressMessage("Critical Code Smell", "S2365:Properties should not make collection or array copies", Justification = "<暫止>")]
        public int[] PermissionValues {
            get => Permissions.Select(e => e.Id).ToArray();
            set => Permissions = Permissions<SystemPermission, Permission>.SystemPermissions.Where(e => value.Contains(e.Id)).ToArray();
        }
    }
}
