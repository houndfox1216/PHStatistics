using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Framework;
using System.Framework.Data;
using System.Framework.Logging;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EmptyProject {
    /// <summary>
    /// 操作紀錄
    /// </summary>
    [Description("操作紀錄"), DataContract(IsReference = true)]
    public class ActionLog : IActionLogData {
        #region IActionLogData members

        #region ILog members

        DateTime ILog.Time => CreatedTime ?? DateTime.Now;
        MessageType? ILog.Type => MessageType.Info;
        string ILog.Message => Xml;

        #endregion

        #region IEntityData members

        object IActionLogData.Id { get => Id; set => Id = (long)value; }
        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<ActionLog>();
        DateTime? IEntityData.UpdatedTime { get => null; set => _ = value; }

        #endregion

        #endregion

        /// <summary>
        /// 識別碼
        /// </summary>
        [Display(Name = "識別碼"), DataMember]
        public long Id { get; set; }

        /// <summary>
        /// 時間
        /// </summary>
        [Display(Name = "時間"), DataMember]
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 操作型別
        /// </summary>
        [Display(Name = "操作型別"), DataMember]
        [MaxLength(128), Unicode(false)]
        public string ActionType { get; set; }

        /// <summary>
        /// 操作名稱
        /// </summary>
        [Display(Name = "操作名稱"), DataMember]
        [MaxLength(32)]
        public string ActionName { get; set; }

        /// <summary>
        /// 用戶型別
        /// </summary>
        [Display(Name = "用戶型別"), DataMember]
        [MaxLength(128), Unicode(false)]
        public string UserType { get; set; }

        /// <summary>
        /// 用戶類型
        /// </summary>
        [Display(Name = "用戶類型"), DataMember]
        [MaxLength(32)]
        public string UserTypeName { get; set; }

        /// <summary>
        /// 用戶資料型別
        /// </summary>
        [Display(Name = "用戶資料型別"), DataMember]
        [MaxLength(128), Unicode(false)]
        public string UserDataType { get; set; }

        /// <summary>
        /// 用戶資料類型
        /// </summary>
        [Display(Name = "用戶資料類型"), DataMember]
        [MaxLength(32)]
        public string UserDataTypeName { get; set; }

        /// <summary>
        /// 用戶代號
        /// </summary>
        [Display(Name = "用戶代號"), DataMember]
        [MaxLength(128), Unicode(false)]
        public string UserId { get; set; }

        /// <summary>
        /// 用戶名稱
        /// </summary>
        [Display(Name = "用戶名稱"), DataMember]
        [MaxLength(32)]
        public string UserName { get; set; }

        /// <summary>
        /// 資料型別
        /// </summary>
        [Display(Name = "資料型別"), DataMember]
        [MaxLength(128), Unicode(false)]
        public string EntityType { get; set; }

        /// <summary>
        /// 資料類型
        /// </summary>
        [Display(Name = "資料類型"), DataMember]
        [MaxLength(128)]
        public string EntityTypeName { get; set; }

        /// <summary>
        /// 資料編號
        /// </summary>
        [Display(Name = "資料編號"), DataMember]
        [MaxLength(128)]
        public string EntityId { get; set; }

        /// <summary>
        /// 資料名稱
        /// </summary>
        [Display(Name = "資料名稱"), DataMember]
        [MaxLength(128)]
        public string EntityName { get; set; }

        /// <summary>
        /// 詳細資料
        /// </summary>
        [Display(Name = "詳細資料")]
        [Column(TypeName = "xml"), DataMember]
        public string Xml { get; set; }
    }
}
