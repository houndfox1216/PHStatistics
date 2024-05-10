using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework.Data;
using System.Runtime.Serialization;

<<<<<<< HEAD
namespace EmptyProject {
=======
namespace PHStatistics {
>>>>>>> origin/develop/schema
    /// <summary>
    /// 權限資料
    /// </summary>
    [Description("權限資料"), DataContract(IsReference = true)]
    [NotMapped]
    public class Permission : IPermissionData {
        #region IPermissionData 成員

        #region IEntityData 成員

        object IEntityData.Id { get { return Id; } }

        #endregion

        string IPermissionData.EntityId { get { return null; } }
        EntityRights IPermissionData.Rights { get { return EntityRights.Readable; } }

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public int Id { get; set; }

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
        /// 數值
        /// </summary>
        [Display(Name = "數值")]
        [MaxLength(128), Required]
        public string Value { get; set; }

        /// <summary>
        /// 列舉名稱
        /// </summary>
        [Display(Name = "列舉名稱")]
        [MaxLength(128), Required]
        public string EnumName { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Display(Name = "名稱"), DataMember]
        [MaxLength(128), Required]
        public string Name { get; set; }

        /// <summary>
        /// 說明
        /// </summary>
        [Display(Name = "說明"), DataMember]
        [MaxLength(256)]
        public string Description { get; set; }
    }
}
