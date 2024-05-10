using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Framework;
using System.Framework.Data;
using System.Framework.Logging;
using System.Runtime.Serialization;

<<<<<<< HEAD
namespace EmptyProject {
=======
namespace PHStatistics {
>>>>>>> origin/develop/schema
    /// <summary>
    /// 訊息
    /// </summary>
    [Description("訊息"), DataContract(IsReference = true)]
    public class Message : IMessageData {
        #region IMessageData members

        #region ILog members

        DateTime ILog.Time => CreatedTime ?? DateTime.Now;
        string ILog.Message => Content;

        #endregion

        #region IEntityData 成員

        object IEntityData.Id => Id;
        string IEntityData.Name => this.GetDefaultName<Message>();

        #endregion

        IUserData IMessageData.Sender => Sender;
        IUserData IMessageData.Recipient => Recipient;
        bool IMessageData.IsReaded => false;
        bool IMessageData.IsDeletedBySender => false;
        bool IMessageData.IsDeletedByRecipient => false;

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
        /// 類型
        /// </summary>
        [Display(Name = "類型"), DataMember]
        public MessageType? Type { get; set; }

        /// <summary>
        /// 主旨
        /// </summary>
        [Display(Name = "主旨"), DataMember]
        [MaxLength(128)]
        public string Subject { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Display(Name = "內容"), DataMember]
        [MaxLength(512)]
        public string Content { get; set; }

        /// <summary>
        /// 寄件人
        /// </summary>
        [Display(Name = "寄件人"), DataMember]
        public User Sender { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        [Display(Name = "寄件人"), DataMember]
        public User Recipient { get; set; }
    }
}
